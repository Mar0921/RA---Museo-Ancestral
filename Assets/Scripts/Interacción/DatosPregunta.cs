using UnityEngine;

[System.Serializable]
public class DatosPregunta
{
    public string idPin;

    [Header("Pregunta 1")]
    public string pregunta1;
    public string respuestaCorrecta1;
    public string respuestaIncorrecta1;
    public AudioClip audioPregunta1;
    public AudioClip audioCorrecta1;
    public AudioClip audioIncorrecta1;

    [Header("Pregunta 2 (segunda oportunidad)")]
    public string pregunta2;
    public string respuestaCorrecta2;
    public string respuestaIncorrecta2;
    public AudioClip audioPregunta2;
    public AudioClip audioCorrecta2;
    public AudioClip audioIncorrecta2;
}