using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Botón global para saltar la narración en curso.
/// Poner este script en un GameObject SIEMPRE ACTIVO (por ejemplo el Canvas o un manager),
/// NO en el botón mismo, porque el botón se oculta/muestra.
/// </summary>
public class BotonSaltarNarracion : MonoBehaviour
{
    public static BotonSaltarNarracion Instance;

    [SerializeField] private Button botonSaltar;

    /// <summary>True desde que el usuario presiona "Saltar" hasta la siguiente Activar().</summary>
    public bool SaltoSolicitado { get; private set; }

    private bool activo = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (botonSaltar != null)
        {
            botonSaltar.onClick.AddListener(Saltar);
            botonSaltar.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[BotonSaltarNarracion] botonSaltar no asignado.");
        }
    }

    void OnDestroy()
    {
        if (botonSaltar != null) botonSaltar.onClick.RemoveListener(Saltar);
        if (Instance == this) Instance = null;
    }

    /// <summary>Muestra el botón y reinicia la solicitud de salto.</summary>
    public void Activar()
    {
        SaltoSolicitado = false;
        activo = true;
        if (botonSaltar != null) botonSaltar.gameObject.SetActive(true);
    }

    /// <summary>Oculta el botón.</summary>
    public void Desactivar()
    {
        activo = false;
        SaltoSolicitado = false;
        if (botonSaltar != null) botonSaltar.gameObject.SetActive(false);
    }

    public void Saltar()
    {
        if (!activo) return;
        if (ConfiguracionManager.MenuAbierto) return;   // el menú no debe saltar nada
        SaltoSolicitado = true;
    }
}