using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class GuiaController : MonoBehaviour
{
    [Header("Movimiento del guía")]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float stoppingDistance = 0.2f;

    [Header("Audio de bienvenida")]
    [SerializeField] private AudioClip audioBienvenida;
    [TextArea]
    [SerializeField] private string textoBienvenida;

    [Header("Animación (opcional si no está en prefab)")]
    [SerializeField] private RuntimeAnimatorController animatorController;

    [Header("Animación de bienvenida")]
    [SerializeField] private string animacionBienvenida = "isWaving";
    [SerializeField] private float duracionAnimacionBienvenida = 2f;

    private Animator animator;
    private AudioSource audioSource;
    private Camera cam;

    private Vector3? targetPos = null;
    private bool reachedTarget = false;
    private PinMapa pinPendiente = null;
    private bool bienvenidaMostrada = false;

    public static AudioSource audioEnReproduccion;

    void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        cam = Camera.main;

        if (animator != null && animator.runtimeAnimatorController == null && animatorController != null)
        {
            animator.runtimeAnimatorController = animatorController;
        }

        if (animator != null && animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"⚠️ {gameObject.name}: Animator sin controller. Animaciones deshabilitadas.");
            animator = null;
        }

        animator?.SetBool("isWalking", false);
        animator?.SetBool("isTalking", false);

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.2f;

        if (audioBienvenida != null)
            StartCoroutine(ReproducirBienvenida());
    }

    void Update()
    {
        HandleClickInput();
        HandleMovement();
    }

    private void HandleClickInput()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TrySetTargetFromScreenPoint(Mouse.current.position.ReadValue());

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            TrySetTargetFromScreenPoint(Touchscreen.current.primaryTouch.position.ReadValue());
    }

    private void TrySetTargetFromScreenPoint(Vector2 screenPos)
    {
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            PinMapa pin = hit.collider.GetComponent<PinMapa>();
            if (pin != null)
            {
                targetPos = new Vector3(pin.transform.position.x, transform.position.y, pin.transform.position.z);
                reachedTarget = false;
                pinPendiente = pin;
            }
        }
    }

    private void HandleMovement()
    {
        if (!targetPos.HasValue)
        {
            animator?.SetBool("isWalking", false);
            return;
        }

        Vector3 dir = targetPos.Value - transform.position;
        dir.y = 0f;
        if (dir.magnitude > stoppingDistance)
        {
            transform.position += dir.normalized * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotateSpeed * Time.deltaTime);
            animator?.SetBool("isWalking", true);
            return;
        }

        if (!reachedTarget)
        {
            reachedTarget = true;
            animator?.SetBool("isWalking", false);
            StartCoroutine(RotateToCamera());

            if (pinPendiente != null)
            {
                pinPendiente.OnGuiaLlego(this);
                pinPendiente = null;
            }

            targetPos = null;
        }
    }

    private IEnumerator RotateToCamera()
    {
        if (cam == null) yield break;

        Quaternion start = transform.rotation;

        Vector3 flatCamPos = new Vector3(cam.transform.position.x, transform.position.y, cam.transform.position.z);
        Vector3 lookDir = flatCamPos - transform.position;

        Quaternion target = Quaternion.LookRotation(lookDir);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * rotateSpeed / 2f;
            transform.rotation = Quaternion.Slerp(start, target, t);
            yield return null;
        }
    }

    private IEnumerator ReproducirBienvenida()
    {
        bienvenidaMostrada = true;

        animator?.SetBool("isWaving", true);

        audioSource.clip = audioBienvenida;
        audioSource.Play();

        if (SubtitulosMito.Instance != null)
            SubtitulosMito.Instance.MostrarTexto(textoBienvenida);

        yield return new WaitForSeconds(2f);

        animator?.SetBool("isWaving", false);
        animator?.SetBool("isTalking", true);

        yield return new WaitForSeconds(audioBienvenida.length - 2f);

        animator?.SetBool("isTalking", false);
    }

    public void SetTalking(bool estado)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool("isTalking", estado);
        }
        else
        {
            Debug.LogWarning($"⚠️ No se puede activar animación de hablar en {gameObject.name}: Animator no configurado correctamente");
        }
    }

    public void TeletransportarA(Vector3 nuevaPosicion)
    {
        StopAllCoroutines();
        transform.position = nuevaPosicion;

        Transform cam = Camera.main?.transform;
        if (cam != null)
        {
            Vector3 lookDir = cam.position - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        Debug.Log($"[GuiaController] Guía teletransportado a {nuevaPosicion}");
    }

    public void DetenerMovimiento()
    {
        StopAllCoroutines();

        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("isMoving", false);
            animator.SetBool("isWalking", false);
            animator.SetBool("isWaving", false);
        }
    }
}