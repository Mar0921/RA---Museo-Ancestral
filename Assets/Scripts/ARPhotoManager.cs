using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using UnityEngine.XR.ARFoundation;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class ARPhotoManager : MonoBehaviour
{
    private enum Estado { Cerrado, Captura, Preview }

    [Header("Panel raíz")]
    [Tooltip("'Panel de fotos' (padre de marco, tomarfoto, descargar, etc.)")]
    public GameObject panelFotos;

    [Header("Modo Captura")]
    public GameObject marco;
    public Button botonTomarFoto;                 // tomarfoto
    [Tooltip("Botón para salir del modo foto (duplica 'reintentar' y llámalo 'cerrar')")]
    public Button botonCerrar;

    [Header("Modo Preview")]
    public GameObject previsualizacion;
    public RawImage previewImage;                 // imagen dentro de previsualizacion
    public Button botonDescargar;                 // descargar
    public Button botonReintentar;                // reintentar

    [Header("Ocultar al entrar al modo foto")]
    [Tooltip("Apaga todo lo que esté al lado de 'Panel de fotos' en el Canvas")]
    public bool ocultarHermanosDelPanel = true;
    [Tooltip("Objetos extra a ocultar (ej: AR Default Plane, modelos 3D sueltos)")]
    public List<GameObject> objetosExtraAOcultar = new List<GameObject>();
    [Tooltip("Oculta lo visual de todos los PinMapa de la escena")]
    public bool ocultarPines = true;
    [Tooltip("Opcional: ARPlaneManager del XR Origin")]
    public ARPlaneManager planeManager;

    [Header("Efectos")]
    public AudioSource shutterSound;

    private Estado estado = Estado.Cerrado;
    private bool capturando = false;
    private Texture2D fotoActual;

    private readonly Dictionary<GameObject, bool> estadoOriginal = new Dictionary<GameObject, bool>();
    private readonly List<Renderer> renderersOcultos = new List<Renderer>();
    private readonly List<Canvas> canvasOcultos = new List<Canvas>();
    private bool planosOcultos = false;

    // ============================================================
    // INICIO
    // ============================================================

    void Start()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
                Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }

        Conectar(botonTomarFoto, () => StartCoroutine(CapturePhoto()));
        Conectar(botonCerrar, CerrarModoFoto);
        Conectar(botonDescargar, DescargarFotoActual);
        Conectar(botonReintentar, Reintentar);

        AplicarEstado(Estado.Cerrado);
    }

    private void Conectar(Button b, UnityEngine.Events.UnityAction accion)
    {
        if (b == null) return;
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(accion);
    }

    // ============================================================
    // ENTRAR / SALIR DEL MODO FOTO
    // ============================================================

    /// <summary>Lo llama BotonpanelfotoAbrir (vía DesbloqueoRecompensa).</summary>
    public void AbrirModoFoto()
    {
        if (estado != Estado.Cerrado) return;

        if (panelFotos == null)
        {
            Debug.LogError("[ARPhotoManager] Falta asignar 'panelFotos'.");
            return;
        }

        OcultarEscena();
        CambiarEstado(Estado.Captura);
        Debug.Log("[ARPhotoManager] Modo foto abierto.");
    }

    public void CerrarModoFoto()
    {
        if (estado == Estado.Cerrado) return;

        LiberarFotoActual();
        CambiarEstado(Estado.Cerrado);
        RestaurarEscena();
        Debug.Log("[ARPhotoManager] Modo foto cerrado, escena restaurada.");
    }

    // ============================================================
    // OCULTAR / RESTAURAR
    // ============================================================

    private void OcultarEscena()
    {
        estadoOriginal.Clear();
        renderersOcultos.Clear();
        canvasOcultos.Clear();

        // 1) Hermanos de 'Panel de fotos' y de sus ancestros, hasta el Canvas
        if (ocultarHermanosDelPanel)
        {
            Transform actual = panelFotos.transform;
            while (actual.parent != null)
            {
                Transform padre = actual.parent;
                foreach (Transform hijo in padre)
                {
                    if (hijo != actual) Registrar(hijo.gameObject);
                }
                if (padre.GetComponent<Canvas>() != null) break;
                actual = padre;
            }
        }

        // 2) Objetos extra
        foreach (var obj in objetosExtraAOcultar)
            if (obj != null) Registrar(obj);

        // 3) Pines (solo lo visual, sin apagar sus scripts)
        if (ocultarPines)
        {
            PinMapa[] pines = FindObjectsByType<PinMapa>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var pin in pines)
            {
                foreach (var r in pin.GetComponentsInChildren<Renderer>(false))
                    if (r.enabled) { r.enabled = false; renderersOcultos.Add(r); }

                foreach (var c in pin.GetComponentsInChildren<Canvas>(false))
                    if (c.enabled) { c.enabled = false; canvasOcultos.Add(c); }
            }
        }

        // 4) Planos AR detectados
        if (planeManager != null)
        {
            planeManager.SetTrackablesActive(false);
            planosOcultos = true;
        }
    }

    private void Registrar(GameObject go)
    {
        if (go == null || go == panelFotos) return;
        if (panelFotos.transform.IsChildOf(go.transform)) return; // nunca apagar un ancestro del panel
        if (estadoOriginal.ContainsKey(go)) return;

        estadoOriginal[go] = go.activeSelf;
        if (go.activeSelf) go.SetActive(false);
    }

    private void RestaurarEscena()
    {
        foreach (var par in estadoOriginal)
            if (par.Key != null) par.Key.SetActive(par.Value);
        estadoOriginal.Clear();

        foreach (var r in renderersOcultos) if (r != null) r.enabled = true;
        renderersOcultos.Clear();

        foreach (var c in canvasOcultos) if (c != null) c.enabled = true;
        canvasOcultos.Clear();

        if (planosOcultos && planeManager != null)
            planeManager.SetTrackablesActive(true);
        planosOcultos = false;
    }

    // ============================================================
    // ESTADOS DE LA UI
    // ============================================================

    private void CambiarEstado(Estado nuevo)
    {
        estado = nuevo;
        AplicarEstado(nuevo);
    }

    private void AplicarEstado(Estado e)
    {
        bool abierto = e != Estado.Cerrado;
        bool captura = e == Estado.Captura;
        bool preview = e == Estado.Preview;

        SetActivo(panelFotos, abierto);

        // Captura
        SetActivo(marco, captura);
        SetActivo(botonTomarFoto, captura);

        // El botón cerrar sirve tanto en captura como en preview
        SetActivo(botonCerrar, captura || preview);

        // Preview
        SetActivo(previsualizacion, preview);
        SetActivo(botonDescargar, preview);
        SetActivo(botonReintentar, preview);
    }

    private void SetActivo(GameObject go, bool activo)
    {
        if (go != null && go.activeSelf != activo) go.SetActive(activo);
    }

    private void SetActivo(Button b, bool activo)
    {
        if (b != null) SetActivo(b.gameObject, activo);
    }

    // ============================================================
    // CAPTURA
    // ============================================================

    public IEnumerator CapturePhoto()
    {
        if (capturando || estado != Estado.Captura) yield break;
        capturando = true;

        // Oculta solo los botones; el marco queda visible y sale en la foto
        SetActivo(botonTomarFoto, false);
        SetActivo(botonCerrar, false);

        yield return null;
        yield return new WaitForEndOfFrame();

        Texture2D foto = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        foto.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        foto.Apply();

        if (shutterSound != null) shutterSound.Play();

        LiberarFotoActual();
        fotoActual = foto;

        if (previewImage != null) previewImage.texture = foto;

        capturando = false;
        CambiarEstado(Estado.Preview);
    }

    // ============================================================
    // REINTENTAR
    // ============================================================

    private void Reintentar()
    {
        LiberarFotoActual();
        CambiarEstado(Estado.Captura);
    }

    private void LiberarFotoActual()
    {
        if (previewImage != null) previewImage.texture = null;
        if (fotoActual != null)
        {
            Destroy(fotoActual);
            fotoActual = null;
        }
    }

    // ============================================================
    // DESCARGA
    // ============================================================

    private void DescargarFotoActual()
    {
        if (fotoActual == null) return;

        string fileName = "ARPhoto_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(path, fotoActual.EncodeToPNG());

#if UNITY_ANDROID
        try
        {
            string galleryDir = "/storage/emulated/0/DCIM/ARPhotos/";
            Directory.CreateDirectory(galleryDir);
            File.Copy(path, Path.Combine(galleryDir, fileName), true);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[ARPhotoManager] No se pudo copiar a la galería: " + ex.Message);
        }
#endif

        Debug.Log("[ARPhotoManager] Foto descargada: " + path);
    }
}