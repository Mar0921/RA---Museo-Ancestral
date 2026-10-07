using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Menú de configuración.
///
/// Funciones:
/// - BotonConfig: abre configuración.
/// - BotonSalir: cierra configuración.
/// - BotonMute: activa/desactiva el sonido.
/// - SliderVolumen: controla el volumen.
/// - BotonCreditos: abre el panel de créditos.
/// - BotonCerrarCreditos: cierra créditos y vuelve a configuración.
///
/// Al abrir configuración se pausa la cámara AR.
/// Al cerrar configuración se reanuda la cámara AR.
/// </summary>
public class ConfiguracionManager : MonoBehaviour
{
    [Header("=== AR ===")]
    [SerializeField] private ARSession arSession;
    [SerializeField] private Camera arCamera;

    [Header("=== Botones ===")]
    [SerializeField] private Button botonConfig;
    [SerializeField] private Button botonSalir;
    [SerializeField] private Button botonMute;
    [SerializeField] private Button botonCreditos;
    [SerializeField] private Button botonCerrarCreditos;

    [Header("=== Imagen del botón Mute ===")]
    [SerializeField] private Image imagenBotonMute;
    [SerializeField] private Sprite iconoSonidoOn;
    [SerializeField] private Sprite iconoSonidoOff;

    [Header("=== Paneles ===")]
    [SerializeField] private GameObject panelConfiguracion;
    [SerializeField] private GameObject panelCreditos;

    [Header("=== Volumen ===")]
    [SerializeField] private Slider sliderVolumen;

    [Header("=== Créditos ===")]
    [TextArea(3, 10)]
    [SerializeField]
    private string creditos =
        "Experiencia AR\n\n" +
        "Desarrollado por:\n" +
        "- Tu Nombre 1\n" +
        "- Tu Nombre 2\n\n" +
        "Universidad / Empresa\n" +
        "Año";

    private bool menuAbierto = false;
    private bool creditosAbiertos = false;

    private bool muteado = false;
    private float volumenAnterior = 1f;

    private const string KEY_VOLUMEN = "cfg_volumen";
    private const string KEY_MUTE = "cfg_mute";


    private void Start()
    {
        // ==========================================
        // CARGAR CONFIGURACIÓN GUARDADA
        // ==========================================

        volumenAnterior = PlayerPrefs.GetFloat(KEY_VOLUMEN, 1f);

        muteado = PlayerPrefs.GetInt(KEY_MUTE, 0) == 1;

        AudioListener.volume = muteado ? 0f : volumenAnterior;


        // ==========================================
        // ESTADO INICIAL DE LOS PANELES
        // ==========================================

        if (panelConfiguracion != null)
            panelConfiguracion.SetActive(false);

        if (panelCreditos != null)
            panelCreditos.SetActive(false);


        // ==========================================
        // ESTADO INICIAL DE LOS BOTONES
        // ==========================================

        if (botonConfig != null)
            botonConfig.gameObject.SetActive(true);

        if (botonSalir != null)
            botonSalir.gameObject.SetActive(false);


        // ==========================================
        // ACTUALIZAR ICONO DE MUTE
        // ==========================================

        ActualizarIconoMute();


        // ==========================================
        // CONFIGURAR TEXTO DE CRÉDITOS
        // ==========================================

        if (panelCreditos != null)
        {
            TextMeshProUGUI texto =
                panelCreditos.GetComponentInChildren<TextMeshProUGUI>(true);

            if (texto != null)
                texto.text = creditos;
        }


        // ==========================================
        // CONFIGURAR SLIDER
        // ==========================================

        if (sliderVolumen != null)
        {
            sliderVolumen.minValue = 0f;
            sliderVolumen.maxValue = 1f;

            sliderVolumen.value =
                muteado ? 0f : volumenAnterior;

            sliderVolumen.onValueChanged.AddListener(OnVolumenCambiado);
        }


        // ==========================================
        // ASIGNAR EVENTOS A LOS BOTONES
        // ==========================================

        if (botonConfig != null)
            botonConfig.onClick.AddListener(AbrirMenu);

        if (botonSalir != null)
            botonSalir.onClick.AddListener(CerrarMenu);

        if (botonMute != null)
            botonMute.onClick.AddListener(ToggleMute);

        if (botonCreditos != null)
            botonCreditos.onClick.AddListener(AbrirCreditos);

        if (botonCerrarCreditos != null)
            botonCerrarCreditos.onClick.AddListener(CerrarCreditos);
    }


    // =========================================================
    // ABRIR CONFIGURACIÓN
    // =========================================================

    public void AbrirMenu()
    {
        menuAbierto = true;
        creditosAbiertos = false;


        // Mostrar configuración
        if (panelConfiguracion != null)
            panelConfiguracion.SetActive(true);


        // Ocultar créditos
        if (panelCreditos != null)
            panelCreditos.SetActive(false);


        // Ocultar botón de abrir configuración
        if (botonConfig != null)
            botonConfig.gameObject.SetActive(false);


        // Mostrar botón de cerrar
        if (botonSalir != null)
            botonSalir.gameObject.SetActive(true);


        // Pausar AR
        PausarAR(true);
    }


    // =========================================================
    // CERRAR CONFIGURACIÓN
    // =========================================================

    public void CerrarMenu()
    {
        menuAbierto = false;
        creditosAbiertos = false;


        // Ocultar configuración
        if (panelConfiguracion != null)
            panelConfiguracion.SetActive(false);


        // Ocultar créditos
        if (panelCreditos != null)
            panelCreditos.SetActive(false);


        // Mostrar botón de configuración
        if (botonConfig != null)
            botonConfig.gameObject.SetActive(true);


        // Ocultar botón de cerrar
        if (botonSalir != null)
            botonSalir.gameObject.SetActive(false);


        // Reanudar AR
        PausarAR(false);
    }


    // =========================================================
    // ABRIR CRÉDITOS
    // =========================================================

    public void AbrirCreditos()
    {
        creditosAbiertos = true;


        // Ocultar configuración
        if (panelConfiguracion != null)
            panelConfiguracion.SetActive(false);


        // Mostrar créditos
        if (panelCreditos != null)
            panelCreditos.SetActive(true);


        Debug.Log("[ConfiguracionManager] Créditos abiertos.");
    }


    // =========================================================
    // CERRAR CRÉDITOS Y VOLVER A CONFIGURACIÓN
    // =========================================================

    public void CerrarCreditos()
    {
        creditosAbiertos = false;


        // Ocultar créditos
        if (panelCreditos != null)
            panelCreditos.SetActive(false);


        // Volver a mostrar configuración
        if (panelConfiguracion != null)
            panelConfiguracion.SetActive(true);


        Debug.Log("[ConfiguracionManager] Créditos cerrados. Regresando a configuración.");
    }


    // =========================================================
    // PAUSAR / REANUDAR AR
    // =========================================================

    private void PausarAR(bool pausar)
    {
        if (arSession != null)
            arSession.enabled = !pausar;

        if (arCamera != null)
            arCamera.enabled = !pausar;
    }


    // =========================================================
    // CAMBIO DE VOLUMEN CON SLIDER
    // =========================================================

    private void OnVolumenCambiado(float valor)
    {
        // Si estaba muteado y el usuario sube el slider,
        // automáticamente se desmutea.
        if (muteado && valor > 0f)
        {
            muteado = false;

            PlayerPrefs.SetInt(KEY_MUTE, 0);

            ActualizarIconoMute();
        }


        // Guardar volumen actual
        volumenAnterior = valor;

        // Aplicar volumen
        AudioListener.volume = valor;


        // Guardar preferencias
        PlayerPrefs.SetFloat(KEY_VOLUMEN, volumenAnterior);
        PlayerPrefs.Save();
    }


    // =========================================================
    // BOTÓN MUTE
    // =========================================================

    public void ToggleMute()
    {
        muteado = !muteado;


        // ==========================================
        // ACTIVAR MUTE
        // ==========================================

        if (muteado)
        {
            // Guardar el volumen antes de mutear
            if (sliderVolumen != null &&
                sliderVolumen.value > 0f)
            {
                volumenAnterior = sliderVolumen.value;
            }


            // Silenciar
            AudioListener.volume = 0f;


            // Llevar slider a 0
            if (sliderVolumen != null)
            {
                sliderVolumen.SetValueWithoutNotify(0f);
            }
        }


        // ==========================================
        // DESACTIVAR MUTE
        // ==========================================

        else
        {
            // Recuperar volumen anterior
            AudioListener.volume = volumenAnterior;


            // Recuperar posición del slider
            if (sliderVolumen != null)
            {
                sliderVolumen.SetValueWithoutNotify(
                    volumenAnterior
                );
            }
        }


        // Actualizar icono
        ActualizarIconoMute();


        // Guardar configuración
        PlayerPrefs.SetInt(
            KEY_MUTE,
            muteado ? 1 : 0
        );

        PlayerPrefs.SetFloat(
            KEY_VOLUMEN,
            volumenAnterior
        );

        PlayerPrefs.Save();
    }


    // =========================================================
    // ACTUALIZAR ICONO DE MUTE
    // =========================================================

    private void ActualizarIconoMute()
    {
        if (imagenBotonMute == null)
            return;


        imagenBotonMute.sprite =
            muteado
            ? iconoSonidoOff
            : iconoSonidoOn;
    }
}