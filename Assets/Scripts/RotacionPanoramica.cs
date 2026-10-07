using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class RotacionPanoramica : MonoBehaviour
{
    [Header("=== Rotación automática (tap) ===")]
    [Tooltip("Duración de la vuelta completa (360°) en segundos")]
    [SerializeField] private float duracionGiro = 14f;

    [Tooltip("Curva de suavizado (0 = lineal, mayor = más suave inicio/fin)")]
    [SerializeField] private float suavizado = 2f;

    [Header("=== Arrastre manual ===")]
    [SerializeField] private float sensibilidadArrastre = 0.4f;
    [SerializeField] private float inercia = 0.92f;
    [SerializeField] private float inerciaMinima = 5f;
    [SerializeField] private float retardoVuelta = 1.5f;
    [SerializeField] private float velocidadVuelta = 90f;

    [Header("=== Eje ===")]
    [SerializeField] private Vector3 ejeRotacion = Vector3.up;

    [Header("=== Sonidos ===")]
    [SerializeField] private AudioClip sonidoInicio;
    [SerializeField] private AudioClip sonidoDetener;
    [SerializeField] private AudioClip sonidoVuelta;
    [Range(0f, 1f)][SerializeField] private float volumen = 1f;

    [Header("=== Opciones ===")]
    [SerializeField] private bool ignorarUI = true;
    [SerializeField] private float umbralArrastre = 10f;

    // ---- Estado ----
    private Quaternion rotacionOriginal;
    private float anguloAcumulado = 0f;

    // Giro automático
    private bool rotandoAuto = false;
    private float tiempoAuto = 0f;
    private float anguloAutoInicio = 0f;   // desde qué ángulo arranca el giro automático

    // Inercia
    private float velocidadInercia = 0f;
    private float tiempoDesdeSoltar = 0f;

    // Input
    private bool arrastrando = false;
    private Vector2 posicionInicialToque;
    private Vector2 posicionAnteriorToque;
    private int dedoActivo = -1;
    private bool posibleTap = false;

    private AudioSource audioSource;

    private void Awake()
    {
        rotacionOriginal = transform.rotation;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void Update()
    {
        ManejarEntrada();
        ManejarInercia();
        ManejarRotacionAutomatica();
        ManejarVueltaAutomatica();
    }

    // =====================================================
    // ENTRADA
    // =====================================================
    private void ManejarEntrada()
    {
        // ---------- TÁCTIL ----------
        if (Input.touchCount > 0)
        {
            if (dedoActivo >= 0)
            {
                Touch t = BuscarToque(dedoActivo);
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    TerminarInteraccion();
                    dedoActivo = -1;
                }
                else
                {
                    ProcesarMovimiento(t.position);
                }
            }
            else
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Began)
                    {
                        if (ignorarUI && EventSystem.current != null &&
                            EventSystem.current.IsPointerOverGameObject(t.fingerId))
                            continue;

                        if (EsToqueSobreObjeto(t.position))
                        {
                            dedoActivo = t.fingerId;
                            IniciarInteraccion(t.position);
                            break;
                        }
                    }
                }
            }
        }
        // ---------- RATÓN ----------
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (!(ignorarUI && EventSystem.current != null &&
                      EventSystem.current.IsPointerOverGameObject()))
                {
                    if (EsToqueSobreObjeto(Input.mousePosition))
                        IniciarInteraccion(Input.mousePosition);
                }
            }
            else if (Input.GetMouseButton(0) && dedoActivo == -2)
            {
                ProcesarMovimiento(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0) && dedoActivo == -2)
            {
                TerminarInteraccion();
                dedoActivo = -1;
            }
        }
    }

    private Touch BuscarToque(int id)
    {
        for (int i = 0; i < Input.touchCount; i++)
            if (Input.GetTouch(i).fingerId == id) return Input.GetTouch(i);
        Touch fake = new Touch();
        fake.phase = TouchPhase.Ended;
        return fake;
    }

    private void IniciarInteraccion(Vector2 pos)
    {
        // Detener giro automático en curso
        rotandoAuto = false;
        velocidadInercia = 0f;

        posicionInicialToque = pos;
        posicionAnteriorToque = pos;
        arrastrando = false;
        posibleTap = true;   // <-- asumimos que puede ser un tap
        tiempoDesdeSoltar = 0f;

        if (Input.touchCount == 0) dedoActivo = -2;
    }

    private void ProcesarMovimiento(Vector2 posActual)
    {
        Vector2 deltaTotal = posActual - posicionInicialToque;

        if (!arrastrando && deltaTotal.magnitude < umbralArrastre)
            return;

        if (!arrastrando)
        {
            arrastrando = true;
            posibleTap = false;  // <-- se movió, ya NO es tap
            ReproducirSonido(sonidoInicio);
        }

        Vector2 deltaFrame = posActual - posicionAnteriorToque;
        posicionAnteriorToque = posActual;

        float grados = -deltaFrame.x * sensibilidadArrastre;
        AplicarRotacion(grados);

        velocidadInercia = grados / Mathf.Max(Time.deltaTime, 0.0001f);
    }

    private void TerminarInteraccion()
    {
        // ---- Si fue un TAP sin arrastre → activar giro automático ----
        if (posibleTap && !arrastrando)
        {
            IniciarGiroAutomatico();
        }
        else
        {
            // Fue un arrastre: aplicar inercia y luego vuelta
            if (Mathf.Abs(velocidadInercia) < inerciaMinima)
                ReproducirSonido(sonidoDetener);
            tiempoDesdeSoltar = 0f;
        }

        arrastrando = false;
        posibleTap = false;
    }

    // =====================================================
    // GIRO AUTOMÁTICO (tap sin arrastre) → 360° y se queda
    // =====================================================
    private void IniciarGiroAutomatico()
    {
        // Si ya está cerca del origen, hacemos una vuelta completa desde 0
        anguloAutoInicio = anguloAcumulado;
        tiempoAuto = 0f;
        rotandoAuto = true;

        ReproducirSonido(sonidoInicio);
    }

    private void ManejarRotacionAutomatica()
    {
        if (!rotandoAuto) return;

        tiempoAuto += Time.deltaTime;
        float t = Mathf.Clamp01(tiempoAuto / Mathf.Max(duracionGiro, 0.3f));
        float tSuave = Suavizar(t);

        // Interpola desde anguloAutoInicio hasta anguloAutoInicio + 360
        float anguloActual = Mathf.Lerp(anguloAutoInicio, anguloAutoInicio + 360f, tSuave);

        AplicarRotacionAbsoluta(anguloActual);

        if (t >= 1f)
        {
            // Al terminar: normalizar a 0 exacto (misma orientación visual)
            anguloAcumulado = 0f;
            AplicarRotacionAbsoluta(0f);
            rotandoAuto = false;
            ReproducirSonido(sonidoVuelta);
        }
    }

    // =====================================================
    // INERCIA
    // =====================================================
    private void ManejarInercia()
    {
        if (arrastrando) return;
        if (rotandoAuto) return;  // no mezclar con giro auto
        if (Mathf.Abs(velocidadInercia) < inerciaMinima)
        {
            velocidadInercia = 0f;
            return;
        }

        float grados = velocidadInercia * Time.deltaTime;
        AplicarRotacion(grados);

        velocidadInercia *= Mathf.Pow(inercia, Time.deltaTime * 60f);

        if (Mathf.Abs(velocidadInercia) < inerciaMinima)
        {
            velocidadInercia = 0f;
            ReproducirSonido(sonidoDetener);
        }
    }

    // =====================================================
    // VUELTA AUTOMÁTICA tras arrastre
    // =====================================================
    private void ManejarVueltaAutomatica()
    {
        if (arrastrando || rotandoAuto) return;
        if (Mathf.Abs(velocidadInercia) > inerciaMinima) return;
        if (Mathf.Abs(anguloAcumulado) < 0.5f) return;

        tiempoDesdeSoltar += Time.deltaTime;
        if (tiempoDesdeSoltar < retardoVuelta) return;

        float paso = velocidadVuelta * Time.deltaTime;
        float nuevoAngulo = Mathf.MoveTowards(anguloAcumulado, 0f, paso);
        AplicarRotacionAbsoluta(nuevoAngulo);

        if (Mathf.Abs(nuevoAngulo) < 0.01f)
        {
            anguloAcumulado = 0f;
            AplicarRotacionAbsoluta(0f);
            ReproducirSonido(sonidoVuelta);
        }
    }

    // =====================================================
    // UTILIDADES
    // =====================================================
    private void AplicarRotacion(float grados)
    {
        anguloAcumulado += grados;
        anguloAcumulado = Mathf.Repeat(anguloAcumulado + 180f, 360f) - 180f;
        transform.rotation = rotacionOriginal * Quaternion.AngleAxis(anguloAcumulado, ejeRotacion.normalized);
    }

    private void AplicarRotacionAbsoluta(float angulo)
    {
        transform.rotation = rotacionOriginal * Quaternion.AngleAxis(angulo, ejeRotacion.normalized);
    }

    private float Suavizar(float t)
    {
        if (suavizado <= 0f) return t;
        return t * t * (3f - 2f * t);
    }

    private bool EsToqueSobreObjeto(Vector2 posicionPantalla)
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(posicionPantalla);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            return hit.transform == transform || hit.transform.IsChildOf(transform);
        return false;
    }

    private void ReproducirSonido(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, volumen);
    }

    public void ResetearRotacion()
    {
        arrastrando = false;
        rotandoAuto = false;
        velocidadInercia = 0f;
        anguloAcumulado = 0f;
        AplicarRotacionAbsoluta(0f);
    }
}