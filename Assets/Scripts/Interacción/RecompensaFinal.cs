using UnityEngine;
using UnityEngine.UI;

public class RecompensaFinal : MonoBehaviour
{
    [Header("UI de recompensa")]
    [SerializeField] private GameObject panelRecompensa;
    [SerializeField] private Button botonTomarFoto;
    [SerializeField] private Button botonCerrar;
    [SerializeField] private Image marcoAlusivo;

    [Header("Foto")]
    [SerializeField] private ARPhotoManager photoManager;

    private bool recompensaDesbloqueada = false;

    void Start()
    {
        if (panelRecompensa != null)
            panelRecompensa.SetActive(false);

        if (botonTomarFoto != null)
            botonTomarFoto.onClick.AddListener(TomarFotoConMarco);

        if (botonCerrar != null)
            botonCerrar.onClick.AddListener(() => panelRecompensa.SetActive(false));
    }

    void OnEnable()
    {
        if (InsigniasManager.Instance != null)
            InsigniasManager.Instance.OnTodasLasInsigniasObtenidas.AddListener(DesbloquearRecompensa);
    }

    void OnDisable()
    {
        if (InsigniasManager.Instance != null)
            InsigniasManager.Instance.OnTodasLasInsigniasObtenidas.RemoveListener(DesbloquearRecompensa);
    }

    public void DesbloquearRecompensa()
    {
        if (recompensaDesbloqueada) return;
        recompensaDesbloqueada = true;

        Debug.Log("[RecompensaFinal] 🎁 ¡Recompensa desbloqueada!");

        if (panelRecompensa != null)
            panelRecompensa.SetActive(true);

        if (marcoAlusivo != null)
            marcoAlusivo.gameObject.SetActive(true);
    }

    private void TomarFotoConMarco()
    {
        if (photoManager == null)
        {
            Debug.LogError("[RecompensaFinal] photoManager no asignado.");
            return;
        }

        photoManager.StartCoroutine(photoManager.CapturePhoto());
        Debug.Log("[RecompensaFinal] 📸 Foto tomada con marco alusivo.");
    }
}