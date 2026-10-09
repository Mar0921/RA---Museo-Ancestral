using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SubtitulosMito : MonoBehaviour
{
    public static SubtitulosMito Instance;

    [SerializeField] private TextMeshProUGUI textoSubtitulos;
    [SerializeField] private float tiempoExtra = 0.5f;

    [Header("Control de Overflow y segmentación")]
    [SerializeField] private bool activarSegmentacionAutomatica = true;
    [SerializeField] private int maxLineasVisibles = 4;

    [Header("Duración dinámica por texto")]
    [SerializeField] private float segundosPorCaracter = 0.07f;

    private Coroutine coroutineActiva;

    void Awake()
    {
        Instance = this;
    }

    public void MostrarTexto(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return;
        DetenerSinLimpiar();
        float duracionTotal = CalcularDuracionTexto(texto);
        coroutineActiva = StartCoroutine(MostrarCoroutine(texto, duracionTotal, true));
    }

    public void MostrarSubtitulosConAudio(string texto, AudioClip clip)
    {
        if (string.IsNullOrEmpty(texto)) return;
        DetenerSinLimpiar();
        float duracion = clip != null ? clip.length : CalcularDuracionTexto(texto);
        coroutineActiva = StartCoroutine(MostrarCoroutine(texto, duracion, true));
    }

    /// <summary>
    /// Escribe un texto fijo (sin coroutine). No se borra solo.
    /// Se usa para la pregunta.
    /// </summary>
    public void MostrarTextoFijo(string texto)
    {
        DetenerSinLimpiar();
        if (textoSubtitulos != null)
            textoSubtitulos.text = texto;
    }

    /// <summary>
    /// Detiene la coroutine y limpia el texto.
    /// </summary>
    public void DetenerYLimpiar()
    {
        DetenerSinLimpiar();
        if (textoSubtitulos != null)
            textoSubtitulos.text = "";
    }

    /// <summary>
    /// Detiene la coroutine pero NO limpia el texto.
    /// </summary>
    public void DetenerSinLimpiar()
    {
        if (coroutineActiva != null)
        {
            StopCoroutine(coroutineActiva);
            coroutineActiva = null;
        }
    }

    private IEnumerator MostrarCoroutine(string texto, float duracion, bool limpiarAlFinal)
    {
        if (textoSubtitulos == null) yield break;

        if (activarSegmentacionAutomatica)
        {
            List<string> segmentos = DividirTextoPorLineas(texto);

            if (segmentos.Count == 0)
            {
                textoSubtitulos.text = texto;
                yield return new WaitForSeconds(duracion + tiempoExtra);
                if (limpiarAlFinal) textoSubtitulos.text = "";
                coroutineActiva = null;
                yield break;
            }

            int totalCaracteres = texto.Length;

            foreach (string segmento in segmentos)
            {
                float proporcion = (float)segmento.Length / totalCaracteres;
                float duracionSegmento = (duracion * proporcion) + 0.3f;

                textoSubtitulos.text = segmento;
                yield return new WaitForSeconds(duracionSegmento);
            }
        }
        else
        {
            textoSubtitulos.text = texto;
            yield return new WaitForSeconds(duracion + tiempoExtra);
        }

        if (limpiarAlFinal) textoSubtitulos.text = "";
        coroutineActiva = null;
    }

    private float CalcularDuracionTexto(string texto)
    {
        return Mathf.Max(1f, texto.Length * segundosPorCaracter);
    }

    private List<string> DividirTextoPorLineas(string texto)
    {
        List<string> segmentos = new List<string>();
        if (textoSubtitulos == null) return segmentos;

        string[] bloquesPorSalto = texto.Split('\n');

        foreach (string bloque in bloquesPorSalto)
        {
            string bloqueLimpio = bloque.Trim();
            if (string.IsNullOrEmpty(bloqueLimpio)) continue;

            textoSubtitulos.text = bloqueLimpio;
            textoSubtitulos.ForceMeshUpdate();

            if (textoSubtitulos.textInfo.lineCount <= maxLineasVisibles &&
                !textoSubtitulos.isTextOverflowing)
            {
                segmentos.Add(bloqueLimpio);
                continue;
            }

            string[] palabras = bloqueLimpio.Split(' ');
            string bloqueActual = "";

            textoSubtitulos.text = "";
            textoSubtitulos.ForceMeshUpdate();

            foreach (string palabra in palabras)
            {
                string prueba = string.IsNullOrEmpty(bloqueActual)
                    ? palabra
                    : bloqueActual + " " + palabra;

                textoSubtitulos.text = prueba;
                textoSubtitulos.ForceMeshUpdate();

                if ((textoSubtitulos.textInfo.lineCount > maxLineasVisibles ||
                     textoSubtitulos.isTextOverflowing) &&
                     !string.IsNullOrEmpty(bloqueActual))
                {
                    segmentos.Add(bloqueActual);
                    bloqueActual = palabra;
                }
                else
                {
                    bloqueActual = prueba;
                }
            }

            if (!string.IsNullOrEmpty(bloqueActual))
                segmentos.Add(bloqueActual);
        }

        textoSubtitulos.text = "";
        return segmentos;
    }
}