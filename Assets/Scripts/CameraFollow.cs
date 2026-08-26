using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Script de cámara 2D/3D ultra suave con anticipación según la dirección de movimiento del jugador (Look-Ahead).
/// Incluye bloqueo automático de movimiento de cámara durante interacciones/clics con estaciones para evitar
/// que el ratón se desplace fuera del colisionador y cause jittering.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public enum CameraUpdateMode
    {
        LateUpdate,
        FixedUpdate,
        Update
    }

    [Header("Objetivo")]
    [Tooltip("Transform del jugador u objeto a seguir. Si se deja vacío, se buscará automáticamente.")]
    [SerializeField] private Transform target;

    [Header("Offset Base (Espacio Mundo)")]
    [Tooltip("Offset fijo en el mundo respecto al jugador. Para juegos 2D, Z debe ser -10.")]
    [SerializeField] private Vector3 baseOffset = new Vector3(0f, 0f, -10f);

    [Header("Anticipación por Dirección (Look-Ahead)")]
    [Tooltip("Desplaza la cámara en la dirección hacia la que se mueve el jugador para ver más adelante.")]
    [SerializeField] private bool useLookAhead = true;

    [Tooltip("Distancia máxima de anticipación en la dirección de movimiento.")]
    [SerializeField] private float lookAheadDistance = 2.0f;

    [Tooltip("Tiempo de suavizado para cambiar la dirección de anticipación.")]
    [SerializeField] private float lookAheadSmoothTime = 0.25f;

    [Tooltip("Bloquea el Look-Ahead al hacer clic o mantener interactuado una estación para estabilizar el ratón en pantalla.")]
    [SerializeField] private bool disableLookAheadOnInteraction = true;

    [Tooltip("Mantiene la anticipación hacia la dirección donde mira el jugador solo cuando está libre sin interactuar.")]
    [SerializeField] private bool lookAheadOnFacingWhenIdle = false;

    [Header("Suavizado de Cámara")]
    [Tooltip("Modo de actualización. LateUpdate es el recomendado para cámaras en Unity.")]
    [SerializeField] private CameraUpdateMode updateMode = CameraUpdateMode.LateUpdate;

    [Tooltip("Tiempo de suavizado del seguimiento. Valores menores (ej. 0.08 - 0.15) hacen la cámara más rápida.")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float smoothTime = 0.1f;

    [Tooltip("Garantiza que el Rigidbody2D del jugador use Interpolate para eliminar el jittering de física.")]
    [SerializeField] private bool autoEnableRigidbodyInterpolation = true;

    [Header("Límites del Mapa (Opcional)")]
    [Tooltip("Si está activo, restringe la posición X e Y de la cámara dentro del rectángulo de límites.")]
    [SerializeField] private bool useBounds = false;

    [Tooltip("Si es true, los bordes visibles de la cámara no pasarán los límites (el marco de la pantalla respeta los bordes). Si es false, se limita solo la posición central de la cámara.")]
    [SerializeField] private bool clampCameraEdges = true;

    [SerializeField] private Vector2 minBounds = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 maxBounds = new Vector2(10f, 10f);

    private TopDownPlayerController2D playerController;
    private Rigidbody2D targetRb;
    private Camera targetCam;

    private Vector3 cameraVelocity = Vector3.zero;
    private Vector2 currentLookAhead = Vector2.zero;
    private Vector2 lookAheadVelocity = Vector2.zero;

    private void Awake()
    {
        FindCameraIfNull();
    }

    private void Start()
    {
        FindCameraIfNull();
        FindTargetIfNull();
        SnapToTarget();
    }

    private void OnEnable()
    {
        FindCameraIfNull();
        FindTargetIfNull();
    }

    /// <summary>
    /// Teletransporta instantáneamente la cámara a la posición del jugador sin suavizado.
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) FindTargetIfNull();
        if (target == null) return;

        Vector3 targetPos = CalculateTargetPosition();
        transform.position = targetPos;
        currentLookAhead = Vector2.zero;
        cameraVelocity = Vector3.zero;
    }

    private void Update()
    {
        if (updateMode == CameraUpdateMode.Update)
        {
            FollowTarget(Time.deltaTime);
        }
    }

    private void LateUpdate()
    {
        if (updateMode == CameraUpdateMode.LateUpdate)
        {
            FollowTarget(Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (updateMode == CameraUpdateMode.FixedUpdate)
        {
            FollowTarget(Time.fixedDeltaTime);
        }
    }

    private void FollowTarget(float deltaTime)
    {
        if (target == null)
        {
            FindTargetIfNull();
            if (target == null) return;
        }

        Vector3 desiredPosition = CalculateTargetPosition();

        // Movimiento ultra suave con SmoothDamp
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref cameraVelocity, smoothTime, Mathf.Infinity, deltaTime);
    }

    private Vector3 CalculateTargetPosition()
    {
        // 1. Posición base del objetivo en mundo (independiente de la rotación del jugador)
        Vector3 targetWorldPos = target.position;

        // 2. Calcular vector de anticipación (Look-Ahead) basado en el movimiento activo del jugador
        Vector2 targetLookAhead = Vector2.zero;

        bool isMouseHolding = false;
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed)
        {
            isMouseHolding = true;
        }

        // Si se está haciendo clic o interactuando, se congela el Look-Ahead para mantener el puntero fijo sobre la estación
        if (useLookAhead && !(disableLookAheadOnInteraction && isMouseHolding))
        {
            Vector2 moveDir = Vector2.zero;

            if (playerController != null)
            {
                // Solo aplicar Look-Ahead cuando el jugador se está Moviendo (MoveInput activa)
                moveDir = playerController.MoveInput;

                if (moveDir == Vector2.zero && lookAheadOnFacingWhenIdle && !isMouseHolding)
                {
                    moveDir = playerController.FacingDirection;
                }
            }
            else if (targetRb != null)
            {
                moveDir = targetRb.linearVelocity.normalized;
            }

            if (moveDir.sqrMagnitude > 0.01f)
            {
                targetLookAhead = moveDir.normalized * lookAheadDistance;
            }
        }

        // 3. Suavizar progresivamente la dirección de anticipación
        currentLookAhead = Vector2.SmoothDamp(currentLookAhead, targetLookAhead, ref lookAheadVelocity, lookAheadSmoothTime);

        // 4. Posición final: posición del jugador + offset en mundo + offset de anticipación
        Vector3 finalPos = targetWorldPos + baseOffset + new Vector3(currentLookAhead.x, currentLookAhead.y, 0f);

        // 5. Aplicar límites si están activos
        if (useBounds)
        {
            if (clampCameraEdges)
            {
                FindCameraIfNull();

                float horizExtent = 0f;
                float vertExtent = 0f;

                if (targetCam != null)
                {
                    if (targetCam.orthographic)
                    {
                        vertExtent = targetCam.orthographicSize;
                        horizExtent = vertExtent * targetCam.aspect;
                    }
                    else
                    {
                        float distance = Mathf.Abs(finalPos.z - targetWorldPos.z);
                        if (Mathf.Approximately(distance, 0f)) distance = Mathf.Abs(baseOffset.z);
                        vertExtent = distance * Mathf.Tan(targetCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                        horizExtent = vertExtent * targetCam.aspect;
                    }
                }

                float minX = minBounds.x + horizExtent;
                float maxX = maxBounds.x - horizExtent;
                float minY = minBounds.y + vertExtent;
                float maxY = maxBounds.y - vertExtent;

                if (minX > maxX)
                    finalPos.x = (minBounds.x + maxBounds.x) * 0.5f;
                else
                    finalPos.x = Mathf.Clamp(finalPos.x, minX, maxX);

                if (minY > maxY)
                    finalPos.y = (minBounds.y + maxBounds.y) * 0.5f;
                else
                    finalPos.y = Mathf.Clamp(finalPos.y, minY, maxY);
            }
            else
            {
                finalPos.x = Mathf.Clamp(finalPos.x, minBounds.x, maxBounds.x);
                finalPos.y = Mathf.Clamp(finalPos.y, minBounds.y, maxBounds.y);
            }
        }

        return finalPos;
    }

    private void FindCameraIfNull()
    {
        if (targetCam == null)
        {
            targetCam = GetComponent<Camera>();
            if (targetCam == null)
            {
                targetCam = Camera.main;
            }
        }
    }

    private void FindTargetIfNull()
    {
        if (target == null)
        {
            playerController = FindFirstObjectByType<TopDownPlayerController2D>();
            if (playerController != null)
            {
                target = playerController.transform;
            }
            else
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    target = playerObj.transform;
                    playerController = playerObj.GetComponent<TopDownPlayerController2D>();
                }
            }
        }
        else if (playerController == null)
        {
            playerController = target.GetComponent<TopDownPlayerController2D>();
        }

        if (target != null)
        {
            targetRb = target.GetComponent<Rigidbody2D>();
            if (autoEnableRigidbodyInterpolation && targetRb != null && targetRb.interpolation == RigidbodyInterpolation2D.None)
            {
                targetRb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 baseTarget = target.position + baseOffset;
            Gizmos.DrawWireSphere(baseTarget, 0.3f);

            if (useLookAhead && Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(baseTarget, transform.position);
                Gizmos.DrawWireSphere(transform.position, 0.2f);
            }
        }

        if (useBounds)
        {
            Gizmos.color = Color.cyan;
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) * 0.5f, (minBounds.y + maxBounds.y) * 0.5f, transform.position.z);
            Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0.1f);
            Gizmos.DrawWireCube(center, size);

            if (clampCameraEdges)
            {
                Camera gCam = targetCam != null ? targetCam : GetComponent<Camera>();
                if (gCam == null) gCam = Camera.main;

                if (gCam != null)
                {
                    float vertExtent = gCam.orthographic ? gCam.orthographicSize : (Mathf.Abs(baseOffset.z) * Mathf.Tan(gCam.fieldOfView * 0.5f * Mathf.Deg2Rad));
                    float horizExtent = vertExtent * gCam.aspect;

                    float innerWidth = (maxBounds.x - minBounds.x) - (2f * horizExtent);
                    float innerHeight = (maxBounds.y - minBounds.y) - (2f * vertExtent);

                    if (innerWidth > 0 && innerHeight > 0)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireCube(center, new Vector3(innerWidth, innerHeight, 0.1f));
                    }
                }
            }
        }
    }
}
