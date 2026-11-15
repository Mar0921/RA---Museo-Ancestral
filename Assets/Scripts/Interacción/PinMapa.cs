using UnityEngine;
using System.Collections;
using TMPro;

[DisallowMultipleComponent]
public class PinMapa : MonoBehaviour
{
    [Header("Objetos a mostrar/ocultar al tocar el pin")]
    [SerializeField] private GameObject[] objetosAR;

    [Header("Letrero del pin")]
    [SerializeField] private GameObject letrero;

    public bool FueActivado { get; private set; } = false;
    public string idPin;

    [Header("Orden de aparición")]
    [SerializeField] private int ordenPin = 0;
    public int OrdenPin => ordenPin;

    [Header("Audios del Pin")]
    [SerializeField] private AudioClip introduccionPin;
    [SerializeField] private AudioClip audioPin;

    [Header("Audios de las respuestas")]
    [SerializeField] private AudioClip audioRespuestaCorrecta;
    [SerializeField] private AudioClip audioRespuestaIncorrecta;

    [Header("Texto de introducción")]
    [TextArea(3, 8)]
    [SerializeField] private string textoIntroduccion;

    [Header("Texto del Pin (historia)")]
    [TextArea(5, 15)]
    [SerializeField] private string textoDelPin;

    [Header("Objeto de recompensa (se ve en detalle tras la insignia)")]
    [Tooltip("Prefab del objeto del mito. Si está vacío, no se muestra visor.")]
    [SerializeField] private GameObject objetoDetalle;
    [SerializeField] private string tituloObjeto;
    [TextArea(3, 8)]
    [SerializeField] private string descripcionObjeto;

    public AudioClip IntroduccionPin => introduccionPin;
    public AudioClip AudioDelPin => audioPin;
    public AudioClip AudioRespuestaCorrecta => audioRespuestaCorrecta;
    public AudioClip AudioRespuestaIncorrecta => audioRespuestaIncorrecta;
    public string TextoDelPin => textoDelPin;
    public string TextoIntroduccion => textoIntroduccion;

    public GameObject ObjetoDetalle => objetoDetalle;
    public string TituloObjeto => tituloObjeto;
    public string DescripcionObjeto => descripcionObjeto;

    private AudioSource audioSource;
    private bool narrando = false;

    void Awake()
    {
        // Reusar el AudioSource si ya existe en el prefab; si no, crearlo.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        OcultarObjetos();
        if (letrero != null) letrero.SetActive(false);

        Debug.Log($"[PinMapa] '{idPin}' audios asignados: " +
                  $"Intro={(introduccionPin != null ? introduccionPin.name : "NULL")}, " +
                  $"Historia={(audioPin != null ? audioPin.name : "NULL")}, " +
                  $"Correcta={(audioRespuestaCorrecta != null ? audioRespuestaCorrecta.name : "NULL")}, " +
                  $"Incorrecta={(audioRespuestaIncorrecta != null ? audioRespuestaIncorrecta.name : "NULL")}, " +
                  $"ObjetoDetalle={(objetoDetalle != null ? objetoDetalle.name : "NULL")}");
    }

    void OnEnable()
    {
        if (letrero != null) letrero.SetActive(true);
    }

    void OnDisable()
    {
        if (letrero != null) letrero.SetActive(false);
        if (audioSource != null) audioSource.Stop();

        if (narrando)
        {
            narrando = false;
            BotonSaltarNarracion.Instance?.Desactivar();
        }
    }

    public void OnGuiaLlego(GuiaController guia)
    {
        if (FueActivado) return;
        FueActivado = true;

        Debug.Log($"[PinMapa] 🐱 Guía llegó al pin {idPin}");
        OcultarObjetos();
        StartCoroutine(SecuenciaIntroduccion(guia));
    }

    private IEnumerator SecuenciaIntroduccion(GuiaController guia)
    {
        Debug.Log($"[PinMapa] 🎬 Iniciando introducción de {idPin}");

        // 1) Introducción (con subtítulos si hay texto)
        if (SubtitulosMito.Instance != null && !string.IsNullOrEmpty(textoIntroduccion))
        {
            if (introduccionPin != null)
                SubtitulosMito.Instance.MostrarSubtitulosConAudio(textoIntroduccion, introduccionPin);
            else
                SubtitulosMito.Instance.MostrarTexto(textoIntroduccion);
        }

        yield return null;
        yield return ReproducirNarracion(introduccionPin);

        // 2) Aparecen los objetos AR
        MostrarObjetos();
        yield return new WaitForSeconds(0.5f);

        // 3) Historia (solo con subtítulos si hay audio, como antes)
        if (audioPin != null && SubtitulosMito.Instance != null && !string.IsNullOrEmpty(textoDelPin))
            SubtitulosMito.Instance.MostrarSubtitulosConAudio(textoDelPin, audioPin);

        yield return ReproducirNarracion(audioPin);

        // 4) Cuestionario
        IniciarDialogoConPanel(guia);
    }

    /// <summary>
    /// Reproduce un clip y espera a que termine, o a que el usuario presione "Saltar".
    /// Tolera pausas del menú (AudioListener.pause / timeScale = 0): una pausa NO cuenta como fin del audio.
    /// Si no hay clip, espera 0.5 s como antes.
    /// </summary>
    private IEnumerator ReproducirNarracion(AudioClip clip)
    {
        if (clip == null)
        {
            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        var skip = BotonSaltarNarracion.Instance;
        narrando = true;
        skip?.Activar();

        audioSource.clip = clip;
        audioSource.Play();

        yield return null; // dejar que arranque

        bool veniaDePausa = false;
        while (true)
        {
            if (skip != null && skip.SaltoSolicitado) break;

            bool pausado = AudioListener.pause || ConfiguracionManager.MenuAbierto;
            if (pausado)
            {
                veniaDePausa = true;
                yield return null;
                continue;
            }

            // Un frame de gracia al reanudar: isPlaying puede tardar en volver a true
            if (veniaDePausa)
            {
                veniaDePausa = false;
                yield return null;
                continue;
            }

            if (!audioSource.isPlaying) break;
            yield return null;
        }

        if (skip != null && skip.SaltoSolicitado)
        {
            Debug.Log($"[PinMapa] ⏭️ Narración saltada en {idPin}");
            audioSource.Stop();
            SubtitulosMito.Instance?.DetenerYLimpiar();
        }

        skip?.Desactivar();
        narrando = false;
    }

    private void IniciarDialogoConPanel(GuiaController guia)
    {
        var quiz = QuizManagerMito.Instance;
        if (quiz == null)
            quiz = FindFirstObjectByType<QuizManagerMito>(FindObjectsInactive.Include);

        if (quiz != null)
            quiz.IniciarDesafio(this, guia);
        else
            Debug.LogError("[PinMapa] ❌ No se encontró QuizManagerMito.");
    }

    public void MostrarObjetos()
    {
        if (objetosAR == null) return;
        foreach (GameObject obj in objetosAR)
            if (obj != null) obj.SetActive(true);
    }

    public void OcultarObjetos()
    {
        if (objetosAR == null) return;
        foreach (GameObject obj in objetosAR)
            if (obj != null) obj.SetActive(false);
    }

    public void ResetearPin()
    {
        FueActivado = false;
        OcultarObjetos();
        if (audioSource != null) audioSource.Stop();

        if (narrando)
        {
            narrando = false;
            BotonSaltarNarracion.Instance?.Desactivar();
        }

        Debug.Log($"[PinMapa] 🔄 Pin {idPin} reseteado.");
    }
}