using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public enum PlayerRotationMode
{
    MovementDirection,
    MousePosition,
    MovementAndMouse,
    RightStickOrMouse
}

public enum InteractionShapeType
{
    Circle,
    Box
}

public enum InteractionGizmoMode
{
    Always,
    WhenSelected,
    Never
}

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownPlayerController2D : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    private float speedBonus = 0f;

    public float BaseMoveSpeed => moveSpeed;
    public float SpeedBonus => speedBonus;
    public float EffectiveMoveSpeed => moveSpeed + speedBonus;

    /// <summary>
    /// Establéce la bonificación adicional de velocidad otorgada por la mejora de Player Speed.
    /// </summary>
    public void SetSpeedBonus(float bonus)
    {
        speedBonus = Mathf.Max(0f, bonus);
    }

    [Header("Interacción")]
    [Tooltip("Punto opcional de interacción (ej. objeto hijo en las manos). Si está vacío, usa la posición del jugador + Offset.")]
    [SerializeField] private Transform interactionPoint;

    [Tooltip("Forma del área de interacción con las estaciones (Circular o Caja rectangular).")]
    [SerializeField] private InteractionShapeType interactionShape = InteractionShapeType.Circle;

    [Tooltip("Desplazamiento del centro de interacción relativo al jugador.")]
    [SerializeField] private Vector2 interactionOffset = new Vector2(0f, 0.5f);

    [Tooltip("Si se activa, el centro de interacción se desplaza dinámicamente según la dirección hacia la que mira el jugador.")]
    [SerializeField] private bool useFacingDirectionOffset = false;

    [Tooltip("Distancia de desplazamiento frontal en la dirección donde mira el jugador.")]
    [SerializeField] private float facingDistanceOffset = 0.5f;

    [Tooltip("Radio del área circular de interacción.")]
    [SerializeField] private float interactionRadius = 1.2f;

    [Tooltip("Dimensiones (ancho y alto) de la caja rectangular de interacción.")]
    [SerializeField] private Vector2 interactionBoxSize = new Vector2(1.5f, 1.5f);

    [Tooltip("Máscara de capas para objetos y estaciones interactuables.")]
    [SerializeField] private LayerMask interactableLayer = ~0;

    [Header("Priorización y Cono Dirección (Anti-Solapamiento)")]
    [Tooltip("Si es verdadero, prioriza objetos dentro del cono frontal de mirada del jugador.")]
    [SerializeField] private bool useConeTargeting = true;

    [Tooltip("Ángulo del cono de interacción frontal en grados (ej. 120°).")]
    [Range(30f, 180f)]
    [SerializeField] private float maxInteractionAngle = 120f;

    [Tooltip("Peso asignado a la alineación frontal con la mirada del jugador.")]
    [Range(0f, 10f)]
    [SerializeField] private float facingWeight = 3.0f;

    [Tooltip("Peso asignado a la cercanía por distancia.")]
    [Range(0f, 10f)]
    [SerializeField] private float distanceWeight = 1.0f;

    [Tooltip("Prioriza dinámicamente acciones según si el jugador lleva las manos llenas o vacías.")]
    [SerializeField] private bool useContextPriority = true;

    [Header("Visualización del Área (Gizmos Editor)")]
    [Tooltip("Modo de visualización del gizmo del área de interacción en la vista Scene.")]
    [SerializeField] private InteractionGizmoMode gizmoDisplayMode = InteractionGizmoMode.WhenSelected;

    [Tooltip("Color con el que se dibuja el área de interacción en la vista Scene.")]
    [SerializeField] private Color gizmoColor = new Color(1f, 0.92f, 0.016f, 0.75f);

    [Tooltip("Si es verdadero, dibuja solo las líneas externas del área; si es falso, la rellena.")]
    [SerializeField] private bool gizmoWireframe = true;

    [Tooltip("Dibuja las líneas de dirección del cono frontal de interacción.")]
    [SerializeField] private bool showConeLines = true;

    [Header("Rotación")]
    [Tooltip("Modo de rotación: Apuntado por ratón 360°, Dirección de movimiento, Híbrido o Stick derecho.")]
    [SerializeField] private PlayerRotationMode rotationMode = PlayerRotationMode.MovementAndMouse;
    [SerializeField] private bool rotateTowardsMovement = true;
    [SerializeField] private float rotationSpeed = 15f;
    [Tooltip("Ajuste de ángulo según el sprite: -90 si mira hacia Arriba por defecto, 0 si mira hacia la Derecha.")]
    [SerializeField] private float spriteForwardOffset = -90f;
    [Tooltip("Paso de ángulo en grados para restringir la rotación (0 = 360° continuo sin restricción de 45°, 15 = 24 direcciones, 22.5 = 16 direcciones, 45 = 8 direcciones).")]
    [SerializeField] private float snapAngleStep = 0f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 facingDirection = Vector2.down;

    public Vector2 FacingDirection => facingDirection;
    public Vector2 MoveInput => moveInput;
    public Rigidbody2D Rb => rb;
    public InteractionShapeType InteractionShape => interactionShape;
    public float InteractionRadius => interactionRadius;
    public Vector2 InteractionBoxSize => interactionBoxSize;
    public Vector2 InteractionOffset => interactionOffset;
    public bool UseFacingDirectionOffset => useFacingDirectionOffset;
    public float FacingDistanceOffset => facingDistanceOffset;

    public Vector3 GetInteractionCenter()
    {
        if (interactionPoint != null)
        {
            return interactionPoint.position;
        }

        Vector3 offset = (Vector3)interactionOffset;
        if (useFacingDirectionOffset && facingDirection != Vector2.zero)
        {
            offset += (Vector3)(facingDirection.normalized * facingDistanceOffset);
        }

        return transform.position + transform.TransformDirection(offset);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Update()
    {
        // Bloquear movimiento e interacción si el restaurante está cerrado o la tienda está abierta
        if ((RestaurantShiftManager.Instance != null && RestaurantShiftManager.Instance.IsClosed) ||
            (ShopUI.Instance != null && ShopUI.Instance.IsOpen))
        {
            moveInput = Vector2.zero;
            return;
        }

        Vector2 input = Vector2.zero;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
        }

        var gamepad = Gamepad.current;
        if (gamepad != null && input == Vector2.zero)
        {
            input = gamepad.leftStick.ReadValue();
        }

        moveInput = input.normalized;

        UpdateFacingDirection(gamepad);

        if (rotateTowardsMovement && facingDirection != Vector2.zero)
        {
            RotateTowardsMovement();
        }

        bool isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        var mouse = Mouse.current;
        bool interactPressed = (mouse != null && mouse.leftButton.wasPressedThisFrame && !isPointerOverUI) ||
                               (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame);

        if (interactPressed)
        {
            TryInteract();
        }
    }

    private void UpdateFacingDirection(Gamepad gamepad)
    {
        Vector2 targetDir = Vector2.zero;

        // 1. Apuntado por Stick Derecho en Gamepad
        if (gamepad != null && (rotationMode == PlayerRotationMode.RightStickOrMouse || rotationMode == PlayerRotationMode.MovementAndMouse))
        {
            Vector2 rightStick = gamepad.rightStick.ReadValue();
            if (rightStick.sqrMagnitude > 0.1f)
            {
                targetDir = rightStick.normalized;
            }
        }

        // 2. Apuntado por Posición del Mouse en el Mundo (360° continuo a cualquier ángulo)
        if (targetDir == Vector2.zero && (rotationMode == PlayerRotationMode.MousePosition || rotationMode == PlayerRotationMode.MovementAndMouse || rotationMode == PlayerRotationMode.RightStickOrMouse))
        {
            Vector2 mouseDir = GetMouseWorldDirection();
            if (mouseDir != Vector2.zero)
            {
                targetDir = mouseDir;
            }
        }

        // 3. Fallback: Dirección de Movimiento (WASD o Left Stick)
        if (targetDir == Vector2.zero && moveInput != Vector2.zero)
        {
            targetDir = moveInput;
        }

        if (targetDir != Vector2.zero)
        {
            if (snapAngleStep > 0f)
            {
                float angle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;
                angle = Mathf.Round(angle / snapAngleStep) * snapAngleStep;
                float rad = angle * Mathf.Deg2Rad;
                targetDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            }

            facingDirection = targetDir;
        }
    }

    private Vector2 GetMouseWorldDirection()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return Vector2.zero;
        }

        Camera mainCam = Camera.main;
        var mouse = Mouse.current;
        if (mainCam != null && mouse != null)
        {
            Vector3 mouseScreenPos = mouse.position.ReadValue();
            mouseScreenPos.z = Mathf.Abs(mainCam.transform.position.z - transform.position.z);
            Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreenPos);
            Vector2 dir = (Vector2)(mouseWorldPos - transform.position);
            if (dir.sqrMagnitude > 0.001f)
            {
                return dir.normalized;
            }
        }
        return Vector2.zero;
    }

    private void RotateTowardsMovement()
    {
        float targetAngle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg + spriteForwardOffset;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

        if (rotationSpeed > 0f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            transform.rotation = targetRotation;
        }
    }

    private void FixedUpdate()
    {
        if ((RestaurantShiftManager.Instance != null && RestaurantShiftManager.Instance.IsClosed) ||
            (ShopUI.Instance != null && ShopUI.Instance.IsOpen))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.MovePosition(rb.position + moveInput * EffectiveMoveSpeed * Time.fixedDeltaTime);
    }

    private void TryInteract()
    {
        ICarrier carrier = GetComponent<ICarrier>();
        Vector3 center = GetInteractionCenter();

        Collider2D[] hitColliders;
        if (interactionShape == InteractionShapeType.Box)
        {
            float angle = (useFacingDirectionOffset && facingDirection != Vector2.zero)
                ? Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg
                : transform.eulerAngles.z;
            hitColliders = Physics2D.OverlapBoxAll(center, interactionBoxSize, angle, interactableLayer);
        }
        else
        {
            hitColliders = Physics2D.OverlapCircleAll(center, interactionRadius, interactableLayer);
        }

        IInteractable bestInteractable = null;
        float bestScore = -Mathf.Infinity;

        foreach (Collider2D col in hitColliders)
        {
            ICarryable item = col.GetComponent<ICarryable>();
            if (item != null && item.IsBeingCarried) continue;

            IInteractable interactable = col.GetComponent<IInteractable>() ?? col.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                float score = CalculateTargetScore(center, col, interactable, carrier);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestInteractable = interactable;
                }
            }
        }

        if (bestInteractable != null)
        {
            if (bestInteractable is ICarryable itemInWorld && carrier != null && carrier.IsFull)
            {
                // Si el ítem interactuable está contenido dentro de una estación, priorizar interactuar con la estación
                IInteractable parentStation = (itemInWorld as MonoBehaviour)?.transform.parent?.GetComponentInParent<IInteractable>();
                if (parentStation != null && !(parentStation is ICarryable))
                {
                    parentStation.Interact();
                    return;
                }

                carrier.DropItem();
                return;
            }

            bestInteractable.Interact();
        }
        else if (carrier != null && carrier.HasItems)
        {
            carrier.DropItem();
        }
    }

    private float CalculateTargetScore(Vector3 center, Collider2D col, IInteractable interactable, ICarrier carrier)
    {
        Vector2 targetPos = col.transform.position;
        Vector2 toTarget = targetPos - (Vector2)center;
        float distance = toTarget.magnitude;
        Vector2 dirToTarget = distance > 0.001f ? toTarget / distance : facingDirection;

        Vector2 facingNorm = facingDirection != Vector2.zero ? facingDirection.normalized : Vector2.down;
        float dot = Vector2.Dot(facingNorm, dirToTarget);
        float angle = Vector2.Angle(facingNorm, dirToTarget);

        float score = 0f;

        // Puntuación por alineación con la mirada y distancia
        score += dot * facingWeight;
        score -= distance * distanceWeight;

        // Penalizar objetos fuera del cono frontal de mirada si está activo
        if (useConeTargeting && angle > maxInteractionAngle * 0.5f)
        {
            score -= 100f;
        }

        // Prioridad contextual según si el jugador lleva items o tiene las manos libres
        if (useContextPriority)
        {
            bool holdingItem = carrier != null && carrier.HasItems;

            if (holdingItem)
            {
                // Al llevar objetos, priorizar depósitos, clientes, slots de entrada/salida y mesas
                if (interactable is CookingGrill || interactable is Freidora || interactable is MesaDeArmado ||
                    interactable is SodaStacion || interactable is PuntoDeVenta || interactable is Customer ||
                    interactable is StationOutputSlot || interactable is StationInputSlot)
                {
                    score += 10f;
                }
            }
            else
            {
                // Con manos libres, priorizar recoger objetos o tomar ítems de ranuras
                if (interactable is ICarryable || interactable is StationOutputSlot || interactable is StationInputSlot || interactable is IngredientContainer)
                {
                    score += 10f;
                }
                else
                {
                    score += 5f;
                }
            }
        }

        return score;
    }

    private void DrawInteractionGizmo()
    {
        Gizmos.color = gizmoColor;
        Vector3 center = GetInteractionCenter();

        if (interactionShape == InteractionShapeType.Box)
        {
            float angle = (useFacingDirectionOffset && facingDirection != Vector2.zero)
                ? Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg
                : transform.eulerAngles.z;

            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, angle), Vector3.one);

            if (gizmoWireframe)
            {
                Gizmos.DrawWireCube(Vector3.zero, interactionBoxSize);
            }
            else
            {
                Gizmos.DrawCube(Vector3.zero, interactionBoxSize);
            }

            Gizmos.matrix = oldMatrix;
        }
        else
        {
            if (gizmoWireframe)
            {
                Gizmos.DrawWireSphere(center, interactionRadius);
            }
            else
            {
                Gizmos.DrawSphere(center, interactionRadius);
            }
        }

        // Dibujar cono frontal de mirada en Scene view
        if (showConeLines && useConeTargeting)
        {
            Vector2 facingNorm = facingDirection != Vector2.zero ? facingDirection.normalized : Vector2.down;
            float halfAngle = maxInteractionAngle * 0.5f;

            Vector3 leftDir = Quaternion.Euler(0, 0, halfAngle) * (Vector3)facingNorm;
            Vector3 rightDir = Quaternion.Euler(0, 0, -halfAngle) * (Vector3)facingNorm;

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            float length = interactionShape == InteractionShapeType.Circle ? interactionRadius : interactionBoxSize.magnitude * 0.5f;
            Gizmos.DrawRay(center, leftDir * length);
            Gizmos.DrawRay(center, rightDir * length);
        }
    }

    private void OnDrawGizmos()
    {
        if (gizmoDisplayMode == InteractionGizmoMode.Always)
        {
            DrawInteractionGizmo();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (gizmoDisplayMode == InteractionGizmoMode.WhenSelected)
        {
            DrawInteractionGizmo();
        }
    }
}
