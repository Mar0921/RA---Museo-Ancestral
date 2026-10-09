using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;

public class LectorPreguntas : MonoBehaviour
{
    public static LectorPreguntas instance;

    private List<DatosPregunta> preguntas = new List<DatosPregunta>();
    private string rutaArchivo;

    void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }

        rutaArchivo = Path.Combine(Application.streamingAssetsPath,
            "Recursos", "TXT_Interaccion", "preguntas.txt");

        StartCoroutine(CargarPreguntasDesdeTXT());
    }

    IEnumerator CargarPreguntasDesdeTXT()
    {
        string contenido = "";

        if (Application.platform == RuntimePlatform.Android)
        {
            UnityWebRequest www = UnityWebRequest.Get(rutaArchivo);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[LectorPreguntas] Error: {www.error}");
                yield break;
            }
            contenido = www.downloadHandler.text;
        }
        else
        {
            if (!File.Exists(rutaArchivo))
            {
                Debug.LogError($"[LectorPreguntas] No existe: {rutaArchivo}");
                yield break;
            }
            contenido = File.ReadAllText(rutaArchivo);
        }

        ProcesarContenido(contenido);
    }

    void ProcesarContenido(string contenido)
    {
        preguntas.Clear();
        string[] lineas = contenido.Split('\n');

        foreach (string linea in lineas)
        {
            if (string.IsNullOrWhiteSpace(linea)) continue;
            if (linea.StartsWith("#")) continue; // comentarios

            string[] p = linea.Split('|');
            if (p.Length < 7)
            {
                Debug.LogWarning($"[LectorPreguntas] Línea inválida: {linea}");
                continue;
            }

            preguntas.Add(new DatosPregunta
            {
                idPin = p[0].Trim(),
                pregunta1 = p[1].Trim(),
                respuestaCorrecta1 = p[2].Trim(),
                respuestaIncorrecta1 = p[3].Trim(),
                pregunta2 = p[4].Trim(),
                respuestaCorrecta2 = p[5].Trim(),
                respuestaIncorrecta2 = p[6].Trim()
            });
        }

        Debug.Log($"[LectorPreguntas] Cargadas {preguntas.Count} preguntas.");
    }

    public DatosPregunta ObtenerDatosPorID(string id)
    {
        var r = preguntas.Find(x => x.idPin == id);
        if (r == null) Debug.LogWarning($"[LectorPreguntas] No encontrado: {id}");
        return r;
    }
}