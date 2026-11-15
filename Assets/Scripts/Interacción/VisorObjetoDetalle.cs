using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Visor de objeto en detalle con render aislado (cámara propia -> RenderTexture -> RawImage).
/// Funciona con cualquier modo de Canvas (Overlay, Camera, World) porque el objeto 3D
/// se dibuja DENTRO del panel, entre el fondo y los textos/botones.
/// Debe estar en un GameObject SIEMPRE ACTIVO (no en el panel que se oculta).
/// </summary>
public class VisorObjetoDetalle : MonoBehaviour
{
    public static VisorObjetoDetalle Instance;

    [Header("UI")]
    [SerializeField] private GameObject panelVisor;
    [Tooltip("Opcional. Si se deja vacío, se crea automáticamente como primer hijo del panel (detrás de textos y botón).")]
    [SerializeField] private RawImage imagenVisor;
    [SerializeField] private Button botonCerrar;
    [SerializeField] private TextMeshProUGUI textoTitulo;
    [SerializeField] private TextMeshProUGUI textoDescripcion;

    [Header("Render aislado")]
    [Tooltip("Cámara AR (si se deja vacío usa Camera.main). Se le oculta la capa del visor mientras está abierto.")]
    [SerializeField] private Camera camaraAR;
    [Tooltip("Capa SIN USAR en el proyecto, solo para el objeto del visor")]
    [SerializeField] private int capaVisor = 31;
    [SerializeField] private Vector3 posicionRig = new Vector3(0f, 5000f, 0f);
    [SerializeField] private float campoVision = 30f;
    [Tooltip("Tamaño interno (unidades) de la dimensión más grande del objeto")]
    [SerializeField] private float tamanoObjetivo = 1f;
    [Tooltip("Qué fracción de la pantalla ocupa el objeto al abrir")]
    [SerializeField, Range(0.3f, 0.9f)] private float ocupacionPantalla = 0.55f;
    [Tooltip("1 = resolución nativa del panel. Baja a 0.75 si el celular va lento")]
    [SerializeField, Range(0.4f, 1f)] private float escalaResolucion = 1f;
    [SerializeField] private bool crearLuzPropia = true;

    [Header("Interacción")]
    [SerializeField] private float velocidadRotacion = 0.3f;     // grados por píxel
    [SerializeField] private float velocidadZoomTactil = 0.002f;
    [SerializeField] private float velocidadZoomMouse = 0.1f;
    [SerializeField] private float zoomMin = 0.5f;
    [SerializeField] private float zoomMax = 2.5f;

    [Header("Animación")]
    [SerializeField] private float duracionAnimacion = 0.4f;

    // Estado de render
    private GameObject rig;
    private Transform pivot;
    private Camera visorCam;
    private RenderTexture rt;
    private int maskOriginalAR;
    private bool maskAlterada = false;

    // Estado de interacción
    private float escalaBase = 1f;
    private float zoomActual = 1f;
    private bool activo = false;
    private bool interactivo = false;
    private bool arrastrando = false;
    private Vector2 ultimaPosMouse;

    private readonly List<RaycastResult> resultadosRaycast = new List<RaycastResult>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (panelVisor != null) panelVisor.SetActive(false);
        if (botonCerrar != null) botonCerrar.onClick.AddListener(CerrarVisor);
    }

    void OnDestroy()
    {
        if (botonCerrar != null) botonCerrar.onClick.RemoveListener(CerrarVisor);
        if (Instance == this) Instance = null;
        if (activo) LimpiarVisor();
    }

    // ============================================================
    // API
    // ============================================================

    /// <summary>
    /// Muestra el objeto y espera (como corrutina) hasta que el usuario lo cierre.
    /// Uso: yield return VisorObjetoDetalle.Instance.MostrarYEsperar(...);
    /// </summary>
    public IEnumerator MostrarYEsperar(GameObject prefab, string titulo, string descripcion)
    {
        if (prefab == null || activo) yield break;
        if (panelVisor == null)
        {
            Debug.LogError("[VisorObjetoDetalle] panelVisor no asignado.");
            yield break;
        }

        activo = true;
        interactivo = false;

        if (camaraAR == null) camaraAR = Camera.main;

        SubtitulosMito.Instance?.DetenerYLimpiar();

        if (textoTitulo != null) textoTitulo.text = titulo ?? "";
        if (textoDescripcion != null) textoDescripcion.text = descripcion ?? "";

        // Panel visible primero: hay que medirlo para dimensionar el RenderTexture
        panelVisor.SetActive(true);
        AsegurarImagenVisor();
        Canvas.ForceUpdateCanvases();

        CrearRenderTexture();
        CrearRig();

        // Instancia del objeto dentro del rig aislado
        GameObject instancia = Instantiate(prefab, pivot);
        instancia.transform.localPosition = Vector3.zero;
        instancia.transform.localRotation = Quaternion.identity;
        AsignarCapaRecursiva(instancia, capaVisor);
        instancia.SetActive(true);

        // Centrar en el pivot y normalizar tamaño
        Renderer[] renderers = instancia.GetComponentsInChildren<Renderer>();
        float maxDim = 1f;
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            instancia.transform.position -= (b.center - pivot.position);
            maxDim = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (maxDim < 0.0001f) maxDim = 1f;
        }
        escalaBase = tamanoObjetivo / maxDim;
        zoomActual = 1f;

        // Distancia de cámara para que el objeto quepa (limita el lado más estrecho de la pantalla)
        float aspecto = (float)rt.width / rt.height;
        float tanMitad = Mathf.Tan(campoVision * 0.5f * Mathf.Deg2Rad);
        float ladoVisible = tamanoObjetivo / ocupacionPantalla;
        float distancia = ladoVisible / (2f * tanMitad * Mathf.Min(1f, aspecto));
        visorCam.transform.localPosition = new Vector3(0f, 0f, -distancia);
        visorCam.transform.localRotation = Quaternion.identity;

        pivot.localScale = Vector3.zero;

        // Ocultar la capa del visor a la cámara AR (por seguridad, además de la distancia)
        if (camaraAR != null)
        {
            maskOriginalAR = camaraAR.cullingMask;
            camaraAR.cullingMask &= ~(1 << capaVisor);
            maskAlterada = true;
        }

        visorCam.enabled = true;
        yield return null; // dejar que la cámara renderice un frame antes de mostrar la textura
        if (imagenVisor != null) imagenVisor.enabled = true;

        yield return AnimarEscala(0f, escalaBase);
        interactivo = true;

        // Esperar a que el usuario lo cierre
        while (activo) yield return null;
    }

    public void CerrarVisor()
    {
        if (!activo || !interactivo) return;
        StartCoroutine(SecuenciaCierre());
    }

    /// <summary>Cierre inmediato (por si el quiz se cierra desde fuera).</summary>
    public void ForzarCierre()
    {
        if (!activo) return;
        StopAllCoroutines();
        LimpiarVisor();
    }

    private IEnumerator SecuenciaCierre()
    {
        interactivo = false;
        arrastrando = false;

        if (pivot != null)
            yield return AnimarEscala(pivot.localScale.x, 0f);

        LimpiarVisor();
    }

    // ============================================================
    // RENDER AISLADO
    // ============================================================

    private void AsegurarImagenVisor()
    {
        if (imagenVisor == null)
        {
            GameObject go = new GameObject("ImagenVisor3D", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(panelVisor.transform, false);
            go.transform.SetAsFirstSibling(); // detrás de textos y botón Cerrar

            RectTransform r = go.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;

            imagenVisor = go.GetComponent<RawImage>();
        }

        imagenVisor.color = Color.white;
        imagenVisor.raycastTarget = true;
        imagenVisor.enabled = false; // se habilita cuando ya hay un frame renderizado
    }

    private void CrearRenderTexture()
    {
        int w = Screen.width;
        int h = Screen.height;

        Rect r = imagenVisor.canvas != null
            ? RectTransformUtility.PixelAdjustRect(imagenVisor.rectTransform, imagenVisor.canvas)
            : new Rect(0, 0, Screen.width, Screen.height);

        if (r.width > 8f && r.height > 8f)
        {
            w = Mathf.RoundToInt(r.width);
            h = Mathf.RoundToInt(r.height);
        }

        w = Mathf.Clamp(Mathf.RoundToInt(w * escalaResolucion), 256, 2048);
        h = Mathf.Clamp(Mathf.RoundToInt(h * escalaResolucion), 256, 2048);

        rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 2,
            name = "VisorObjetoRT"
        };
        rt.Create();
        imagenVisor.texture = rt;
    }

    private void CrearRig()
    {
        rig = new GameObject("VisorRig");
        rig.transform.position = posicionRig;

        pivot = new GameObject("PivotVisor").transform;
        pivot.SetParent(rig.transform, false);

        GameObject camGO = new GameObject("VisorCamera");
        camGO.transform.SetParent(rig.transform, false);
        visorCam = camGO.AddComponent<Camera>();
        visorCam.clearFlags = CameraClearFlags.SolidColor;
        visorCam.backgroundColor = new Color(0f, 0f, 0f, 0f);   // fondo transparente
        visorCam.cullingMask = 1 << capaVisor;
        visorCam.fieldOfView = campoVision;
        visorCam.nearClipPlane = 0.1f;
        visorCam.farClipPlane = 200f;
        visorCam.allowHDR = false;
        visorCam.allowMSAA = false;
        visorCam.depth = -10f;
        visorCam.targetTexture = rt;
        visorCam.enabled = false;

        if (crearLuzPropia)
        {
            GameObject luzGO = new GameObject("VisorLuz");
            luzGO.transform.SetParent(rig.transform, false);
            luzGO.transform.localRotation = Quaternion.Euler(40f, -30f, 0f);
            Light l = luzGO.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.1f;
            l.shadows = LightShadows.None;
            l.cullingMask = 1 << capaVisor;
        }
    }

    private void AsignarCapaRecursiva(GameObject go, int capa)
    {
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = capa;
    }

    private void LimpiarVisor()
    {
        interactivo = false;
        arrastrando = false;

        if (visorCam != null) visorCam.targetTexture = null;

        if (imagenVisor != null)
        {
            imagenVisor.texture = null;
            imagenVisor.enabled = false;
        }

        if (rig != null) Destroy(rig);
        rig = null;
        pivot = null;
        visorCam = null;

        if (rt != null)
        {
            rt.Release();
            Destroy(rt);
            rt = null;
        }

        if (maskAlterada && camaraAR != null) camaraAR.cullingMask = maskOriginalAR;
        maskAlterada = false;

        if (panelVisor != null) panelVisor.SetActive(false);
        activo = false;
    }

    private IEnumerator AnimarEscala(float desde, float hasta)
    {
        float t = 0f;
        while (t < duracionAnimacion)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duracionAnimacion));
            if (pivot != null) pivot.localScale = Vector3.one * Mathf.Lerp(desde, hasta, k);
            yield return null;
        }
        if (pivot != null) pivot.localScale = Vector3.one * hasta;
    }

    // ============================================================
    // INPUT
    // ============================================================
    void Update()
    {
        if (!activo || !interactivo || pivot == null || visorCam == null) return;
        if (ConfiguracionManager.MenuAbierto) { arrastrando = false; return; }

        if (Input.touchCount > 0) ManejarToque();
        else ManejarMouse();
    }

    private void ManejarToque()
    {
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
                arrastrando = !PunteroSobreBoton(t.position);

            if (t.phase == TouchPhase.Moved && arrastrando)
                Rotar(t.deltaPosition);

            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                arrastrando = false;
        }
        else if (Input.touchCount >= 2)
        {
            arrastrando = false;
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            float distPrev = ((t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition)).magnitude;
            float distAct = (t0.position - t1.position).magnitude;

            AplicarZoom((distAct - distPrev) * velocidadZoomTactil);
        }
    }

    private void ManejarMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            arrastrando = !PunteroSobreBoton(Input.mousePosition);
            ultimaPosMouse = Input.mousePosition;
        }

        if (Input.GetMouseButton(0) && arrastrando)
        {
            Vector2 pos = Input.mousePosition;
            Rotar(pos - ultimaPosMouse);
            ultimaPosMouse = pos;
        }

        if (Input.GetMouseButtonUp(0)) arrastrando = false;

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f) AplicarZoom(scroll * velocidadZoomMouse);
    }

    private void Rotar(Vector2 delta)
    {
        Transform cam = visorCam.transform;
        pivot.Rotate(cam.up, -delta.x * velocidadRotacion, Space.World);
        pivot.Rotate(cam.right, delta.y * velocidadRotacion, Space.World);
    }

    private void AplicarZoom(float delta)
    {
        zoomActual = Mathf.Clamp(zoomActual + delta, zoomMin, zoomMax);
        pivot.localScale = Vector3.one * (escalaBase * zoomActual);
    }

    /// <summary>
    /// True solo si el toque cae sobre un elemento interactivo (botón, slider...).
    /// El fondo del panel y la imagen del visor NO bloquean la rotación.
    /// </summary>
    private bool PunteroSobreBoton(Vector2 posicionPantalla)
    {
        if (EventSystem.current == null) return false;

        PointerEventData ev = new PointerEventData(EventSystem.current) { position = posicionPantalla };
        resultadosRaycast.Clear();
        EventSystem.current.RaycastAll(ev, resultadosRaycast);

        foreach (RaycastResult r in resultadosRaycast)
        {
            if (r.gameObject.GetComponentInParent<Selectable>() != null)
                return true;
        }
        return false;
    }
}