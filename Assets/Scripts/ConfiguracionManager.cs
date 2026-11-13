using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Video;
using UnityEngine.Playables;
using TMPro;
using UnityEngine.XR.ARFoundation;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ConfiguracionManager : MonoBehaviour
{
    public static bool MenuAbierto { get; private set; }

    [Header("=== AR ===")]
    [SerializeField] private ARSession arSession;
    [SerializeField] private Camera arCamera;
    [SerializeField] private bool pausarSesionAR = true;
    [SerializeField] private bool desactivarCamaraAR = false;

    [Header("=== Botones ===")]
    [SerializeField] private Button botonConfig;
    [SerializeField] private Button botonSalir;
    [SerializeField] private Button botonMute;
    [SerializeField] private Button botonCreditos;
    [SerializeField] private Button botonCerrarCreditos;

    [Header("=== Imagen del botón Mute ===")]
    [SerializeField] private Image imagenBotonMute;
    [SerializeField] private Sprite iconoSonidoOn;
    [SerializeField] private Sprite iconoSonidoOff;

    [Header("=== Paneles ===")]
    [SerializeField] private GameObject panelConfiguracion;
    [SerializeField] private GameObject panelCreditos;
    [SerializeField] private GameObject panelSubtitulos;

    [Header("=== Volumen ===")]
    [SerializeField] private Slider sliderVolumen;

    [Header("=== Orden de dibujo ===")]
    [SerializeField] private int ordenBase = 1000;

    [Header("=== DIAGNÓSTICO ===")]
    [SerializeField] private bool logsActivos = true;
    [Tooltip("Resumen cada X segundos (tiempo real). 0 = apagado")]
    [SerializeField] private float logPeriodicoSeg = 0f;
    [SerializeField] private bool logToques = true;

    [Header("=== Créditos ===")]
    [TextArea(3, 10)]
    [SerializeField]
    private string creditos =
        "Experiencia AR\n\n" +
        "Desarrollado por:\n" +
        "- Tu Nombre 1\n" +
        "- Tu Nombre 2\n\n" +
        "Universidad / Empresa\n" +
        "Año";

    private bool muteado = false;
    private float volumenAnterior = 1f;
    private float timeScaleAnterior = 1f;
    private bool audioEstabaPausado = false;
    private GameObject bloqueador;
    private float proximoLogPeriodico;

    private const string KEY_VOLUMEN = "cfg_volumen";
    private const string KEY_MUTE = "cfg_mute";
    private const string TAG = "[CFG] ";

    // =========================================================
    // LOG HELPERS
    // =========================================================
    private void Log(string msg)
    {
        if (!logsActivos) return;
        Debug.Log(TAG + "(t=" + Time.unscaledTime.ToString("F2") + " f=" + Time.frameCount + ") " + msg);
    }

    private void LogWarn(string msg)
    {
        Debug.LogWarning(TAG + "(t=" + Time.unscaledTime.ToString("F2") + ") " + msg);
    }

    private static string Ruta(Transform t)
    {
        if (t == null) return "NULL";
        StringBuilder sb = new StringBuilder(t.name);
        while (t.parent != null)
        {
            t = t.parent;
            sb.Insert(0, t.name + "/");
        }
        return sb.ToString();
    }

    private static string Estado(GameObject go)
    {
        if (go == null) return "NULL";
        return go.name + " [activeSelf=" + go.activeSelf + ", activeInHierarchy=" + go.activeInHierarchy +
               ", siblingIndex=" + go.transform.GetSiblingIndex() + "]";
    }

    private void LogCanvasInfo(string etiqueta, GameObject go)
    {
        if (go == null) { Log(etiqueta + ": NULL (no asignado)"); return; }

        Canvas propio = go.GetComponent<Canvas>();
        StringBuilder sb = new StringBuilder();
        sb.Append(etiqueta).Append(": ").Append(Estado(go));
        if (propio != null)
            sb.Append(" | CanvasPropio(override=").Append(propio.overrideSorting)
              .Append(", sortingOrder=").Append(propio.sortingOrder)
              .Append(", isRoot=").Append(propio.isRootCanvas)
              .Append(", mode=").Append(propio.renderMode).Append(")");
        else sb.Append(" | sin Canvas propio");
        Log(sb.ToString());
    }

    private void LogHijos(string etiqueta, Transform padre)
    {
        if (padre == null) return;
        StringBuilder sb = new StringBuilder(etiqueta + " - hijos de " + Ruta(padre) + ":\n");
        for (int i = 0; i < padre.childCount; i++)
        {
            Transform h = padre.GetChild(i);
            Canvas c = h.GetComponent<Canvas>();
            sb.Append("   [").Append(i).Append("] ").Append(h.name)
              .Append(" active=").Append(h.gameObject.activeSelf)
              .Append(c != null ? " | CANVAS(order=" + c.sortingOrder + ", override=" + c.overrideSorting + ")" : "")
              .Append("\n");
        }
        Log(sb.ToString());
    }

    private void LogEstadoGlobal(string cuando)
    {
        StringBuilder sb = new StringBuilder("=== ESTADO GLOBAL (" + cuando + ") ===\n");
        sb.Append("   MenuAbierto=").Append(MenuAbierto).Append("\n");
        sb.Append("   Time.timeScale=").Append(Time.timeScale).Append("\n");
        sb.Append("   AudioListener.pause=").Append(AudioListener.pause).Append("\n");
        sb.Append("   AudioListener.volume=").Append(AudioListener.volume).Append("\n");
        sb.Append("   muteado=").Append(muteado).Append(" volumenAnterior=").Append(volumenAnterior).Append("\n");
        sb.Append("   ARSession.enabled=").Append(arSession != null ? arSession.enabled.ToString() : "NULL").Append("\n");
        sb.Append("   Bloqueador=").Append(Estado(bloqueador)).Append("\n");
        sb.Append("   panelConfiguracion=").Append(Estado(panelConfiguracion)).Append("\n");
        sb.Append("   panelCreditos=").Append(Estado(panelCreditos)).Append("\n");
        sb.Append("   panelSubtitulos=").Append(Estado(panelSubtitulos)).Append("\n");
        Log(sb.ToString());
    }

    private void LogFuentesDeTiempo(string cuando)
    {
        StringBuilder sb = new StringBuilder("=== AUDIO/TIEMPO (" + cuando + ") ===\n");

        AudioSource[] audios = FindObjectsOfType<AudioSource>(true);
        int sonando = 0;
        foreach (var a in audios)
        {
            if (a.isPlaying || a.time > 0f)
            {
                if (a.isPlaying) sonando++;
                sb.Append("   ").Append(Ruta(a.transform))
                  .Append(" | clip=").Append(a.clip != null ? a.clip.name : "null")
                  .Append(" | isPlaying=").Append(a.isPlaying)
                  .Append(" | time=").Append(a.time.ToString("F2"))
                  .Append(" | ignoreListenerPause=").Append(a.ignoreListenerPause).Append("\n");
            }
        }
        sb.Append("AudioSources totales=").Append(audios.Length).Append(" sonando=").Append(sonando).Append("\n");
        sb.Append("VideoPlayers=").Append(FindObjectsOfType<VideoPlayer>(true).Length)
          .Append(" PlayableDirectors=").Append(FindObjectsOfType<PlayableDirector>(true).Length)
          .Append(" Animators=").Append(FindObjectsOfType<Animator>(true).Length).Append("\n");
        Log(sb.ToString());
    }

    private void LogRaycastEnPosicion(Vector2 pos)
    {
        if (EventSystem.current == null) { LogWarn("TOQUE: no hay EventSystem"); return; }

        PointerEventData ev = new PointerEventData(EventSystem.current) { position = pos };
        List<RaycastResult> res = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ev, res);

        StringBuilder sb = new StringBuilder("TOQUE en " + pos + " -> " + res.Count + " objetos (el #0 recibe el toque):\n");
        for (int i = 0; i < res.Count; i++)
        {
            RaycastResult r = res[i];
            sb.Append("   #").Append(i).Append(" ").Append(Ruta(r.gameObject.transform))
              .Append(" | sortingOrder=").Append(r.sortingOrder)
              .Append(" | depth=").Append(r.depth).Append("\n");
        }
        if (res.Count == 0) sb.Append("   (nada de UI bajo el toque)\n");
        Log(sb.ToString());
    }

    // =========================================================
    // CICLO DE VIDA
    // =========================================================
    private void Awake()
    {
        Log("Awake() - script en " + Ruta(transform));
    }

    private void Start()
    {
        Log("########## START ##########");
        MenuAbierto = false;

        volumenAnterior = PlayerPrefs.GetFloat(KEY_VOLUMEN, 1f);
        muteado = PlayerPrefs.GetInt(KEY_MUTE, 0) == 1;
        AudioListener.volume = muteado ? 0f : volumenAnterior;
        Log("Config cargada: volumen=" + volumenAnterior + " muteado=" + muteado);

        if (panelSubtitulos == null) LogWarn("panelSubtitulos NO asignado (solo para diagnóstico)");
        if (EventSystem.current == null) LogWarn("NO HAY EventSystem activo en la escena.");

        CrearBloqueador();
        ConfigurarCanvas(panelConfiguracion, ordenBase + 1, "panelConfiguracion");
        ConfigurarCanvas(panelCreditos, ordenBase + 2, "panelCreditos");

        // Estado inicial
        if (panelConfiguracion != null) panelConfiguracion.SetActive(false);
        if (panelCreditos != null) panelCreditos.SetActive(false);
        if (botonConfig != null) botonConfig.gameObject.SetActive(true);
        if (botonSalir != null) botonSalir.gameObject.SetActive(false);
        if (bloqueador != null) bloqueador.SetActive(false);

        ActualizarIconoMute();

        if (panelCreditos != null)
        {
            TextMeshProUGUI texto = panelCreditos.GetComponentInChildren<TextMeshProUGUI>(true);
            if (texto != null) texto.text = creditos;
            else LogWarn("panelCreditos no tiene TextMeshProUGUI hijo");
        }

        if (sliderVolumen != null)
        {
            sliderVolumen.minValue = 0f;
            sliderVolumen.maxValue = 1f;
            sliderVolumen.value = muteado ? 0f : volumenAnterior;
            sliderVolumen.onValueChanged.AddListener(OnVolumenCambiado);
        }
        else LogWarn("sliderVolumen NO asignado");

        AsignarBoton(botonConfig, AbrirMenu, "botonConfig");
        AsignarBoton(botonSalir, CerrarMenu, "botonSalir");
        AsignarBoton(botonMute, ToggleMute, "botonMute");
        AsignarBoton(botonCreditos, AbrirCreditos, "botonCreditos");
        AsignarBoton(botonCerrarCreditos, CerrarCreditos, "botonCerrarCreditos");

        LogCanvasInfo("Tras configurar -> panelConfiguracion", panelConfiguracion);
        LogCanvasInfo("Tras configurar -> panelCreditos", panelCreditos);
        LogCanvasInfo("Tras configurar -> bloqueador", bloqueador);
        Log("########## FIN START ##########");
    }

    private void AsignarBoton(Button b, UnityEngine.Events.UnityAction accion, string nombre)
    {
        if (b == null) { LogWarn("Botón '" + nombre + "' NO asignado en el Inspector"); return; }
        b.onClick.AddListener(accion);
        Log("Botón '" + nombre + "' enlazado -> " + Ruta(b.transform));
    }

    private void Update()
    {
        if (logToques && logsActivos && HayToque(out Vector2 pos))
            LogRaycastEnPosicion(pos);

        if (logsActivos && logPeriodicoSeg > 0f && Time.unscaledTime >= proximoLogPeriodico)
        {
            proximoLogPeriodico = Time.unscaledTime + logPeriodicoSeg;
            Log("[tick] MenuAbierto=" + MenuAbierto + " timeScale=" + Time.timeScale +
                " audioPause=" + AudioListener.pause);
        }
    }

    private bool HayToque(out Vector2 pos)
    {
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            pos = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pos = Mouse.current.position.ReadValue();
            return true;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0)) { pos = Input.mousePosition; return true; }
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            pos = Input.GetTouch(0).position;
            return true;
        }
#endif
        pos = default;
        return false;
    }

    private void OnDestroy()
    {
        if (MenuAbierto) RestaurarEstado();

        if (sliderVolumen != null) sliderVolumen.onValueChanged.RemoveListener(OnVolumenCambiado);
        if (botonConfig != null) botonConfig.onClick.RemoveListener(AbrirMenu);
        if (botonSalir != null) botonSalir.onClick.RemoveListener(CerrarMenu);
        if (botonMute != null) botonMute.onClick.RemoveListener(ToggleMute);
        if (botonCreditos != null) botonCreditos.onClick.RemoveListener(AbrirCreditos);
        if (botonCerrarCreditos != null) botonCerrarCreditos.onClick.RemoveListener(CerrarCreditos);
    }

    [ContextMenu("Volcar diagnóstico ahora")]
    public void VolcarDiagnostico()
    {
        LogEstadoGlobal("manual");
        LogCanvasInfo("panelSubtitulos", panelSubtitulos);
        LogCanvasInfo("panelConfiguracion", panelConfiguracion);
        LogCanvasInfo("panelCreditos", panelCreditos);
        LogCanvasInfo("bloqueador", bloqueador);
        if (panelConfiguracion != null) LogHijos("MANUAL", panelConfiguracion.transform.parent);
        LogFuentesDeTiempo("manual");
    }

    // =========================================================
    // CANVAS / BLOQUEADOR
    // =========================================================

    private void AplicarOrden(GameObject objeto, int orden)
    {
        Canvas c = objeto.GetComponent<Canvas>();
        if (c == null) c = objeto.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = orden;

        if (objeto.GetComponent<GraphicRaycaster>() == null)
            objeto.AddComponent<GraphicRaycaster>();
    }

    private void ConfigurarCanvas(GameObject objeto, int orden, string nombre)
    {
        if (objeto == null) { LogWarn("ConfigurarCanvas: '" + nombre + "' es NULL"); return; }

        bool estabaActivo = objeto.activeSelf;
        if (!estabaActivo) objeto.SetActive(true);

        AplicarOrden(objeto, orden);

        Canvas c = objeto.GetComponent<Canvas>();
        Log("ConfigurarCanvas('" + nombre + "') -> override=" + c.overrideSorting + ", sortingOrder=" + c.sortingOrder);

        if (!estabaActivo) objeto.SetActive(false);
    }

    private void CrearBloqueador()
    {
        Transform padre = null;
        if (panelConfiguracion != null) padre = panelConfiguracion.transform.parent;
        if (padre == null && panelCreditos != null) padre = panelCreditos.transform.parent;
        if (padre == null)
        {
            LogWarn("No se pudo crear el bloqueador: falta asignar los paneles.");
            return;
        }

        bloqueador = new GameObject("BloqueadorMenu", typeof(RectTransform));
        bloqueador.transform.SetParent(padre, false);

        RectTransform rtB = bloqueador.GetComponent<RectTransform>();
        rtB.anchorMin = Vector2.zero;
        rtB.anchorMax = Vector2.one;
        rtB.offsetMin = Vector2.zero;
        rtB.offsetMax = Vector2.zero;

        AplicarOrden(bloqueador, ordenBase);

        Image img = bloqueador.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.5f);
        img.raycastTarget = true;

        Log("Bloqueador creado en " + Ruta(padre) + " con sortingOrder=" + ordenBase);
    }

    // =========================================================
    // MENÚ
    // =========================================================
    public void AbrirMenu()
    {
        Log(">>>>>> AbrirMenu() <<<<<<");

        if (bloqueador != null)
        {
            bloqueador.SetActive(true);
            AplicarOrden(bloqueador, ordenBase);
        }

        if (panelConfiguracion != null)
        {
            panelConfiguracion.SetActive(true);
            AplicarOrden(panelConfiguracion, ordenBase + 1);
        }
        if (panelCreditos != null) panelCreditos.SetActive(false);

        if (botonConfig != null) botonConfig.gameObject.SetActive(false);
        if (botonSalir != null) botonSalir.gameObject.SetActive(true);

        PausarTodo(true);

        LogCanvasInfo("AL ABRIR -> bloqueador", bloqueador);
        LogCanvasInfo("AL ABRIR -> panelConfiguracion", panelConfiguracion);
        LogEstadoGlobal("después de abrir");
        LogFuentesDeTiempo("al abrir menú");
    }

    public void CerrarMenu()
    {
        Log(">>>>>> CerrarMenu() <<<<<<");

        if (panelConfiguracion != null) panelConfiguracion.SetActive(false);
        if (panelCreditos != null) panelCreditos.SetActive(false);
        if (bloqueador != null) bloqueador.SetActive(false);
        if (botonConfig != null) botonConfig.gameObject.SetActive(true);
        if (botonSalir != null) botonSalir.gameObject.SetActive(false);

        PausarTodo(false);

        LogEstadoGlobal("después de cerrar");
        LogFuentesDeTiempo("al cerrar menú");
    }

    public void AbrirCreditos()
    {
        Log(">>>>>> AbrirCreditos() <<<<<<");

        // 🔥 AQUÍ ESTÁ EL CAMBIO: Se eliminó la línea que ocultaba el panelConfiguracion

        if (panelCreditos != null)
        {
            panelCreditos.SetActive(true);
            AplicarOrden(panelCreditos, ordenBase + 2);
        }
        LogCanvasInfo("AL ABRIR CRÉDITOS -> panelCreditos", panelCreditos);
    }

    public void CerrarCreditos()
    {
        Log(">>>>>> CerrarCreditos() <<<<<<");
        if (panelCreditos != null) panelCreditos.SetActive(false);

        // Nos aseguramos de que la configuración vuelva a quedar al frente al cerrar créditos
        if (panelConfiguracion != null)
        {
            AplicarOrden(panelConfiguracion, ordenBase + 1);
        }
    }

    // =========================================================
    // PAUSA GENERAL
    // =========================================================
    private void PausarTodo(bool pausar)
    {
        Log("PausarTodo(" + pausar + ")");

        if (pausar)
        {
            if (MenuAbierto) { LogWarn("PausarTodo(true) ignorado: ya estaba abierto"); return; }

            MenuAbierto = true;

            audioEstabaPausado = AudioListener.pause;
            AudioListener.pause = true;

            timeScaleAnterior = Time.timeScale;
            Time.timeScale = 0f;

            PausarAR(true);
        }
        else
        {
            RestaurarEstado();
        }
    }

    private void RestaurarEstado()
    {
        MenuAbierto = false;

        AudioListener.pause = audioEstabaPausado;
        Time.timeScale = timeScaleAnterior;

        Log("Restaurado: timeScale=" + Time.timeScale + ", AudioListener.pause=" + AudioListener.pause);
        PausarAR(false);
    }

    private void PausarAR(bool pausar)
    {
        if (pausarSesionAR && arSession != null) arSession.enabled = !pausar;
        if (desactivarCamaraAR && arCamera != null) arCamera.enabled = !pausar;
    }

    // =========================================================
    // VOLUMEN / MUTE
    // =========================================================
    private void OnVolumenCambiado(float valor)
    {
        Log("Slider volumen -> " + valor.ToString("F2"));

        if (muteado && valor > 0f)
        {
            muteado = false;
            PlayerPrefs.SetInt(KEY_MUTE, 0);
            ActualizarIconoMute();
        }

        volumenAnterior = valor;
        AudioListener.volume = valor;

        PlayerPrefs.SetFloat(KEY_VOLUMEN, volumenAnterior);
        PlayerPrefs.Save();
    }

    public void ToggleMute()
    {
        muteado = !muteado;
        Log("ToggleMute() -> muteado=" + muteado);

        if (muteado)
        {
            if (sliderVolumen != null && sliderVolumen.value > 0f)
                volumenAnterior = sliderVolumen.value;

            AudioListener.volume = 0f;
            if (sliderVolumen != null) sliderVolumen.SetValueWithoutNotify(0f);
        }
        else
        {
            AudioListener.volume = volumenAnterior;
            if (sliderVolumen != null) sliderVolumen.SetValueWithoutNotify(volumenAnterior);
        }

        ActualizarIconoMute();
        PlayerPrefs.SetInt(KEY_MUTE, muteado ? 1 : 0);
        PlayerPrefs.SetFloat(KEY_VOLUMEN, volumenAnterior);
        PlayerPrefs.Save();
    }

    private void ActualizarIconoMute()
    {
        if (imagenBotonMute == null) return;
        imagenBotonMute.sprite = muteado ? iconoSonidoOff : iconoSonidoOn;
    }
}