using UnityEngine;
using UnityEngine.UI;

public class DesbloqueoRecompensa : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("El botón que permitirá abrir la cámara de fotos cuando todo esté completo")]
    [SerializeField] private Button botonTomarFotos;

    [Header("Referencias de Sistema")]
    [SerializeField] private ARPhotoManager photoManager;

    void Start()
    {
        // El botón empieza oculto al iniciar la experiencia
        if (botonTomarFotos != null)
            botonTomarFotos.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        // Nos suscribimos al evento de que todas las insignias fueron obtenidas
        if (InsigniasManager.Instance != null)
            InsigniasManager.Instance.OnTodasLasInsigniasObtenidas.AddListener(DesbloquearBoton);
    }

    void OnDisable()
    {
        // Nos desuscribimos para evitar errores
        if (InsigniasManager.Instance != null)
            InsigniasManager.Instance.OnTodasLasInsigniasObtenidas.RemoveListener(DesbloquearBoton);
    }

    // Se ejecuta automáticamente cuando ganas la última insignia
    private void DesbloquearBoton()
    {
        Debug.Log("[DesbloqueoRecompensa] 🎉 ¡Todas las insignias completadas! Desbloqueando botón de fotos.");

        if (botonTomarFotos != null)
        {
            botonTomarFotos.gameObject.SetActive(true);
        }
    }

    // Este método se asignará al OnClick del botón en el Inspector
    public void ClickBotonFotos()
    {
        if (photoManager != null)
        {
            photoManager.AbrirModoFoto();
        }
        else
        {
            Debug.LogError("[DesbloqueoRecompensa] Falta asignar el ARPhotoManager en el Inspector.");
        }
    }
}