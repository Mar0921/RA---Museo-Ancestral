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

    public int TotalPines => totalPinesEnEscena;
    public int PinesCompletados
    {
        get
        {
            int count = 0;
            foreach (var s in slots) if (s.desbloqueada) count++;
            return count;
        }
    }

    public bool TodasCompletadas => PinesCompletados >= totalPinesEnEscena && totalPinesEnEscena > 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        InicializarSlots();
    }

    /// <summary>
    /// Deja todos los slots con su ícono bloqueado y sin activo.
    /// </summary>
    private void InicializarSlots()
    {
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

    /// <summary>
    /// Cuenta dinámicamente todos los PinMapa de la escena.
    /// </summary>
    public void CalcularTotalPines()
    {
        PinMapa[] todos = FindObjectsByType<PinMapa>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        totalPinesEnEscena = todos.Length;
        Debug.Log($"[InsigniasManager] Total de pines detectados: {totalPinesEnEscena}");
    }

    /// <summary>
    /// Otorga la insignia del pin indicado. Devuelve true si es nueva.
    /// </summary>
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
            Debug.LogWarning($"[InsigniasManager] No hay slot para el idPin '{idPin}'.");
            return false;
        }

        if (slot.desbloqueada)
        {
            Debug.Log($"[InsigniasManager] Insignia de {idPin} ya otorgada.");
            return false;
        }

        slot.desbloqueada = true;

        // Cambiar imagen bloqueada por activa
        if (slot.imagenBloqueada != null)
            slot.imagenBloqueada.gameObject.SetActive(false);

        if (slot.imagenActiva != null)
            slot.imagenActiva.gameObject.SetActive(true);

        Debug.Log($"[InsigniasManager] 🏅 Insignia otorgada: {idPin} ({PinesCompletados}/{totalPinesEnEscena})");

        if (TodasCompletadas)
        {
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