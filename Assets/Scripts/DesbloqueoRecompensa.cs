// DesbloqueoRecompensa.cs  (va en "ManagerRecompensa", dentro de Managers)
using UnityEngine;
using UnityEngine.UI;

public class DesbloqueoRecompensa : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("BotonpanelfotoAbrir (dentro de SafeArea, fuera de Panel de fotos)")]
    [SerializeField] private Button botonAbrirPanelFoto;

    [Header("Referencias de Sistema")]
    [Tooltip("El objeto ARPhotoCapture")]
    [SerializeField] private ARPhotoManager photoManager;

    private bool suscrito = false;
    private bool desbloqueado = false;
    private int ultimoProgreso = -1;

    // ============================================================
    // CICLO DE VIDA
    // ============================================================

    void Awake()
    {
        Debug.Log("[DesbloqueoRecompensa] Awake en '" + gameObject.name + "'.");

        if (botonAbrirPanelFoto == null)
        {
            Debug.LogError("[DesbloqueoRecompensa] ❌ botonAbrirPanelFoto NO está asignado en el Inspector.");
            return;
        }

        if (botonAbrirPanelFoto.gameObject == gameObject)
            Debug.LogError("[DesbloqueoRecompensa] ❌ Este script está en el mismo objeto que el botón. Muévelo a ManagerRecompensa.");

        botonAbrirPanelFoto.onClick.RemoveListener(AbrirPanelFoto);
        botonAbrirPanelFoto.onClick.AddListener(AbrirPanelFoto);
        botonAbrirPanelFoto.gameObject.SetActive(false);
    }

    void OnEnable() { Suscribirse(); }
    void OnDisable() { Desuscribirse(); }

    void Start()
    {
        Suscribirse();

        var im = InsigniasManager.Instance;
        if (im == null)
            Debug.LogError("[DesbloqueoRecompensa] ❌ InsigniasManager.Instance es null. ¿Está InsigniaManager activo en la escena?");
        else
            Debug.Log($"[DesbloqueoRecompensa] InsigniasManager OK. Insignias: {im.PinesCompletados}/{im.TotalPines}" +
                      (im.TotalPines == 0 ? "  ⚠️ TotalPines = 0: nunca se completarán (lista 'slots' vacía o versión vieja del script)." : ""));

        if (photoManager == null)
            Debug.LogWarning("[DesbloqueoRecompensa] ⚠️ photoManager no asignado (el botón aparecería pero no abriría nada).");

        RevisarEstado();
    }

    // Respaldo: si el evento no llegó, igual se desbloquea al completar todas
    void Update()
    {
        if (desbloqueado) return;

        var im = InsigniasManager.Instance;
        if (im != null && im.PinesCompletados != ultimoProgreso)
        {
            ultimoProgreso = im.PinesCompletados;
            Debug.Log($"[DesbloqueoRecompensa] Progreso de insignias: {im.PinesCompletados}/{im.TotalPines}");
        }

        RevisarEstado();
    }

    // ============================================================
    // LÓGICA
    // ============================================================

    private void RevisarEstado()
    {
        if (desbloqueado) return;
        if (InsigniasManager.Instance != null && InsigniasManager.Instance.TodasCompletadas)
            DesbloquearBoton();
    }

    private void Suscribirse()
    {
        if (suscrito || InsigniasManager.Instance == null) return;
        InsigniasManager.Instance.OnTodasLasInsigniasObtenidas.AddListener(DesbloquearBoton);
        suscrito = true;
    }

    private void Desuscribirse()
    {
        if (!suscrito || InsigniasManager.Instance == null) return;
        InsigniasManager.Instance.OnTodasLasInsigniasObtenidas.RemoveListener(DesbloquearBoton);
        suscrito = false;
    }

    private void DesbloquearBoton()
    {
        if (desbloqueado) return;
        desbloqueado = true;

        Debug.Log("[DesbloqueoRecompensa] 🎉 Todas las insignias completadas. Encendiendo botón.");

        if (botonAbrirPanelFoto == null)
        {
            Debug.LogError("[DesbloqueoRecompensa] ❌ No hay botón asignado, no se puede encender.");
            return;
        }

        botonAbrirPanelFoto.gameObject.SetActive(true);
        botonAbrirPanelFoto.interactable = true;

        DiagnosticarBoton();
    }

    /// <summary>Revisa por qué el botón podría estar encendido pero no verse.</summary>
    private void DiagnosticarBoton()
    {
        if (botonAbrirPanelFoto.gameObject.activeInHierarchy)
            Debug.Log("[DesbloqueoRecompensa] ✅ El botón está activo en la jerarquía.");

        Transform t = botonAbrirPanelFoto.transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
                Debug.LogWarning($"[DesbloqueoRecompensa] ⚠️ El padre '{t.name}' está DESACTIVADO, por eso el botón no se ve.");

            CanvasGroup cg = t.GetComponent<CanvasGroup>();
            if (cg != null && cg.alpha < 0.01f)
                Debug.LogWarning($"[DesbloqueoRecompensa] ⚠️ '{t.name}' tiene CanvasGroup con alpha 0.");

            t = t.parent;
        }

        Image img = botonAbrirPanelFoto.GetComponent<Image>();
        if (img != null && img.color.a < 0.01f)
            Debug.LogWarning("[DesbloqueoRecompensa] ⚠️ La Image del botón tiene alfa 0.");

        Canvas canvas = botonAbrirPanelFoto.GetComponentInParent<Canvas>();
        if (canvas == null)
            Debug.LogWarning("[DesbloqueoRecompensa] ⚠️ El botón no está dentro de ningún Canvas.");
    }

    // ============================================================
    // PRUEBAS (clic derecho sobre el componente en el Inspector)
    // ============================================================

    [ContextMenu("PRUEBA: Forzar desbloqueo")]
    private void ForzarDesbloqueo()
    {
        Debug.Log("[DesbloqueoRecompensa] PRUEBA: forzando desbloqueo manual.");
        DesbloquearBoton();
    }

    /// <summary>Ya se registra por código; NO lo agregues también en el OnClick del Inspector.</summary>
    public void AbrirPanelFoto()
    {
        Debug.Log("[DesbloqueoRecompensa] Click en 'Abrir panel foto'.");

        if (photoManager == null)
        {
            Debug.LogError("[DesbloqueoRecompensa] Falta asignar el ARPhotoManager.");
            return;
        }

        photoManager.AbrirModoFoto();
    }
}