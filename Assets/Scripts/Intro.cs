using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Intro : MonoBehaviour
{
    [Serializable]
    public class Diapositiva
    {
        [Tooltip("Opcional. Si está vacío, el título se oculta.")]
        public string titulo;
        [TextArea(2, 6)] public string cuerpo;
    }

    [Header("Escena")]
    [SerializeField] private string nombreEscenaPrincipal = "Principal";

    [Header("Objetos del Canvas - Intro")]
    [SerializeField] private GameObject oscuro;
    [SerializeField] private GameObject logo;
    [SerializeField] private GameObject internet;
    [SerializeField] private GameObject grupoCarga;
    [SerializeField] private Image relleno;
    [SerializeField] private TextMeshProUGUI textoProgreso;
    [SerializeField] private RectTransform pajaro;

    [Header("Objetos del Canvas - Onboarding")]
    [SerializeField] private GameObject panelOnboarding;
    [SerializeField] private GameObject grupoTextos;
    [SerializeField] private TextMeshProUGUI textoTitulo;
    [SerializeField] private TextMeshProUGUI textoCuerpo;
    [SerializeField] private Button botonSiguiente;
    [SerializeField] private Button botonSaltar;
    [SerializeField] private Button botonSonido;
    [SerializeField] private Image iconoSonido;
    [SerializeField] private Sprite spriteSonidoOn;
    [SerializeField] private Sprite spriteSonidoOff;

    [Header("Panel 'Cada historia'")]
    [SerializeField] private GameObject panelHistoria;
    [SerializeField] private Button botonSiguienteHistoria;

    [Header("Panel 'En cada Eco vas a'")]
    [SerializeField] private GameObject panelEco;
    [SerializeField] private Button botonSiguienteEco;
    [Tooltip("Objeto 'checks' (sus hijos son los checks, en orden).")]
    [SerializeField] private Transform contenedorChecks;
    [Tooltip("cuerpo1, cuerpo2, cuerpo3, cuerpo4 en orden.")]
    [SerializeField] private GameObject[] cuerposEco;

    [Header("Panel 'Guía rápida 1'")]
    [SerializeField] private GameObject panelGuia1;
    [SerializeField] private Button botonSiguienteGuia1;

    [Header("Panel 'Guía rápida 2'")]
    [SerializeField] private GameObject panelGuia2;
    [SerializeField] private Button botonEntendido;
    [SerializeField] private Button botonAtras;

    [Header("Panel 'Listo para iniciar'")]
    [Tooltip("Objeto 'listo para iniciar'.")]
    [SerializeField] private GameObject panelListo;
    [Tooltip("Botón 'iniciar' dentro de 'listo para iniciar'.")]
    [SerializeField] private Button botonIniciar;

    [Header("Panel 'Para escanear'")]
    [Tooltip("Objeto 'para escanear'.")]
    [SerializeField] private GameObject panelEscanear;
    [Tooltip("Botón 'entendido' dentro de 'para escanear'.")]
    [SerializeField] private Button botonEntendidoEscanear;

    [Header("Panel 'Ten presente' (último)")]
    [Tooltip("Objeto 'ten presente'.")]
    [SerializeField] private GameObject panelPresente;
    [Tooltip("Botón 'siguiente' dentro de 'ten presente'. Carga la escena principal.")]
    [SerializeField] private Button botonSiguientePresente;
    [Tooltip("Opcional. Objeto 'con luz': aparece con fundido.")]
    [SerializeField] private GameObject conLuz;
    [Tooltip("Opcional. Objeto 'sin luz': se desvanece mientras 'con luz' aparece.")]
    [SerializeField] private GameObject sinLuz;
    [SerializeField] private float retardoLuz = 0.6f;
    [SerializeField] private float duracionLuz = 0.8f;

    [Header("Puntos indicadores")]
    [SerializeField] private Image[] puntos;
    [SerializeField] private Sprite spritePuntoActivo;
    [SerializeField] private Sprite spritePuntoInactivo;
    [SerializeField] private Color colorPuntoActivo = new Color(0.61f, 0.48f, 0.24f);
    [SerializeField] private Color colorPuntoInactivo = new Color(0.93f, 0.89f, 0.78f);

    [Header("Diapositivas")]
    [SerializeField]
    private Diapositiva[] diapositivas = new Diapositiva[]
    {
        new Diapositiva {
            titulo = "Te doy la Bienvenida a\nEcos del Territorio",
            cuerpo = "Soy el espíritu guía y voy a\nacompañarte en este recorrido"
        },
        new Diapositiva {
            titulo = "",
            cuerpo = "Nací de las historias que se contaban\njunto al fogón:\nlas de los abuelos, los pescadores\ny los que volvían del monte."
        },
        new Diapositiva {
            titulo = "",
            cuerpo = "Mientras alguien las contaba, el río,\nel monte\ny los caminos seguían vivos."
        },
        new Diapositiva {
            titulo = "",
            cuerpo = "Pero cada vez menos personas\nlas cuentan,\ny lo que se olvida se apaga.\nAntes de que eso pase,\nquiero que me ayudes a encontrarlas."
        }
    };

    [Header("Ave")]
    [SerializeField] private float margenSuperior = 180f;
    [Range(0.1f, 1f)]
    [SerializeField] private float fraccionVisibleInicio = 0.6f;
    [SerializeField] private float amplitudOscilacion = 25f;
    [SerializeField] private float frecuenciaOscilacion = 2f;

    [Header("Tiempos (segundos)")]
    [SerializeField] private float pausaPanelOscuro = 0.8f;
    [SerializeField] private float duracionFundidoLogo = 1.2f;
    [SerializeField] private float pausaLogoSolo = 0.8f;
    [SerializeField] private float duracionFundidoTextos = 0.6f;
    [SerializeField] private float duracionCarga = 3.5f;
    [SerializeField] private float pausaTrasCarga = 0.4f;
    [SerializeField] private float duracionFundidoSalidaLogo = 1f;
    [SerializeField] private float pausaOscuroAntesAve = 0.6f;
    [SerializeField] private float duracionVueloAve = 3.5f;
    [SerializeField] private float pausaAveAntesOnboarding = 0.3f;
    [SerializeField] private float duracionFundidoOnboarding = 0.8f;
    [SerializeField] private float duracionSalidaTexto = 0.3f;
    [SerializeField] private float duracionEntradaTexto = 0.45f;
    [SerializeField] private float duracionFundidoPanel = 0.9f;
    [SerializeField] private float pausaAntesDeItems = 0.25f;
    [SerializeField] private float retardoEntreItems = 0.45f;
    [SerializeField] private float duracionItem = 0.4f;

    [Header("Curvas")]
    [SerializeField] private AnimationCurve curvaFundido = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve curvaVuelo = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Textos")]
    [SerializeField] private string mensajeCarga = "Preparando el territorio...";

    private CanvasGroup cgOscuro, cgLogo, cgInternet, cgCarga, cgPajaro,
                        cgOnboarding, cgTextos, cgHistoria, cgEco, cgGuia1, cgGuia2,
                        cgListo, cgEscanear, cgPresente, cgConLuz, cgSinLuz;

    private readonly List<CanvasGroup> itemsEco = new List<CanvasGroup>();

    private int indiceActual = 0;
    private bool transicionando = false;
    private bool sonidoActivo = true;

    private void Start()
    {
        PrepararEstadoInicial();
        ConectarBotones();
        StartCoroutine(SecuenciaIntro());
    }

    // ---------- Estado inicial ----------
    private void PrepararEstadoInicial()
    {
        cgOscuro = ObtenerGrupo(oscuro);
        cgLogo = ObtenerGrupo(logo);
        cgInternet = ObtenerGrupo(internet);
        cgCarga = ObtenerGrupo(grupoCarga);
        cgPajaro = pajaro != null ? ObtenerGrupo(pajaro.gameObject) : null;
        cgOnboarding = ObtenerGrupo(panelOnboarding);
        cgTextos = ObtenerGrupo(grupoTextos);
        cgHistoria = ObtenerGrupo(panelHistoria);
        cgEco = ObtenerGrupo(panelEco);
        cgGuia1 = ObtenerGrupo(panelGuia1);
        cgGuia2 = ObtenerGrupo(panelGuia2);
        cgListo = ObtenerGrupo(panelListo);
        cgEscanear = ObtenerGrupo(panelEscanear);
        cgPresente = ObtenerGrupo(panelPresente);
        cgConLuz = ObtenerGrupo(conLuz);
        cgSinLuz = ObtenerGrupo(sinLuz);

        GameObject[] aActivar =
        {
            oscuro, logo, internet, grupoCarga, panelOnboarding, grupoTextos,
            panelHistoria, panelEco, panelGuia1, panelGuia2,
            panelListo, panelEscanear, panelPresente, conLuz, sinLuz
        };
        foreach (GameObject g in aActivar)
            if (g != null) g.SetActive(true);

        Fijar(cgOscuro, 1f);
        Fijar(cgLogo, 0f);
        Fijar(cgInternet, 0f);
        Fijar(cgCarga, 0f);
        Fijar(cgPajaro, 0f);
        Fijar(cgTextos, 1f);
        Fijar(cgConLuz, 0f);
        Fijar(cgSinLuz, 1f);

        // Onboarding y paneles posteriores: invisibles y sin responder a toques
        OcultarPanel(cgOnboarding);
        OcultarPanel(cgHistoria);
        OcultarPanel(cgEco);
        OcultarPanel(cgGuia1);
        OcultarPanel(cgGuia2);
        OcultarPanel(cgListo);
        OcultarPanel(cgEscanear);
        OcultarPanel(cgPresente);

        if (cgPajaro != null) cgPajaro.blocksRaycasts = false;

        PrepararItemsEco();

        if (relleno != null) relleno.fillAmount = 0f;
        if (textoProgreso != null) textoProgreso.text = $"{mensajeCarga} 0%";
        if (pajaro != null) pajaro.gameObject.SetActive(false);

        MostrarDiapositiva(0);
        ActualizarIconoSonido();
    }

    // check1, cuerpo1, check2, cuerpo2, ...
    private void PrepararItemsEco()
    {
        itemsEco.Clear();

        int nChecks = contenedorChecks != null ? contenedorChecks.childCount : 0;
        int nCuerpos = cuerposEco != null ? cuerposEco.Length : 0;
        int n = Mathf.Max(nChecks, nCuerpos);

        for (int i = 0; i < n; i++)
        {
            if (i < nChecks)
            {
                CanvasGroup c = ObtenerGrupo(contenedorChecks.GetChild(i).gameObject);
                if (c != null) { c.alpha = 0f; itemsEco.Add(c); }
            }
            if (i < nCuerpos && cuerposEco[i] != null)
            {
                CanvasGroup c = ObtenerGrupo(cuerposEco[i]);
                if (c != null) { c.alpha = 0f; itemsEco.Add(c); }
            }
        }
    }

    private void ConectarBotones()
    {
        if (botonSiguiente != null) botonSiguiente.onClick.AddListener(OnSiguiente);
        if (botonSaltar != null) botonSaltar.onClick.AddListener(Empezar);
        if (botonSonido != null) botonSonido.onClick.AddListener(AlternarSonido);
        if (botonSiguienteHistoria != null) botonSiguienteHistoria.onClick.AddListener(OnSiguienteHistoria);
        if (botonSiguienteEco != null) botonSiguienteEco.onClick.AddListener(OnSiguienteEco);
        if (botonSiguienteGuia1 != null) botonSiguienteGuia1.onClick.AddListener(OnSiguienteGuia1);
        if (botonAtras != null) botonAtras.onClick.AddListener(OnAtrasGuia2);
        if (botonEntendido != null) botonEntendido.onClick.AddListener(OnEntendidoGuia2);
        if (botonIniciar != null) botonIniciar.onClick.AddListener(OnIniciar);
        if (botonEntendidoEscanear != null) botonEntendidoEscanear.onClick.AddListener(OnEntendidoEscanear);
        if (botonSiguientePresente != null) botonSiguientePresente.onClick.AddListener(Empezar);
    }

    // ---------- Secuencia principal ----------
    private IEnumerator SecuenciaIntro()
    {
        yield return new WaitForSeconds(pausaPanelOscuro);

        yield return Fundido(cgLogo, 0f, 1f, duracionFundidoLogo);
        yield return new WaitForSeconds(pausaLogoSolo);

        StartCoroutine(Fundido(cgInternet, 0f, 1f, duracionFundidoTextos));
        yield return Fundido(cgCarga, 0f, 1f, duracionFundidoTextos);

        yield return AnimarCarga();
        yield return new WaitForSeconds(pausaTrasCarga);

        yield return Fundido(cgLogo, 1f, 0f, duracionFundidoSalidaLogo);
        yield return new WaitForSeconds(pausaOscuroAntesAve);

        yield return AnimarAve();
        yield return new WaitForSeconds(pausaAveAntesOnboarding);

        yield return Fundido(cgOnboarding, 0f, 1f, duracionFundidoOnboarding);
        MostrarPanel(cgOnboarding);
    }

    // ---------- Barra de carga ----------
    private IEnumerator AnimarCarga()
    {
        float t = 0f;
        while (t < duracionCarga)
        {
            t += Time.deltaTime;
            float progreso = Mathf.Clamp01(t / duracionCarga);

            if (relleno != null) relleno.fillAmount = progreso;
            if (textoProgreso != null)
                textoProgreso.text = $"{mensajeCarga} {Mathf.RoundToInt(progreso * 100f)}%";

            yield return null;
        }

        if (relleno != null) relleno.fillAmount = 1f;
        if (textoProgreso != null) textoProgreso.text = $"{mensajeCarga} 100%";
    }

    // ---------- Ave ----------
    private IEnumerator AnimarAve()
    {
        if (pajaro == null) yield break;

        pajaro.SetAsLastSibling();
        if (panelOnboarding != null) panelOnboarding.transform.SetAsLastSibling();

        RectTransform padre = pajaro.parent as RectTransform;
        float yMin = padre.rect.yMin;
        float yMax = padre.rect.yMax;

        float alto = pajaro.rect.height * pajaro.localScale.y;
        float pivoteY = pajaro.pivot.y;

        float bordeInferiorInicio = yMin - alto * (1f - fraccionVisibleInicio);
        float yInicio = bordeInferiorInicio + alto * pivoteY;
        float yFin = (yMax - margenSuperior) - alto * (1f - pivoteY);

        Vector3 pos = pajaro.localPosition;
        float x = pos.x;
        float z = pos.z;

        pajaro.localPosition = new Vector3(x, yInicio, z);
        pajaro.gameObject.SetActive(true);

        if (cgPajaro != null)
        {
            cgPajaro.alpha = 0f;
            StartCoroutine(Fundido(cgPajaro, 0f, 1f, 0.5f));
        }

        float t = 0f;
        while (t < duracionVueloAve)
        {
            t += Time.deltaTime;
            float k = curvaVuelo.Evaluate(Mathf.Clamp01(t / duracionVueloAve));

            float y = Mathf.Lerp(yInicio, yFin, k);
            float oscilacion = Mathf.Sin(t * frecuenciaOscilacion) * amplitudOscilacion * (1f - k);

            pajaro.localPosition = new Vector3(x + oscilacion, y, z);
            yield return null;
        }

        pajaro.localPosition = new Vector3(x, yFin, z);
    }

    // ---------- Onboarding ----------
    private void OnSiguiente()
    {
        if (transicionando) return;

        if (indiceActual >= diapositivas.Length - 1)
        {
            if (panelHistoria != null) StartCoroutine(IrAPanelHistoria());
            else Empezar();
            return;
        }

        StartCoroutine(CambiarDiapositiva(indiceActual + 1));
    }

    private IEnumerator CambiarDiapositiva(int nuevoIndice)
    {
        transicionando = true;

        yield return Fundido(cgTextos, 1f, 0f, duracionSalidaTexto);
        MostrarDiapositiva(nuevoIndice);
        yield return Fundido(cgTextos, 0f, 1f, duracionEntradaTexto);

        transicionando = false;
    }

    private void MostrarDiapositiva(int indice)
    {
        if (diapositivas == null || diapositivas.Length == 0) return;

        indiceActual = Mathf.Clamp(indice, 0, diapositivas.Length - 1);
        Diapositiva d = diapositivas[indiceActual];

        if (textoTitulo != null)
        {
            bool hayTitulo = !string.IsNullOrEmpty(d.titulo);
            textoTitulo.gameObject.SetActive(hayTitulo);
            if (hayTitulo) textoTitulo.text = d.titulo;
        }

        if (textoCuerpo != null) textoCuerpo.text = d.cuerpo;

        ActualizarPuntos();
    }

    private void ActualizarPuntos()
    {
        if (puntos == null) return;

        for (int i = 0; i < puntos.Length; i++)
        {
            if (puntos[i] == null) continue;

            bool activo = (i == indiceActual);
            Sprite sprite = activo ? spritePuntoActivo : spritePuntoInactivo;

            if (sprite != null)
            {
                puntos[i].sprite = sprite;
                puntos[i].color = Color.white;
            }
            else
            {
                puntos[i].color = activo ? colorPuntoActivo : colorPuntoInactivo;
            }
        }
    }

    // ---------- Panel "Cada historia" ----------
    private IEnumerator IrAPanelHistoria()
    {
        transicionando = true;

        // El ave grande del onboarding se va; los paneles traen su propia ave
        if (cgPajaro != null) StartCoroutine(Fundido(cgPajaro, cgPajaro.alpha, 0f, duracionFundidoPanel));

        yield return TransicionPanel(cgOnboarding, cgHistoria);

        transicionando = false;
    }

    private void OnSiguienteHistoria()
    {
        if (transicionando) return;

        if (panelEco != null) StartCoroutine(IrAPanelEco());
        else Empezar();
    }

    // ---------- Panel "En cada Eco vas a" ----------
    private IEnumerator IrAPanelEco()
    {
        transicionando = true;

        foreach (CanvasGroup c in itemsEco) c.alpha = 0f;

        yield return TransicionPanel(cgHistoria, cgEco);

        transicionando = false;

        // Checks y textos aparecen uno por uno
        yield return new WaitForSeconds(pausaAntesDeItems);
        foreach (CanvasGroup c in itemsEco)
        {
            StartCoroutine(Fundido(c, 0f, 1f, duracionItem));
            yield return new WaitForSeconds(retardoEntreItems);
        }
    }

    // ---------- Cadena de paneles finales ----------
    // Eco -> Guía 1 -> Guía 2 -> Listo para iniciar -> Para escanear -> Ten presente -> Principal
    private void OnSiguienteEco() { Ir(cgEco, cgGuia1); }
    private void OnSiguienteGuia1() { Ir(cgGuia1, cgGuia2); }
    private void OnAtrasGuia2() { Ir(cgGuia2, cgGuia1); }
    private void OnEntendidoGuia2() { Ir(cgGuia2, cgListo); }
    private void OnIniciar() { Ir(cgListo, cgEscanear); }
    private void OnEntendidoEscanear() { Ir(cgEscanear, cgPresente, EncenderLuz); }
    // 'siguiente' de Ten presente -> Empezar() (carga la escena principal)

    // Va de un panel a otro; si el destino no está configurado, carga la escena principal
    private void Ir(CanvasGroup origen, CanvasGroup destino, Action alLlegar = null)
    {
        if (transicionando) return;

        if (destino == null) { Empezar(); return; }

        StartCoroutine(IrCoroutine(origen, destino, alLlegar));
    }

    private IEnumerator IrCoroutine(CanvasGroup origen, CanvasGroup destino, Action alLlegar)
    {
        transicionando = true;
        yield return TransicionPanel(origen, destino);
        transicionando = false;

        if (alLlegar != null) alLlegar();
    }

    // La bombilla pasa de "sin luz" a "con luz" (fundido cruzado)
    private void EncenderLuz()
    {
        if (cgConLuz == null) return;
        cgConLuz.alpha = 0f;
        if (cgSinLuz != null) cgSinLuz.alpha = 1f;
        StartCoroutine(EncenderLuzCoroutine());
    }

    private IEnumerator EncenderLuzCoroutine()
    {
        yield return new WaitForSeconds(retardoLuz);

        if (cgSinLuz != null) StartCoroutine(Fundido(cgSinLuz, 1f, 0f, duracionLuz));
        yield return Fundido(cgConLuz, 0f, 1f, duracionLuz);
    }

    // Fundido cruzado entre dos paneles
    private IEnumerator TransicionPanel(CanvasGroup desde, CanvasGroup hacia)
    {
        if (hacia == null) yield break;

        // El panel nuevo se dibuja encima de todo
        hacia.transform.SetAsLastSibling();

        OcultarPanel(desde, conservarAlpha: true);
        hacia.alpha = 0f;

        if (desde != null) StartCoroutine(Fundido(desde, desde.alpha, 0f, duracionFundidoPanel));
        yield return Fundido(hacia, 0f, 1f, duracionFundidoPanel);

        MostrarPanel(hacia);
    }

    // ---------- Sonido ----------
    private void AlternarSonido()
    {
        sonidoActivo = !sonidoActivo;
        AudioListener.volume = sonidoActivo ? 1f : 0f;
        ActualizarIconoSonido();
    }

    private void ActualizarIconoSonido()
    {
        if (iconoSonido == null) return;

        Sprite s = sonidoActivo ? spriteSonidoOn : spriteSonidoOff;
        if (s != null) iconoSonido.sprite = s;
    }

    // ---------- Utilidades ----------
    private IEnumerator Fundido(CanvasGroup grupo, float desde, float hasta, float duracion)
    {
        if (grupo == null) yield break;

        float t = 0f;
        grupo.alpha = desde;
        while (t < duracion)
        {
            t += Time.deltaTime;
            grupo.alpha = Mathf.Lerp(desde, hasta, curvaFundido.Evaluate(Mathf.Clamp01(t / duracion)));
            yield return null;
        }
        grupo.alpha = hasta;
    }

    private static CanvasGroup ObtenerGrupo(GameObject objeto)
    {
        if (objeto == null) return null;
        CanvasGroup cg = objeto.GetComponent<CanvasGroup>();
        if (cg == null) cg = objeto.AddComponent<CanvasGroup>();
        return cg;
    }

    private static void Fijar(CanvasGroup grupo, float alpha)
    {
        if (grupo != null) grupo.alpha = alpha;
    }

    private static void OcultarPanel(CanvasGroup grupo, bool conservarAlpha = false)
    {
        if (grupo == null) return;
        if (!conservarAlpha) grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
    }

    private static void MostrarPanel(CanvasGroup grupo)
    {
        if (grupo == null) return;
        grupo.interactable = true;
        grupo.blocksRaycasts = true;
    }

    // ---------- Escena / salida ----------
    public void Empezar()
    {
        SceneManager.LoadScene(nombreEscenaPrincipal);
    }

    public void Salir()
    {
        Debug.Log("Saliendo de la experiencia...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}