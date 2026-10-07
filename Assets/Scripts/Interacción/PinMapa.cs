using UnityEngine;
using System.Collections;
using TMPro;

[DisallowMultipleComponent]
public class PinMapa : MonoBehaviour
{
    // ============================================================
    // OBJETOS AR
    // ============================================================

    [Header("Objetos a mostrar/ocultar al tocar el pin")]
    [SerializeField] private GameObject[] objetosAR;

    // ============================================================
    // LETRERO
    // ============================================================

    [Header("Letrero del pin")]
    [SerializeField] private GameObject letrero;

    public bool FueActivado { get; private set; } = false;

    public string idPin;

    // ============================================================
    // ORDEN
    // ============================================================

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
    // TEXTO DE INTRODUCCIÓN
    // ============================================================

    [Header("Texto de introducción")]

    [Tooltip("Este texto aparecerá en RespuestaZylo antes de mostrar los modelos.")]
    [TextArea(3, 8)]
    [SerializeField] private string textoIntroduccion;

    // ============================================================
    // TEXTO DEL PIN
    // ============================================================

    [Header("Texto del Pin")]

    [TextArea]
    [SerializeField] private string textoDelPin;

    // ============================================================
    // REFERENCIAS
    // ============================================================

    [Header("Referencias directas")]
    [SerializeField] private PanelPreguntasZylo panelPreguntasZylo;

    // ============================================================
    // PROPIEDADES
    // ============================================================

    public AudioClip IntroduccionPin => introduccionPin;

    public AudioClip AudioDelPin => audioPin;

    public AudioClip AudioRespuesta1 => audioRespuesta1;

    public AudioClip AudioRespuesta2 => audioRespuesta2;

    public string TextoDelPin => textoDelPin;

    public string TextoIntroduccion => textoIntroduccion;

    // ============================================================
    // AUDIO SOURCE
    // ============================================================

    private AudioSource audioSource;

    // ============================================================
    // REFERENCIAS A ELEMENTOS DE LA ESCENA
    // ============================================================

    private GameObject respuestaZylo;
    private TMP_Text textoRespuesta;

    // ============================================================
    // AWAKE
    // ============================================================

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        // Los modelos comienzan ocultos
        OcultarObjetos();

        // Letrero oculto
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

        OcultarRespuestaZylo();
    }

    // ============================================================
    // ZYLO LLEGÓ AL PIN
    // ============================================================

    public void OnZyloLlego(CatController gato)
    {
        if (FueActivado)
        {
            return;
        }

        FueActivado = true;

        Debug.Log(
            $"[PinMapa] 🐱 Zylo llegó al pin {idPin}"
        );

        // Los modelos NO deben aparecer todavía
        OcultarObjetos();

        // Comenzar introducción
        StartCoroutine(SecuenciaIntroduccion(gato));
    }

    // ============================================================
    // SECUENCIA DE INTRODUCCIÓN
    // ============================================================

    private IEnumerator SecuenciaIntroduccion(CatController gato)
    {
        Debug.Log(
            $"[PinMapa] 🎬 Iniciando introducción de {idPin}"
        );

        // ========================================================
        // 1. BUSCAR EL OBJETO DE LA ESCENA
        // ========================================================

        BuscarRespuestaZylo();

        // ========================================================
        // 2. MOSTRAR TEXTO DE INTRODUCCIÓN
        // ========================================================

        MostrarIntroduccion();

        // Esperamos un frame para que Unity actualice
        // el Canvas antes de reproducir el audio.
        yield return null;

        // ========================================================
        // 3. REPRODUCIR AUDIO
        // ========================================================

        if (introduccionPin != null)
        {
            Debug.Log(
                $"[PinMapa] 🔊 Reproduciendo: {introduccionPin.name}"
            );

            audioSource.clip = introduccionPin;
            audioSource.Play();

            // El texto permanece visible
            // y los modelos permanecen ocultos.
            yield return new WaitWhile(
                () => audioSource.isPlaying
            );
        }
        else
        {
            Debug.LogWarning(
                $"[PinMapa] ⚠️ El pin {idPin} no tiene IntroduccionPin."
            );

            yield return new WaitForSeconds(0.1f);
        }

        // ========================================================
        // 4. TERMINÓ INTRODUCCIÓN
        // ========================================================

        Debug.Log(
            $"[PinMapa] ✅ Terminó introducción de {idPin}"
        );

        // ========================================================
        // 5. OCULTAR TEXTO
        // ========================================================

        OcultarRespuestaZylo();

        // ========================================================
        // 6. MOSTRAR MODELOS
        // ========================================================

        MostrarObjetos();

        Debug.Log(
            $"[PinMapa] 👁️ Modelos AR mostrados."
        );

        // ========================================================
        // 7. ABRIR PREGUNTAS
        // ========================================================

        IniciarDialogoConPanel(gato);
    }

    // ============================================================
    // BUSCAR RESPUESTA ZYLO EN LA ESCENA
    // ============================================================

    private void BuscarRespuestaZylo()
    {
        // --------------------------------------------------------
        // Buscar el objeto RespuestaZylo en TODA LA ESCENA
        // incluyendo objetos desactivados.
        // --------------------------------------------------------

        GameObject[] objetos =
            Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject obj in objetos)
        {
            // Evitar objetos que sean assets/prefabs del proyecto
            if (!obj.scene.IsValid())
            {
                continue;
            }

            if (obj.name == "RespuestaZylo")
            {
                respuestaZylo = obj;

                // Buscar TextoRespuesta dentro de RespuestaZylo
                TMP_Text[] textos =
                    respuestaZylo.GetComponentsInChildren<TMP_Text>(
                        true
                    );

                foreach (TMP_Text texto in textos)
                {
                    if (texto.gameObject.name == "TextoRespuesta")
                    {
                        textoRespuesta = texto;

                        Debug.Log(
                            "[PinMapa] ✅ Encontrado RespuestaZylo y TextoRespuesta en la escena."
                        );

                        return;
                    }
                }

                Debug.LogWarning(
                    "[PinMapa] ⚠️ Se encontró RespuestaZylo, pero no TextoRespuesta."
                );

                return;
            }
        }

        Debug.LogError(
            "[PinMapa] ❌ No se encontró RespuestaZylo en la escena."
        );
    }

    // ============================================================
    // MOSTRAR INTRODUCCIÓN
    // ============================================================

    private void MostrarIntroduccion()
    {
        // Si por alguna razón todavía no existe la referencia,
        // volver a buscarla.
        if (respuestaZylo == null || textoRespuesta == null)
        {
            BuscarRespuestaZylo();
        }

        // --------------------------------------------------------
        // Activar RespuestaZylo
        // --------------------------------------------------------

        if (respuestaZylo != null)
        {
            respuestaZylo.SetActive(true);
        }

        // --------------------------------------------------------
        // Escribir texto
        // --------------------------------------------------------

        if (textoRespuesta != null)
        {
            textoRespuesta.text = textoIntroduccion;
            textoRespuesta.enabled = true;

            Debug.Log(
                $"[PinMapa] 📝 Texto de introducción mostrado:\n{textoIntroduccion}"
            );
        }
        else
        {
            Debug.LogError(
                "[PinMapa] ❌ No se encontró TextoRespuesta."
            );
        }
    }

    // ============================================================
    // OCULTAR RESPUESTA ZYLO
    // ============================================================

    private void OcultarRespuestaZylo()
    {
        if (respuestaZylo != null)
        {
            respuestaZylo.SetActive(false);
        }
    }

    // ============================================================
    // INICIAR DIÁLOGO
    // ============================================================

    private void IniciarDialogoConPanel(CatController gato)
    {
        Debug.Log(
            $"[PinMapa] 🔎 Buscando PanelPreguntasZylo para {idPin}..."
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
                $"[PinMapa] ✅ Panel encontrado."
            );

            panelPreguntas.MostrarPanelPreguntas(
                this,
                gato
            );
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
        {
            return;
        }

        foreach (GameObject obj in objetosAR)
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
        {
            return;
        }

        foreach (GameObject obj in objetosAR)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }
}