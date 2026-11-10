using System;
using System.Collections;
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
    [SerializeField] private Image iconoSonido;       // puede ser el Image del propio botón
    [SerializeField] private Sprite spriteSonidoOn;
    [SerializeField] private Sprite spriteSonidoOff;

    [Header("Puntos indicadores")]
    [SerializeField] private Image[] puntos;          // punto1..punto4 en orden
    [Tooltip("Sprite del punto pintado (activo). Si está vacío se usa solo el color.")]
    [SerializeField] private Sprite spritePuntoActivo;
    [Tooltip("Sprite del punto sin pintar (inactivo). Si está vacío se usa solo el color.")]
    [SerializeField] private Sprite spritePuntoInactivo;
    [Tooltip("Solo se usa cuando el sprite correspondiente está vacío.")]
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

    [Header("Curvas")]
    [SerializeField] private AnimationCurve curvaFundido = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve curvaVuelo = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Textos")]
    [SerializeField] private string mensajeCarga = "Preparando el territorio...";

    private CanvasGroup cgOscuro, cgLogo, cgInternet, cgCarga, cgPajaro, cgOnboarding, cgTextos;

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

        if (oscuro != null) oscuro.SetActive(true);
        if (logo != null) logo.SetActive(true);
        if (internet != null) internet.SetActive(true);
        if (grupoCarga != null) grupoCarga.SetActive(true);
        if (panelOnboarding != null) panelOnboarding.SetActive(true);
        if (grupoTextos != null) grupoTextos.SetActive(true);

        Fijar(cgOscuro, 1f);
        Fijar(cgLogo, 0f);
        Fijar(cgInternet, 0f);
        Fijar(cgCarga, 0f);
        Fijar(cgPajaro, 0f);
        Fijar(cgTextos, 1f);

        if (cgOnboarding != null)
        {
            cgOnboarding.alpha = 0f;
            cgOnboarding.interactable = false;
            cgOnboarding.blocksRaycasts = false;
        }

        if (cgPajaro != null) cgPajaro.blocksRaycasts = false;

        if (relleno != null) relleno.fillAmount = 0f;
        if (textoProgreso != null) textoProgreso.text = $"{mensajeCarga} 0%";
        if (pajaro != null) pajaro.gameObject.SetActive(false);

        MostrarDiapositiva(0);
        ActualizarIconoSonido();
    }

    private void ConectarBotones()
    {
        if (botonSiguiente != null) botonSiguiente.onClick.AddListener(OnSiguiente);
        if (botonSaltar != null) botonSaltar.onClick.AddListener(Empezar);
        if (botonSonido != null) botonSonido.onClick.AddListener(AlternarSonido);
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
        if (cgOnboarding != null)
        {
            cgOnboarding.interactable = true;
            cgOnboarding.blocksRaycasts = true;
        }
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
            Empezar();
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
                // Con sprite propio, color blanco para no teñirlo
                puntos[i].sprite = sprite;
                puntos[i].color = Color.white;
            }
            else
            {
                // Sin sprite asignado, se mantiene el sprite actual y se tiñe
                puntos[i].color = activo ? colorPuntoActivo : colorPuntoInactivo;
            }
        }
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