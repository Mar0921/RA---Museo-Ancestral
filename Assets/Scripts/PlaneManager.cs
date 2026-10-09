using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

[RequireComponent(typeof(ARPlaneManager))]
public class PlaneManager : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] private ARPlaneManager arPlaneManager;
    [SerializeField] private ARRaycastManager arRaycastManager;

    [Header("Prefabs principales")]
    [SerializeField] private GameObject guiaPrefab;
    [Tooltip("Prefabs de mapas (máx. 5)")]
    [SerializeField] private List<GameObject> mapPrefabs = new List<GameObject>();

    [Header("UI de exhibición")]
    [SerializeField] private GameObject panelExhibicion;
    [SerializeField] private TextMeshProUGUI textoTituloObjeto;
    [SerializeField] private Button botonSalirExhibicion;

    private DocsTouch objetoActualEnExhibicion;

    [Header("Posiciones fijas relativas al guía")]
    [SerializeField]
    private List<Vector3> mapOffsets = new List<Vector3>()
    {
        new Vector3(0f, 0f, 0.5f),
        new Vector3(0.5f, 0f, 0f),
        new Vector3(0.5f, 0f, 0f),
        new Vector3(0.5f, 0f, 0f),
        new Vector3(0.5f, 0f, 0f)
    };

    [Header("Opciones")]
    [SerializeField, Range(0f, 5f)] private float distanceFromCamera = 2f;
    [SerializeField] private bool verbose = true;

    [Header("Configuración de Avance")]
    [SerializeField] private float tiempoEsperaAntesDeObjeto = 1.5f;

    [Header("Espaciado entre pines")]
    [SerializeField] private float distanciaEntrePines = 1.5f;

    private bool todosPinesCompletados = false;
    public bool TodosPinesCompletados => todosPinesCompletados;

    private GameObject guiaInstance;
    private GuiaController guiaController;
    private readonly List<GameObject> mapInstances = new List<GameObject>();
    private int currentMapIndex = 0;
    private int currentPinIndex = 0;
    private bool contentPlaced = false;
    private bool listoParaAvanzar = false;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    [Header("Botón FAB Épocas")]
    [SerializeField] private GameObject fabButton;

    // ============================================================
    // DIAGNÓSTICO
    // ============================================================
    private float timerDiagnostico = 0f;
    private const float INTERVALO_DIAGNOSTICO = 2f;

    void Awake()
    {
        // ============================================================
        // DIAGNÓSTICO INICIAL
        // ============================================================
        Debug.Log("========== [PlaneManager] DIAGNÓSTICO INICIAL ==========");

        // 1. Verificar arPlaneManager
        if (!arPlaneManager)
            arPlaneManager = GetComponent<ARPlaneManager>();

        if (arPlaneManager == null)
            Debug.LogError("[PlaneManager] ❌ arPlaneManager es NULL. No se puede detectar planos.");
        else
            Debug.Log($"[PlaneManager] ✅ arPlaneManager OK: {arPlaneManager.gameObject.name}");

        // 2. Verificar arRaycastManager
        if (!arRaycastManager)
            arRaycastManager = FindFirstObjectByType<ARRaycastManager>();

        if (arRaycastManager == null)
            Debug.LogError("[PlaneManager] ❌ arRaycastManager es NULL. No se puede hacer raycast.");
        else
            Debug.Log($"[PlaneManager] ✅ arRaycastManager OK: {arRaycastManager.gameObject.name}");

        // 3. Verificar guiaPrefab
        if (guiaPrefab == null)
        {
            Debug.LogError("[PlaneManager] ❌ guiaPrefab es NULL. " +
                           "Asigna el prefab del guía en el Inspector.");
        }
        else
        {
            Debug.Log($"[PlaneManager] ✅ guiaPrefab asignado: {guiaPrefab.name}");

            GuiaController gc = guiaPrefab.GetComponent<GuiaController>();
            if (gc == null)
            {
                Debug.LogError("[PlaneManager] ❌ El prefab del guía NO tiene el componente GuiaController.");
            }
            else
            {
                Debug.Log("[PlaneManager] ✅ El prefab del guía tiene GuiaController.");
            }
        }

        // 4. Verificar mapPrefabs
        if (mapPrefabs == null || mapPrefabs.Count == 0)
        {
            Debug.LogError("[PlaneManager] ❌ mapPrefabs está vacío. Asigna los prefabs de mapas.");
        }
        else
        {
            int nulos = 0;
            for (int i = 0; i < mapPrefabs.Count; i++)
            {
                if (mapPrefabs[i] == null)
                {
                    Debug.LogError($"[PlaneManager] ❌ mapPrefabs[{i}] es NULL.");
                    nulos++;
                }
                else
                {
                    Debug.Log($"[PlaneManager] ✅ mapPrefabs[{i}]: {mapPrefabs[i].name}");
                }
            }
            if (nulos == 0)
                Debug.Log($"[PlaneManager] ✅ Los {mapPrefabs.Count} prefabs de mapas están asignados.");
        }

        // 5. Verificar ARSession
        ARSession arSession = FindFirstObjectByType<ARSession>();
        if (arSession == null)
            Debug.LogError("[PlaneManager] ❌ No hay ARSession en la escena.");
        else
            Debug.Log($"[PlaneManager] ✅ ARSession encontrado. enabled = {arSession.enabled}");

        // 6. Verificar cámara AR
        Camera arCamara = Camera.main;
        if (arCamara == null)
            Debug.LogError("[PlaneManager] ❌ No hay Camera.main en la escena.");
        else
            Debug.Log($"[PlaneManager] ✅ Camera.main OK: {arCamara.name}, enabled = {arCamara.enabled}");

        Debug.Log("========== FIN DIAGNÓSTICO INICIAL ==========");
    }

    void Start()
    {
        arPlaneManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;

        if (verbose)
            Debug.Log("[PlaneManager] Buscando plano horizontal frente al usuario...");
    }

    void Update()
    {
        if (contentPlaced) return;

        timerDiagnostico += Time.deltaTime;
        if (timerDiagnostico >= INTERVALO_DIAGNOSTICO)
        {
            timerDiagnostico = 0f;
            DiagnosticoPeriodico();
        }

        TryPlaceContentInFrontOfUser();
    }

    // ============================================================
    // DIAGNÓSTICO PERIÓDICO
    // ============================================================
    private void DiagnosticoPeriodico()
    {
        Debug.Log("---------- [PlaneManager] DIAGNÓSTICO PERIÓDICO ----------");

        Transform cam = Camera.main?.transform;
        if (cam == null)
        {
            Debug.LogError("[PlaneManager] ❌ Camera.main es NULL.");
            return;
        }
        Debug.Log($"[PlaneManager] ✅ Cámara en: {cam.position}");

        if (arRaycastManager == null)
        {
            Debug.LogError("[PlaneManager] ❌ arRaycastManager es NULL.");
            return;
        }

        if (arPlaneManager != null)
        {
            int planosDetectados = 0;
            foreach (var plane in arPlaneManager.trackables)
                planosDetectados++;

            Debug.Log($"[PlaneManager] Planos detectados hasta ahora: {planosDetectados}");
        }

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        List<ARRaycastHit> hitsDiag = new List<ARRaycastHit>();
        bool hitEncontrado = arRaycastManager.Raycast(screenCenter, hitsDiag, TrackableType.PlaneWithinPolygon);

        if (hitEncontrado)
        {
            Debug.Log($"[PlaneManager] ✅ Raycast central encontró {hitsDiag.Count} hit(s). " +
                      $"Posición: {hitsDiag[0].pose.position}");
        }
        else
        {
            Debug.LogWarning("[PlaneManager] ⚠️ Raycast central NO encontró plano. " +
                             "Mueve el teléfono lentamente sobre una superficie con textura.");
        }

        if (guiaPrefab == null)
            Debug.LogError("[PlaneManager] ❌ guiaPrefab es NULL.");
        else if (guiaPrefab.GetComponent<GuiaController>() == null)
            Debug.LogError("[PlaneManager] ❌ guiaPrefab no tiene GuiaController.");
        else
            Debug.Log("[PlaneManager] ✅ guiaPrefab listo.");

        Debug.Log("---------- FIN DIAGNÓSTICO PERIÓDICO ----------");
    }

    private void TryPlaceContentInFrontOfUser()
    {
        if (arRaycastManager == null)
        {
            Debug.LogError("[PlaneManager] ❌ ARRaycastManager no encontrado.");
            return;
        }

        Transform cam = Camera.main?.transform;
        if (cam == null)
        {
            Debug.LogError("[PlaneManager] ❌ No se encontró la cámara principal.");
            return;
        }

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        if (arRaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            Debug.Log("[PlaneManager] ✅ Plano detectado. Iniciando instanciación...");

            ARRaycastHit hit = hits[0];
            Vector3 forward = cam.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 targetPosition = cam.position + forward * distanceFromCamera;
            targetPosition.y = hit.pose.position.y;

            PlaceContentAtPosition(targetPosition);

            arPlaneManager.requestedDetectionMode = PlaneDetectionMode.None;
            foreach (var plane in arPlaneManager.trackables)
                plane.gameObject.SetActive(false);

            contentPlaced = true;
        }
    }

    private void PlaceContentAtPosition(Vector3 position)
    {
        Debug.Log("========== [PlaneManager] PLACE CONTENT ==========");

        if (guiaPrefab == null)
        {
            Debug.LogError("[PlaneManager] ❌ guiaPrefab es NULL. No se puede instanciar.");
            return;
        }

        GuiaController prefabGuiaComponent = guiaPrefab.GetComponent<GuiaController>();
        if (prefabGuiaComponent == null)
        {
            Debug.LogError("[PlaneManager] ❌ guiaPrefab no tiene GuiaController.");
            return;
        }

        Transform cam = Camera.main?.transform;
        if (cam == null)
        {
            Debug.LogError("[PlaneManager] ❌ Camera.main es NULL.");
            return;
        }

        // ============================================================
        // 1. Instanciar el guía
        // ============================================================
        Vector3 directionToGuia = cam.position - position;
        directionToGuia.y = 0;
        Quaternion guiaRotation = Quaternion.LookRotation(directionToGuia);
        guiaInstance = Instantiate(guiaPrefab, position, guiaRotation);
        guiaController = guiaInstance.GetComponent<GuiaController>();

        // ============================================================
        // 2. Activar el panel de subtítulos justo cuando aparece el guía
        // ============================================================
        ActivarPanelSubtitulos();

        if (guiaController == null)
            Debug.LogError("[PlaneManager] ❌ La instancia del guía no tiene GuiaController.");
        else
            Debug.Log($"[PlaneManager] ✅ Guía instanciado en: {position}");

        if (mapPrefabs.Count == 0 || mapPrefabs[0] == null)
        {
            Debug.LogError("[PlaneManager] ❌ No hay prefabs de mapas asignados.");
            return;
        }

        string[] decadas = { "70s", "80s", "90s", "2000s", "2010s" };

        Vector3 offset = (0 < mapOffsets.Count) ? mapOffsets[0] : new Vector3(0f, 0f, 0.5f);
        Vector3 firstMapPos = guiaInstance.transform.position + guiaInstance.transform.TransformDirection(offset);
        firstMapPos.y = position.y;

        Vector3 lookDir = guiaInstance.transform.position - firstMapPos;
        lookDir.y = 0;
        Quaternion rotacionHaciaGuia = Quaternion.LookRotation(lookDir);

        GameObject firstMap = Instantiate(mapPrefabs[0], firstMapPos, rotacionHaciaGuia);
        firstMap.name = "Map_1_70s";

        Transform primerPin = EncontrarPinPrincipalPorNombre(firstMap);
        if (primerPin == null)
        {
            Debug.LogError("[PlaneManager] ❌ No se encontró pin principal en 70s.");
            Destroy(firstMap);
            return;
        }

        Vector3 posicionPinReferencia = primerPin.position;
        float yReferencia = firstMap.transform.position.y;
        Quaternion rotacionReferencia = firstMap.transform.rotation;

        Transform pinPrefabOriginal = EncontrarPinPrincipalPorNombre(mapPrefabs[0]);
        Vector3 posicionOriginalPinPrefab70s = Vector3.zero;
        if (pinPrefabOriginal != null)
            posicionOriginalPinPrefab70s = pinPrefabOriginal.position;

        Debug.Log($"[PlaneManager] Posición guía: {guiaInstance.transform.position}");
        Debug.Log($"[PlaneManager] Posición PIN 70s (REFERENCIA): {posicionPinReferencia}");

        firstMap.SetActive(false);
        mapInstances.Add(firstMap);

        for (int i = 1; i < mapPrefabs.Count && i < 5; i++)
        {
            if (mapPrefabs[i] == null)
            {
                Debug.LogWarning($"[PlaneManager] Prefab {i} es NULL. Saltando.");
                continue;
            }

            Transform pinPrefabActual = EncontrarPinPrincipalPorNombre(mapPrefabs[i]);
            if (pinPrefabActual == null)
            {
                Debug.LogError($"[PlaneManager] ❌ No se encontró pin en el prefab {decadas[i]}.");
                continue;
            }

            Vector3 offsetEntrePinesPrefab = pinPrefabActual.position - posicionOriginalPinPrefab70s;
            Vector3 offsetRotado = rotacionHaciaGuia * offsetEntrePinesPrefab;

            Vector3 targetPinPosition = posicionPinReferencia + offsetRotado;
            targetPinPosition.y = yReferencia;

            GameObject nuevoMapa = Instantiate(mapPrefabs[i], firstMap.transform.position, rotacionReferencia);
            nuevoMapa.name = $"Map_{i + 1}_{decadas[i]}";

            Transform pinNuevo = EncontrarPinPrincipalPorNombre(nuevoMapa);

            if (pinNuevo != null)
            {
                Vector3 offsetPinWorld = pinNuevo.position - nuevoMapa.transform.position;
                Vector3 nuevaPosicionRaiz = targetPinPosition - offsetPinWorld;
                nuevaPosicionRaiz.y = yReferencia;

                nuevoMapa.transform.position = nuevaPosicionRaiz;

                Debug.Log($"[PlaneManager] ✅ Mapa {decadas[i]} colocado. Pin en {pinNuevo.position}");

                nuevoMapa.SetActive(false);
                mapInstances.Add(nuevoMapa);
            }
            else
            {
                Debug.LogError($"[PlaneManager] ❌ No se encontró pin en la instancia de {decadas[i]}.");
                Destroy(nuevoMapa);
            }
        }

        if (mapInstances.Count > 0)
        {
            currentMapIndex = 0;
            currentPinIndex = 0;
            MostrarMapaYPrimerPin();
        }

        InsigniasManager.Instance?.CalcularTotalPines();

        Debug.Log("========== FIN PLACE CONTENT ==========");
    }

    // ============================================================
    // ACTIVAR PANEL DE SUBTÍTULOS
    // ============================================================
    private void ActivarPanelSubtitulos()
    {
        GameObject[] objetos = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in objetos)
        {
            if (!obj.scene.IsValid()) continue;

            // Busca PanelSubtitulos o RespuestaMito (por si aún no lo renombraste)
            if (obj.name == "PanelSubtitulos" || obj.name.Contains("PanelSubtitulos") ||
                obj.name == "RespuestaMito" || obj.name.Contains("RespuestaMito"))
            {
                if (!obj.activeSelf)
                {
                    obj.SetActive(true);
                    Debug.Log($"[PlaneManager] ✅ PanelSubtitulos activado: {obj.name}");
                }
                else
                {
                    Debug.Log($"[PlaneManager] ℹ️ PanelSubtitulos ya estaba activo: {obj.name}");
                }
                return;
            }
        }
        Debug.LogWarning("[PlaneManager] ⚠️ No se encontró PanelSubtitulos ni RespuestaMito en la escena.");
    }

    private Transform EncontrarPinPrincipalPorNombre(GameObject mapa)
    {
        Transform[] todosLosHijos = mapa.GetComponentsInChildren<Transform>(true);
        foreach (Transform hijo in todosLosHijos)
        {
            if (hijo.name.Contains("PinPrincipal"))
                return hijo;
        }
        Debug.LogError($"[EncontrarPinPrincipal] ❌ NO encontrado en '{mapa.name}'");
        return null;
    }

    public bool PuedeAvanzar() => listoParaAvanzar;

    public void NotificarPinCompletado(PinMapa pin)
    {
        if (pin == null) return;

        if (verbose)
            Debug.Log($"[PlaneManager] Pin completado: {pin.name} (orden {pin.OrdenPin})");

        if (currentMapIndex >= mapInstances.Count) return;

        GameObject currentMap = mapInstances[currentMapIndex];
        PinMapa[] pins = currentMap.GetComponentsInChildren<PinMapa>(true);
        System.Array.Sort(pins, (a, b) => a.OrdenPin.CompareTo(b.OrdenPin));

        if (currentPinIndex + 1 < pins.Length)
        {
            currentPinIndex++;
            pins[currentPinIndex].gameObject.SetActive(true);

            if (verbose)
                Debug.Log($"[PlaneManager] Activando siguiente pin: {pins[currentPinIndex].name}, {currentPinIndex + 1}/{pins.Length}");
        }
        else
        {
            if (verbose)
                Debug.Log($"[PlaneManager] 🎉 Todos los pines del mapa {currentMapIndex + 1} completados.");

            StartCoroutine(SecuenciaCompletarMapa(pins));
        }
    }

    private IEnumerator SecuenciaCompletarMapa(PinMapa[] pins)
    {
        listoParaAvanzar = true;
        MarcarTodosPinesComoCompletados(pins);
        yield return new WaitForSeconds(0.3f);

        QuizManagerMito quiz = FindFirstObjectByType<QuizManagerMito>(FindObjectsInactive.Include);
        quiz?.Cerrar();
        yield return new WaitForSeconds(0.2f);

        yield return new WaitForSeconds(tiempoEsperaAntesDeObjeto);

        GameObject mapaActual = mapInstances[currentMapIndex];
        ObjetoInteractivoCambioMapa objetoAvanzar = mapaActual.GetComponentInChildren<ObjetoInteractivoCambioMapa>(true);

        if (objetoAvanzar != null)
            objetoAvanzar.gameObject.SetActive(true);
    }

    private void MarcarTodosPinesComoCompletados(PinMapa[] pins)
    {
        foreach (var pin in pins)
        {
            Transform letrero = pin.transform.Find("Letrero");
            if (letrero != null)
                letrero.gameObject.SetActive(false);
        }
    }

    public void OnObjetoAvanzarClickeado()
    {
        if (!listoParaAvanzar) return;

        if (currentMapIndex < mapInstances.Count)
        {
            GameObject mapaActual = mapInstances[currentMapIndex];
            ObjetoInteractivoCambioMapa objetoAvanzar = mapaActual.GetComponentInChildren<ObjetoInteractivoCambioMapa>(true);

            if (objetoAvanzar != null)
                objetoAvanzar.gameObject.SetActive(false);
        }

        AvanzarAlSiguienteMapa();
    }

    private void OcultarMapaActual()
    {
        if (currentMapIndex >= mapInstances.Count) return;
        mapInstances[currentMapIndex].SetActive(false);
    }

    public void AvanzarAlSiguienteMapa()
    {
        OcultarMapaActual();
        listoParaAvanzar = false;

        currentMapIndex++;
        currentPinIndex = 0;

        if (currentMapIndex < mapInstances.Count)
        {
            if (guiaController != null)
                guiaController.DetenerMovimiento();

            StartCoroutine(CambiarANuevoMapa());
        }
        else
        {
            Debug.Log("[PlaneManager] 🎊 ¡Todos los mapas completados!");
            todosPinesCompletados = true;
            ActivarFABSiUltimoMapa();
        }
    }

    private IEnumerator CambiarANuevoMapa()
    {
        yield return new WaitForEndOfFrame();
        MostrarMapaYPrimerPin();
    }

    public List<GameObject> GetMapas() => mapInstances;
    public int GetCurrentMapIndex() => currentMapIndex;

    public List<string> GetPinesRecorridos(int mapIndex)
    {
        List<string> lista = new List<string>();
        if (mapIndex >= mapInstances.Count) return lista;

        PinMapa[] pins = mapInstances[mapIndex].GetComponentsInChildren<PinMapa>(true);
        System.Array.Sort(pins, (a, b) => a.OrdenPin.CompareTo(b.OrdenPin));

        for (int i = 0; i <= currentPinIndex && i < pins.Length; i++)
            lista.Add(pins[i].name);

        return lista;
    }

    public void IrAlPin(int mapIndex, string pinName)
    {
        if (mapIndex >= mapInstances.Count) return;

        GameObject mapa = mapInstances[mapIndex];
        mapa.SetActive(true);

        PinMapa[] pins = mapa.GetComponentsInChildren<PinMapa>(true);
        foreach (var pin in pins)
            pin.gameObject.SetActive(pin.name == pinName);

        currentMapIndex = mapIndex;
    }

    private void MostrarMapaYPrimerPin()
    {
        if (currentMapIndex >= mapInstances.Count) return;

        GameObject currentMap = mapInstances[currentMapIndex];
        currentMap.SetActive(true);

        PinMapa[] pins = currentMap.GetComponentsInChildren<PinMapa>(true);
        if (pins.Length == 0) return;

        System.Array.Sort(pins, (a, b) => a.OrdenPin.CompareTo(b.OrdenPin));

        foreach (var pin in pins)
            pin.gameObject.SetActive(false);

        if (currentPinIndex < pins.Length)
        {
            pins[currentPinIndex].gameObject.SetActive(true);

            if (verbose)
                Debug.Log($"[PlaneManager] Mostrando mapa {currentMapIndex + 1}, pin {pins[currentPinIndex].name}, {currentPinIndex + 1}/{pins.Length}");
        }
    }

    private string ObtenerNombreMapa(int index)
    {
        string[] nombres = { "70s", "80s", "90s", "2000s", "2010s" };
        return (index >= 0 && index < nombres.Length) ? nombres[index] : $"Mapa {index + 1}";
    }

    private void ActivarFABSiUltimoMapa()
    {
        if (currentMapIndex >= mapInstances.Count && todosPinesCompletados)
        {
            if (fabButton != null)
                fabButton.SetActive(true);
        }
    }

    public void MostrarDatosObjeto(string nombre, DocsTouch objeto)
    {
        if (textoTituloObjeto != null)
            textoTituloObjeto.text = nombre;

        objetoActualEnExhibicion = objeto;
        panelExhibicion?.SetActive(true);
        botonSalirExhibicion?.gameObject.SetActive(true);

        var btn = botonSalirExhibicion.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => objetoActualEnExhibicion?.SalirDeExhibicion());
    }

    public void MostrarColeccionableSinAvanzar()
    {
        if (currentMapIndex >= mapInstances.Count) return;

        GameObject mapaActual = mapInstances[currentMapIndex];
        ObjetoInteractivoCambioMapa objetoAvanzar = mapaActual.GetComponentInChildren<ObjetoInteractivoCambioMapa>(true);

        if (objetoAvanzar != null)
        {
            objetoAvanzar.gameObject.SetActive(true);
            listoParaAvanzar = true;
        }
    }

    public void OcultarPanelExhibicion()
    {
        if (panelExhibicion != null) panelExhibicion.SetActive(false);
        if (botonSalirExhibicion != null) botonSalirExhibicion.gameObject.SetActive(false);
        if (textoTituloObjeto != null) textoTituloObjeto.text = "";
        objetoActualEnExhibicion = null;
    }
}