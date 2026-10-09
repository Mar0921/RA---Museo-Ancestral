using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InsigniasManager : MonoBehaviour
{
    public static InsigniasManager Instance;

    [Header("UI de insignias (una por pin, se llenan en orden)")]
    [SerializeField] private List<Image> slotsInsignias;

    [Header("Sprite de insignia obtenida")]
    [SerializeField] private Sprite spriteInsignia;

    [Header("Opciones")]
    [Tooltip("Si está activo, las imágenes de los slots se activan al ganar la insignia.")]
    [SerializeField] private bool activarImagenAlGanar = true;

    [Header("Eventos")]
    public UnityEngine.Events.UnityEvent OnTodasLasInsigniasObtenidas;

    private List<string> pinesCompletados = new List<string>();
    private int totalPinesEnEscena = 0;

    public int TotalPines => totalPinesEnEscena;
    public int PinesCompletados => pinesCompletados.Count;
    public bool TodasCompletadas => pinesCompletados.Count >= totalPinesEnEscena && totalPinesEnEscena > 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        InicializarSlots();
    }

    public void CalcularTotalPines()
    {
        PinMapa[] todos = FindObjectsByType<PinMapa>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        totalPinesEnEscena = todos.Length;
        Debug.Log($"[InsigniasManager] Total de pines detectados: {totalPinesEnEscena}");
    }

    private void InicializarSlots()
    {
        if (slotsInsignias == null) return;

        foreach (var slot in slotsInsignias)
        {
            if (slot == null) continue;
            slot.sprite = null;
            slot.gameObject.SetActive(!activarImagenAlGanar);
        }
    }

    public bool OtorgarInsignia(string idPin)
    {
        if (string.IsNullOrEmpty(idPin))
        {
            Debug.LogWarning("[InsigniasManager] idPin vacío.");
            return false;
        }

        if (pinesCompletados.Contains(idPin))
        {
            Debug.Log($"[InsigniasManager] Insignia de {idPin} ya otorgada.");
            return false;
        }

        pinesCompletados.Add(idPin);
        ActualizarUIInsignias();

        Debug.Log($"[InsigniasManager] 🏅 Insignia otorgada: {idPin} ({PinesCompletados}/{totalPinesEnEscena})");

        if (TodasCompletadas)
        {
            Debug.Log("[InsigniasManager] 🎉 ¡Todas las insignias obtenidas!");
            OnTodasLasInsigniasObtenidas?.Invoke();
        }

        return true;
    }

    private void ActualizarUIInsignias()
    {
        if (slotsInsignias == null || slotsInsignias.Count == 0) return;

        int index = pinesCompletados.Count - 1;
        if (index < 0 || index >= slotsInsignias.Count) return;

        var slot = slotsInsignias[index];
        if (slot == null) return;

        if (spriteInsignia != null)
            slot.sprite = spriteInsignia;

        if (activarImagenAlGanar)
            slot.gameObject.SetActive(true);
    }

    public bool PinYaCompletado(string idPin) => pinesCompletados.Contains(idPin);

    [ContextMenu("Resetear insignias")]
    public void Resetear()
    {
        pinesCompletados.Clear();
        InicializarSlots();
        Debug.Log("[InsigniasManager] Insignias reseteadas.");
    }
}