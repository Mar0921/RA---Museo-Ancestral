using UnityEngine;
using TMPro;

public class DiagnosticoVisual : MonoBehaviour
{
    public TextMeshProUGUI debugText;

    void Start()
    {
        if (debugText == null)
        {
            Debug.LogError("[DiagnosticoVisual] debugText no está asignado en el Inspector");
            return;
        }

        string estado = "=== DIAGNÓSTICO VISUAL ===\n\n";

        // Verificar SubtitulosMito.Instance
        if (SubtitulosMito.Instance == null)
        {
            estado += "SubtitulosMito.Instance es NULL\n";
        }
        else
        {
            estado += "SubtitulosMito.Instance existe\n";

            TextMeshProUGUI subtituloTexto = SubtitulosMito.Instance.GetComponentInChildren<TextMeshProUGUI>();

            if (subtituloTexto == null)
                estado += "No se encontró TextMeshProUGUI en SubtitulosMito\n";
            else
                estado += $"Texto subtítulos: '{subtituloTexto.text}'\n";
        }

        debugText.text = estado;
        Debug.Log(estado);
    }
}