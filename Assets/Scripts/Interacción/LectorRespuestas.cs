using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;

public class LectorRespuestas : MonoBehaviour
{
    public static LectorRespuestas instance;

    private List<DatosRespuesta> respuestas = new List<DatosRespuesta>();
    private string rutaArchivo;

    void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }

        rutaArchivo = Path.Combine(Application.streamingAssetsPath,
            "Recursos", "TXT_Interaccion", "respuestas.txt");

        Debug.Log($"[LectorRespuestas] Ruta: {rutaArchivo}");
        StartCoroutine(CargarRespuestasDesdeTXT());
    }

    IEnumerator CargarRespuestasDesdeTXT()
    {
        string contenido = "";

        if (Application.platform == RuntimePlatform.Android)
        {
            UnityWebRequest www = UnityWebRequest.Get(rutaArchivo);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[LectorRespuestas] Error en Android: {www.error}");
                yield break;
            }
            contenido = www.downloadHandler.text;
        }
        else
        {
            if (!File.Exists(rutaArchivo))
            {
                Debug.LogError($"[LectorRespuestas] No existe: {rutaArchivo}");
                yield break;
            }
            contenido = File.ReadAllText(rutaArchivo);
        }

        Debug.Log($"[LectorRespuestas] Contenido leído ({contenido.Length} caracteres).");
        ProcesarContenido(contenido);
    }

    void ProcesarContenido(string contenido)
    {
        respuestas.Clear();
        string[] lineas = contenido.Split('\n');
        Debug.Log($"[LectorRespuestas] Total de líneas: {lineas.Length}");

        foreach (string linea in lineas)
        {
            if (string.IsNullOrWhiteSpace(linea)) continue;
            if (linea.TrimStart().StartsWith("#")) continue;

            string[] partes = linea.Split('-');
            if (partes.Length < 3)
            {
                Debug.LogWarning($"[LectorRespuestas] Línea inválida ({partes.Length} partes): {linea}");
                continue;
            }

            respuestas.Add(new DatosRespuesta
            {
                idPin = partes[0].Trim(),
                respuesta1 = partes[1].Trim(),
                respuesta2 = partes[2].Trim()
            });
        }

        Debug.Log($"[LectorRespuestas] Cargadas {respuestas.Count} respuestas.");
        foreach (var r in respuestas)
            Debug.Log($"[LectorRespuestas] → idPin: {r.idPin}");
    }

    public DatosRespuesta ObtenerDatosPorID(string id)
    {
        var r = respuestas.Find(x => x.idPin == id);
        if (r == null) Debug.LogWarning($"[LectorRespuestas] No encontrado: {id}");
        return r;
    }
}