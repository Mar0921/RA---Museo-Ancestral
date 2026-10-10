using UnityEngine;
using UnityEngine.UI;

public class BotonMochila : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelMaleta;

    [Tooltip("Botón de la mochila. Un solo clic abre/cierra el panel.")]
    [SerializeField] private Button botonMochila;

    [Tooltip("Opcional: botón de cerrar DENTRO del panel. Puede dejarse vacío.")]
    [SerializeField] private Button botonCerrar;

    private bool abierto = false;

    void Start()
    {
        if (panelMaleta != null) panelMaleta.SetActive(false);

        if (botonMochila != null)
            botonMochila.onClick.AddListener(TogglePanel);

        if (botonCerrar != null && botonCerrar != botonMochila)
            botonCerrar.onClick.AddListener(CerrarPanel);
    }

    public void TogglePanel()
    {
        abierto = !abierto;
        if (panelMaleta != null) panelMaleta.SetActive(abierto);
        Debug.Log($"[BotonMochila] Panel maleta {(abierto ? "abierto" : "cerrado")}");
    }

    public void AbrirPanel()
    {
        abierto = true;
        if (panelMaleta != null) panelMaleta.SetActive(true);
    }

    public void CerrarPanel()
    {
        abierto = false;
        if (panelMaleta != null) panelMaleta.SetActive(false);
    }
}