using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownPlayerController2D : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Interacción")]
    [Tooltip("Punto opcional de interacción (ej. objeto hijo en las manos). Si está vacío, usa la posición del jugador + Offset.")]
    [SerializeField] private Transform interactionPoint;
    [Tooltip("Desplazamiento del centro de interacción relativo al jugador.")]
    [SerializeField] private Vector2 interactionOffset = new Vector2(0f, 0.5f);
    [Tooltip("Radio/Tamaño del área circular de interacción.")]
    [SerializeField] private float interactionRadius = 1.2f;
    [SerializeField] private LayerMask interactableLayer = ~0;

    [Header("Rotación")]
    [SerializeField] private bool rotateTowardsMovement = true;
    [SerializeField] private float rotationSpeed = 15f;
    [Tooltip("Ajuste de ángulo según el sprite: -90 si mira hacia Arriba por defecto, 0 si mira hacia la Derecha.")]
    [SerializeField] private float spriteForwardOffset = -90f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 facingDirection = Vector2.down;

    public Vector2 FacingDirection => facingDirection;

    public Vector3 GetInteractionCenter()
    {
        if (interactionPoint != null)
        {
            return interactionPoint.position;
        }
        return transform.position + transform.TransformDirection((Vector3)interactionOffset);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
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

        if (moveInput != Vector2.zero)
        {
            facingDirection = moveInput;
        }

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

        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    private void TryInteract()
    {
        ICarrier carrier = GetComponent<ICarrier>();
        Vector3 center = GetInteractionCenter();

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(center, interactionRadius, interactableLayer);

        IInteractable closestInteractable = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider2D col in hitColliders)
        {
            ICarryable item = col.GetComponent<ICarryable>();
            if (item != null && item.IsBeingCarried) continue;

            IInteractable interactable = col.GetComponent<IInteractable>() ?? col.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                float distance = Vector2.Distance(center, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = interactable;
                }
            }
        }

        if (closestInteractable != null)
        {
            if (closestInteractable is ICarryable itemInWorld && carrier != null && carrier.IsFull)
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

            closestInteractable.Interact();
        }
        else if (carrier != null && carrier.HasItems)
        {
            carrier.DropItem();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GetInteractionCenter(), interactionRadius);
    }
}
