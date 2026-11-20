using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InsigniasManager : MonoBehaviour
{
    public static InsigniasManager Instance;

    [System.Serializable]
    public class SlotInsignia
    {
        public string idPin;              // ej: "hojarasquin"
        public Image imagenBloqueada;     // sprite gris/candado
        public Image imagenActiva;        // sprite a color
        [HideInInspector] public bool desbloqueada = false;
    }

    [Header("Slots de insignias (uno por mito)")]
    [SerializeField] private List<SlotInsignia> slots = new List<SlotInsignia>();

    [Header("Eventos")]
    public UnityEngine.Events.UnityEvent OnTodasLasInsigniasObtenidas;

    private int totalPinesEnEscena = 0;
    private bool eventoDisparado = false;

    // El objetivo real son los slots; si no hay slots se usa el conteo de pines.
    public int TotalPines => slots.Count > 0 ? slots.Count : totalPinesEnEscena;

    public int PinesCompletados
    {
        get
        {
            int count = 0;
            foreach (var s in slots) if (s.desbloqueada) count++;
            return count;
        }
    }

    public bool TodasCompletadas => TotalPines > 0 && PinesCompletados >= TotalPines;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        InicializarSlots();
    }

    /// <summary>Deja todos los slots con su ícono bloqueado y sin activo.</summary>
    private void InicializarSlots()
    {
        eventoDisparado = false;

        foreach (var s in slots)
        {
            s.desbloqueada = false;

            if (s.imagenBloqueada != null)
            {
                s.imagenBloqueada.gameObject.SetActive(true);
                s.imagenBloqueada.enabled = true;
            }

            if (s.imagenActiva != null)
                s.imagenActiva.gameObject.SetActive(false);
        }

        Debug.Log($"[InsigniasManager] Slots inicializados: {slots.Count}");
    }

    /// <summary>Solo informativo; ya no es necesario para desbloquear la recompensa.</summary>
    public void CalcularTotalPines()
    {
        PinMapa[] todos = FindObjectsByType<PinMapa>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        totalPinesEnEscena = todos.Length;
        Debug.Log($"[InsigniasManager] Pines en escena: {totalPinesEnEscena} | Slots: {slots.Count}");
    }

    /// <summary>Otorga la insignia del pin indicado. Devuelve true si es nueva.</summary>
    public bool OtorgarInsignia(string idPin)
    {
        if (string.IsNullOrEmpty(idPin))
        {
            Debug.LogWarning("[InsigniasManager] idPin vacío.");
            return false;
        }

        SlotInsignia slot = slots.Find(s => s.idPin == idPin);
        if (slot == null)
        {
            Debug.LogWarning($"[InsigniasManager] No hay slot para el idPin '{idPin}'. Debe coincidir exactamente con el de la lista.");
            return false;
        }

        if (slot.desbloqueada)
        {
            Debug.Log($"[InsigniasManager] Insignia de {idPin} ya otorgada.");
            return false;
        }

        slot.desbloqueada = true;

        if (slot.imagenBloqueada != null)
            slot.imagenBloqueada.gameObject.SetActive(false);

        if (slot.imagenActiva != null)
            slot.imagenActiva.gameObject.SetActive(true);

        Debug.Log($"[InsigniasManager] 🏅 Insignia otorgada: {idPin} ({PinesCompletados}/{TotalPines})");

        if (TodasCompletadas && !eventoDisparado)
        {
            eventoDisparado = true;
            Debug.Log("[InsigniasManager] 🎉 ¡Todas las insignias obtenidas!");
            OnTodasLasInsigniasObtenidas?.Invoke();
        }

        return true;
    }

    public bool PinYaCompletado(string idPin)
    {
        SlotInsignia slot = slots.Find(s => s.idPin == idPin);
        return slot != null && slot.desbloqueada;
    }

    [ContextMenu("Resetear insignias")]
    public void Resetear()
    {
        InicializarSlots();
        Debug.Log("[InsigniasManager] Insignias reseteadas.");
    }
}