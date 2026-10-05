using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class PinMapa : MonoBehaviour
{
    [Header("Objetos a mostrar/ocultar al tocar el pin")]
    [SerializeField] private GameObject[] objetosAR;

    [Header("Letrero del pin")]
    [SerializeField] private GameObject letrero;

    public bool FueActivado { get; private set; } = false;

    public string idPin; // Ej: "2000_2002"

    [Header("Orden de aparición")]
    [SerializeField] private int ordenPin = 0;
    public int OrdenPin => ordenPin;

    // ============================================================
    // AUDIOS
    // ============================================================

    [Header("Audios del Pin")]

    [Tooltip("Se reproduce primero. Los objetos AR aparecen cuando termina.")]
    [SerializeField] private AudioClip introduccionPin;

    [Tooltip("Audio principal del pin/etapa.")]
    [SerializeField] private AudioClip audioPin;

    [Header("Audios de las respuestas")]
    [SerializeField] private AudioClip audioRespuesta1;
    [SerializeField] private AudioClip audioRespuesta2;

    // ============================================================
    // TEXTO
    // ============================================================

    [TextArea]
    [SerializeField] private string textoDelPin;

    // ============================================================
    // REFERENCIAS
    // ============================================================

    [Header("Referencias directas")]
    [SerializeField] private PanelPreguntasZylo panelPreguntasZylo;

    // ============================================================
    // PROPIEDADES PÚBLICAS
    // ============================================================

    public AudioClip IntroduccionPin => introduccionPin;

    public AudioClip AudioDelPin => audioPin;

    public AudioClip AudioRespuesta1 => audioRespuesta1;

    public AudioClip AudioRespuesta2 => audioRespuesta2;

    public string TextoDelPin => textoDelPin;

    // ============================================================
    // AUDIO SOURCE
    // ============================================================

    private AudioSource audioSource;

    // ============================================================
    // AWAKE
    // ============================================================

    void Awake()
    {
        // Crear AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        // Ocultar objetos AR al inicio
        OcultarObjetos();

        // Ocultar letrero al inicio
        if (letrero != null)
        {
            letrero.SetActive(false);
        }
    }

    // ============================================================
    // ON ENABLE
    // ============================================================

    void OnEnable()
    {
        if (letrero != null)
        {
            letrero.SetActive(true);
        }
    }

    // ============================================================
    // ON DISABLE
    // ============================================================

    void OnDisable()
    {
        if (letrero != null)
        {
            letrero.SetActive(false);
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    // ============================================================
    // ZYLO LLEGÓ AL PIN
    // ============================================================

    public void OnZyloLlego(CatController gato)
    {
        // Evitar activar nuevamente mientras ya fue activado
        if (FueActivado)
        {
            return;
        }

        FueActivado = true;

        Debug.Log($"[PinMapa] Zylo llegó al pin {idPin}");

        // MUY IMPORTANTE:
        // Los objetos permanecen ocultos mientras suena
        // la introducción.
        OcultarObjetos();

        StartCoroutine(SecuenciaIntroduccion(gato));
    }

    // ============================================================
    // SECUENCIA DE INTRODUCCIÓN
    // ============================================================

    private IEnumerator SecuenciaIntroduccion(CatController gato)
    {
        Debug.Log($"[PinMapa] 🔊 Reproduciendo introducción del pin {idPin}");

        // --------------------------------------------------------
        // 1. REPRODUCIR INTRODUCCIÓN
        // --------------------------------------------------------

        if (introduccionPin != null)
        {
            audioSource.clip = introduccionPin;
            audioSource.Play();

            // Esperar exactamente hasta que termine
            yield return new WaitWhile(() => audioSource.isPlaying);
        }
        else
        {
            Debug.LogWarning(
                $"[PinMapa] ⚠️ El pin {idPin} no tiene Introducción Pin asignada."
            );
        }

        // --------------------------------------------------------
        // 2. TERMINÓ LA INTRODUCCIÓN
        // --------------------------------------------------------

        Debug.Log(
            $"[PinMapa] ✅ Terminó la introducción del pin {idPin}"
        );

        // Ahora sí aparecen los objetos
        MostrarObjetos();

        Debug.Log(
            $"[PinMapa] 👁️ Objetos AR mostrados para {idPin}"
        );

        // --------------------------------------------------------
        // 3. ABRIR PANEL DE PREGUNTAS
        // --------------------------------------------------------

        IniciarDialogoConPanel(gato);
    }

    // ============================================================
    // INICIAR DIÁLOGO
    // ============================================================

    private void IniciarDialogoConPanel(CatController gato)
    {
        Debug.Log(
            $"[PinMapa] Buscando PanelPreguntasZylo para {idPin}..."
        );

        PanelPreguntasZylo panelPreguntas = panelPreguntasZylo;

        if (panelPreguntas == null)
        {
            panelPreguntas =
                Object.FindFirstObjectByType<PanelPreguntasZylo>(
                    FindObjectsInactive.Include
                );
        }

        if (panelPreguntas != null)
        {
            Debug.Log(
                $"[PinMapa] ✅ Panel encontrado. Iniciando preguntas para {idPin}"
            );

            panelPreguntas.MostrarPanelPreguntas(this, gato);
        }
        else
        {
            Debug.LogError(
                "[PinMapa] ❌ No se encontró PanelPreguntasZylo en la escena."
            );
        }
    }

    // ============================================================
    // MOSTRAR OBJETOS
    // ============================================================

    public void MostrarObjetos()
    {
        if (objetosAR == null)
            return;

        foreach (var obj in objetosAR)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
    }

    // ============================================================
    // OCULTAR OBJETOS
    // ============================================================

    public void OcultarObjetos()
    {
        if (objetosAR == null)
            return;

        foreach (var obj in objetosAR)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }
}