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

    public AudioClip IntroduccionPin => introduccionPin;
    public AudioClip AudioDelPin => audioPin;
    public AudioClip AudioRespuestaCorrecta => audioRespuestaCorrecta;
    public AudioClip AudioRespuestaIncorrecta => audioRespuestaIncorrecta;
    public string TextoDelPin => textoDelPin;
    public string TextoIntroduccion => textoIntroduccion;

    private AudioSource audioSource;

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
                  $"Incorrecta={(audioRespuestaIncorrecta != null ? audioRespuestaIncorrecta.name : "NULL")}");
    }

    void OnEnable()
    {
        if (letrero != null) letrero.SetActive(true);
    }

    void OnDisable()
    {
        if (letrero != null) letrero.SetActive(false);
        if (audioSource != null) audioSource.Stop();
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

        if (SubtitulosMito.Instance != null && !string.IsNullOrEmpty(textoIntroduccion))
        {
            if (introduccionPin != null)
                SubtitulosMito.Instance.MostrarSubtitulosConAudio(textoIntroduccion, introduccionPin);
            else
                SubtitulosMito.Instance.MostrarTexto(textoIntroduccion);
        }

        yield return null;

        if (introduccionPin != null)
        {
            audioSource.clip = introduccionPin;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        MostrarObjetos();
        yield return new WaitForSeconds(0.5f);

        if (audioPin != null)
        {
            if (SubtitulosMito.Instance != null && !string.IsNullOrEmpty(textoDelPin))
                SubtitulosMito.Instance.MostrarSubtitulosConAudio(textoDelPin, audioPin);

            audioSource.clip = audioPin;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        IniciarDialogoConPanel(guia);
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
        Debug.Log($"[PinMapa] 🔄 Pin {idPin} reseteado.");
    }
}