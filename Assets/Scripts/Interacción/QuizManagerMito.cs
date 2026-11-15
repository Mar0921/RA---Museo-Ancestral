using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class QuizManagerMito : MonoBehaviour
{
    public static QuizManagerMito Instance;

    [Header("UI - Panel de preguntas (PanelPreguntasMito)")]
    [SerializeField] private GameObject panelPreguntas;

    [Header("Botones de opciones (Pregunta 1 y Pregunta 2)")]
    [SerializeField] private Button botonRespuestaA;
    [SerializeField] private Button botonRespuestaB;
    [SerializeField] private TextMeshProUGUI textoRespuestaA;
    [SerializeField] private TextMeshProUGUI textoRespuestaB;

    [Header("UI - Feedback")]
    [SerializeField] private GameObject panelFelicitaciones;
    [SerializeField] private GameObject panelIncorrecto;
    [SerializeField] private float duracionFeedback = 2f;

    [Header("Insignia (opcional)")]
    [SerializeField] private GameObject animacionInsignia;

    [Header("Animación del guía")]
    [SerializeField] private string animacionPensar = "isThinking";
    [SerializeField] private float tiempoPensando = 1.5f;

    [Header("Audios de feedback (fallback)")]
    [SerializeField] private AudioClip audioAcierto;
    [SerializeField] private AudioClip audioFallo;
    [SerializeField] private AudioSource audioSource;

    private DatosPregunta datosActuales;
    private PinMapa pinActual;
    private GuiaController guiaActual;
    private Animator guiaAnimator;

    private int oportunidad = 1;
    private bool esperandoRespuesta = false;
    private bool _botonCorrectoEsA;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (panelPreguntas != null) panelPreguntas.SetActive(false);
        if (panelFelicitaciones != null) panelFelicitaciones.SetActive(false);
        if (panelIncorrecto != null) panelIncorrecto.SetActive(false);
        if (animacionInsignia != null) animacionInsignia.SetActive(false);

        if (botonRespuestaA != null) botonRespuestaA.onClick.AddListener(() => Responder(true));
        if (botonRespuestaB != null) botonRespuestaB.onClick.AddListener(() => Responder(false));

        // Diagnóstico del AudioSource
        Debug.Log("========== [QuizManagerMito] DIAGNÓSTICO INICIAL ==========");
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError("[QuizManagerMito] ❌ No hay AudioSource en este GameObject. " +
                               "Añade uno con Add Component → Audio Source.");
            }
            else
            {
                Debug.Log("[QuizManagerMito] ✅ AudioSource encontrado en el mismo GameObject.");
            }
        }
        else
        {
            Debug.Log("[QuizManagerMito] ✅ AudioSource asignado en el Inspector.");
        }

        if (audioSource != null)
        {
            Debug.Log($"[QuizManagerMito] audioSource.enabled: {audioSource.enabled}");
            Debug.Log($"[QuizManagerMito] audioSource.playOnAwake: {audioSource.playOnAwake}");
            Debug.Log($"[QuizManagerMito] audioSource.volume: {audioSource.volume}");
            Debug.Log($"[QuizManagerMito] audioSource.mute: {audioSource.mute}");
            Debug.Log($"[QuizManagerMito] audioSource.spatialBlend: {audioSource.spatialBlend}");
        }

        Debug.Log($"[QuizManagerMito] AudioListener.volume: {AudioListener.volume}");
        Debug.Log($"[QuizManagerMito] AudioListener.pause: {AudioListener.pause}");
        Debug.Log("========== FIN DIAGNÓSTICO INICIAL ==========");
    }

    public void IniciarDesafio(PinMapa pin, GuiaController guia)
    {
        if (pin == null) { Debug.LogError("[QuizManagerMito] Pin nulo."); return; }

        pinActual = pin;
        guiaActual = guia;
        oportunidad = 1;

        datosActuales = LectorPreguntas.instance?.ObtenerDatosPorID(pin.idPin);
        if (datosActuales == null)
        {
            Debug.LogError($"[QuizManagerMito] No hay preguntas para {pin.idPin}.");
            return;
        }

        if (guiaActual != null)
            guiaAnimator = guiaActual.GetComponent<Animator>();

        StartCoroutine(SecuenciaPregunta());
    }

    private IEnumerator SecuenciaPregunta()
    {
        esperandoRespuesta = true;

        if (datosActuales == null)
        {
            Debug.LogError("[QuizManagerMito] datosActuales es null. Abortando.");
            esperandoRespuesta = false;
            yield break;
        }

        SubtitulosMito.Instance?.DetenerSinLimpiar();

        if (guiaAnimator != null) guiaAnimator.SetBool(animacionPensar, true);
        yield return new WaitForSeconds(tiempoPensando);
        if (guiaAnimator != null) guiaAnimator.SetBool(animacionPensar, false);

        string pregunta, correcta, incorrecta;
        if (oportunidad == 1)
        {
            pregunta = datosActuales.pregunta1;
            correcta = datosActuales.respuestaCorrecta1;
            incorrecta = datosActuales.respuestaIncorrecta1;
        }
        else
        {
            pregunta = datosActuales.pregunta2;
            correcta = datosActuales.respuestaCorrecta2;
            incorrecta = datosActuales.respuestaIncorrecta2;
        }

        bool correctaEnA = Random.value < 0.5f;
        _botonCorrectoEsA = correctaEnA;

        SubtitulosMito.Instance?.MostrarTextoFijo(pregunta);

        if (textoRespuestaA != null) textoRespuestaA.text = correctaEnA ? correcta : incorrecta;
        if (textoRespuestaB != null) textoRespuestaB.text = correctaEnA ? incorrecta : correcta;

        if (panelPreguntas != null) panelPreguntas.SetActive(true);
        if (botonRespuestaA != null) botonRespuestaA.interactable = true;
        if (botonRespuestaB != null) botonRespuestaB.interactable = true;

        esperandoRespuesta = false;

        Debug.Log($"[QuizManagerMito] Pregunta {oportunidad} mostrada para {pinActual.idPin}");
    }

    private void Responder(bool esBotonA)
    {
        if (esperandoRespuesta) return;

        Debug.Log($"[QuizManagerMito] Botón presionado: {(esBotonA ? "A" : "B")}");

        if (panelPreguntas != null) panelPreguntas.SetActive(false);
        SubtitulosMito.Instance?.DetenerYLimpiar();

        bool correcto = (esBotonA == _botonCorrectoEsA);
        StartCoroutine(ProcesarRespuesta(correcto));
    }

    private IEnumerator ProcesarRespuesta(bool correcto)
    {
        esperandoRespuesta = true;

        if (botonRespuestaA != null) botonRespuestaA.interactable = false;
        if (botonRespuestaB != null) botonRespuestaB.interactable = false;

        // Referencia local: pinActual puede anularse si algo llama a Cerrar()
        PinMapa pin = pinActual;

        // ============================================================
        // DATOS DE RESPUESTA
        // ============================================================
        DatosRespuesta datosResp = LectorRespuestas.instance?.ObtenerDatosPorID(pin.idPin);
        string textoRespuesta = null;
        AudioClip audioRespuesta = null;

        if (correcto)
        {
            if (datosResp != null) textoRespuesta = datosResp.respuesta1;
            audioRespuesta = pin.AudioRespuestaCorrecta;
        }
        else
        {
            if (datosResp != null) textoRespuesta = datosResp.respuesta2;
            audioRespuesta = pin.AudioRespuestaIncorrecta;
        }

        // ============================================================
        // LOGS DE DIAGNÓSTICO
        // ============================================================
        Debug.Log("========== [QuizManagerMito] DIAGNÓSTICO RESPUESTA ==========");
        Debug.Log($"[QuizManagerMito] Correcto? {correcto}");
        Debug.Log($"[QuizManagerMito] pinActual: {pin.idPin}");
        Debug.Log($"[QuizManagerMito] datosResp es null? {datosResp == null}");
        Debug.Log($"[QuizManagerMito] textoRespuesta: '{textoRespuesta}'");
        Debug.Log($"[QuizManagerMito] audioRespuesta es null? {audioRespuesta == null}");
        if (audioRespuesta != null)
            Debug.Log($"[QuizManagerMito] audioRespuesta.name: {audioRespuesta.name}, length: {audioRespuesta.length}");
        Debug.Log($"[QuizManagerMito] audioSource es null? {audioSource == null}");
        if (audioSource != null)
        {
            Debug.Log($"[QuizManagerMito] audioSource.enabled: {audioSource.enabled}");
            Debug.Log($"[QuizManagerMito] audioSource.volume: {audioSource.volume}");
            Debug.Log($"[QuizManagerMito] audioSource.mute: {audioSource.mute}");
            Debug.Log($"[QuizManagerMito] audioSource.gameObject: {audioSource.gameObject.name}");
        }
        Debug.Log($"[QuizManagerMito] AudioListener.volume: {AudioListener.volume}");
        Debug.Log($"[QuizManagerMito] AudioListener.pause: {AudioListener.pause}");
        Debug.Log("========== FIN DIAGNÓSTICO ==========");

        // ============================================================
        // REPRODUCIR AUDIO + SUBTÍTULOS (con botón Saltar)
        // ============================================================
        var skip = BotonSaltarNarracion.Instance;

        if (audioRespuesta != null && audioSource != null)
        {
            Debug.Log($"[QuizManagerMito] 🔊 Reproduciendo: {audioRespuesta.name}");

            if (!string.IsNullOrEmpty(textoRespuesta) && SubtitulosMito.Instance != null)
                SubtitulosMito.Instance.MostrarSubtitulosConAudio(textoRespuesta, audioRespuesta);

            skip?.Activar();
            audioSource.PlayOneShot(audioRespuesta);
            yield return EsperarConSalto(audioRespuesta.length, skip);

            if (skip != null && skip.SaltoSolicitado)
            {
                audioSource.Stop();
                SubtitulosMito.Instance?.DetenerYLimpiar();
            }
            skip?.Desactivar();
        }
        else if (!string.IsNullOrEmpty(textoRespuesta) && SubtitulosMito.Instance != null)
        {
            Debug.LogWarning("[QuizManagerMito] ⚠️ No hay audio, mostrando solo texto.");
            SubtitulosMito.Instance.MostrarTexto(textoRespuesta);

            skip?.Activar();
            yield return EsperarConSalto(3f, skip);

            if (skip != null && skip.SaltoSolicitado)
                SubtitulosMito.Instance?.DetenerYLimpiar();
            skip?.Desactivar();
        }
        else
        {
            Debug.LogWarning("[QuizManagerMito] ⚠️ No hay audio ni texto de respuesta.");
            AudioClip fallback = correcto ? audioAcierto : audioFallo;
            if (fallback != null && audioSource != null)
            {
                audioSource.PlayOneShot(fallback);
                yield return new WaitForSeconds(fallback.length);
            }
        }

        // ============================================================
        // RESTO DEL FLUJO
        // ============================================================
        if (correcto)
        {
            // 1) Felicitación + insignia
            if (panelFelicitaciones != null) panelFelicitaciones.SetActive(true);
            if (animacionInsignia != null) animacionInsignia.SetActive(true);

            InsigniasManager.Instance?.OtorgarInsignia(pin.idPin);

            yield return new WaitForSeconds(duracionFeedback);

            if (panelFelicitaciones != null) panelFelicitaciones.SetActive(false);
            if (animacionInsignia != null) animacionInsignia.SetActive(false);

            // 2) Objeto del mito en detalle (rotar / zoom / cerrar)
            if (pin.ObjetoDetalle != null)
            {
                if (VisorObjetoDetalle.Instance != null)
                {
                    yield return VisorObjetoDetalle.Instance.MostrarYEsperar(
                        pin.ObjetoDetalle, pin.TituloObjeto, pin.DescripcionObjeto);
                }
                else
                {
                    Debug.LogWarning("[QuizManagerMito] ⚠️ Hay objeto de detalle pero no existe VisorObjetoDetalle en la escena.");
                }
            }

            // 3) Continuar con los demás pines
            var manager = FindFirstObjectByType<PlaneManager>();
            manager?.NotificarPinCompletado(pin);

            Cerrar();
        }
        else
        {
            if (panelIncorrecto != null) panelIncorrecto.SetActive(true);
            yield return new WaitForSeconds(duracionFeedback);
            if (panelIncorrecto != null) panelIncorrecto.SetActive(false);

            if (oportunidad == 1)
            {
                oportunidad = 2;
                StartCoroutine(SecuenciaPregunta());
            }
            else
            {
                RepetirPin();
            }
        }
    }

    /// <summary>
    /// Espera 'segundos' con tiempo ESCALADO (se congela si el menú pone timeScale = 0,
    /// igual que el audio pausado), o hasta que se presione "Saltar".
    /// </summary>
    private IEnumerator EsperarConSalto(float segundos, BotonSaltarNarracion skip)
    {
        float t = 0f;
        while (t < segundos)
        {
            if (skip != null && skip.SaltoSolicitado) yield break;
            t += Time.deltaTime;
            yield return null;
        }
    }

    private void RepetirPin()
    {
        if (pinActual != null)
        {
            pinActual.ResetearPin();
            pinActual.OnGuiaLlego(guiaActual);
        }
        Cerrar();
    }

    public void Cerrar()
    {
        StopAllCoroutines();
        esperandoRespuesta = false;

        BotonSaltarNarracion.Instance?.Desactivar();
        VisorObjetoDetalle.Instance?.ForzarCierre();

        if (panelPreguntas != null) panelPreguntas.SetActive(false);
        if (panelFelicitaciones != null) panelFelicitaciones.SetActive(false);
        if (panelIncorrecto != null) panelIncorrecto.SetActive(false);
        if (animacionInsignia != null) animacionInsignia.SetActive(false);

        pinActual = null;
        guiaActual = null;
        datosActuales = null;
    }

    public void CerrarTodo() => Cerrar();
}