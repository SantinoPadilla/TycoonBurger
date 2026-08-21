using UnityEngine;

/// <summary>
/// Componente para objetos 2D que el jugador puede recoger, llevar en las manos y soltar.
/// Implementa ICarryable e IInteractable.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HoldableItem : MonoBehaviour, ICarryable, IInteractable
{
    [Header("Configuración del Objeto")]
    [SerializeField] private string itemName = "Objeto";
    [SerializeField] private Vector3 holdOffset = Vector3.zero;
    [Tooltip("(Opcional) Slot asignado en el Inspector a donde se moverá este ingrediente al estar cocinado (Cooked).")]
    [SerializeField] private Transform cookedTargetSlot;

    private Collider2D itemCollider;
    private Rigidbody2D itemRigidbody;
    private bool isBeingCarried = false;
    private Vector3 originalScale = Vector3.one;

    public bool IsBeingCarried => isBeingCarried;
    public string ItemName => itemName;
    public Vector3 OriginalScale => originalScale;

    private void Awake()
    {
        itemCollider = GetComponent<Collider2D>();
        itemRigidbody = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;
    }

    /// <summary>
    /// Coloca el objeto en un punto de destino (como la plancha o mesa de armado) manteniendo su escala e identidad de rotación.
    /// </summary>
    public void PlaceAtPoint(Transform targetPoint)
    {
        if (originalScale == Vector3.zero) originalScale = transform.localScale;

        transform.SetParent(targetPoint);
        transform.localPosition = Vector3.zero;
        transform.rotation = Quaternion.identity;

        Vector3 targetLossy = targetPoint.lossyScale;
        if (targetLossy.x != 0 && targetLossy.y != 0 && targetLossy.z != 0)
        {
            transform.localScale = new Vector3(
                originalScale.x / targetLossy.x,
                originalScale.y / targetLossy.y,
                originalScale.z / targetLossy.z
            );
        }
        else
        {
            transform.localScale = originalScale;
        }
    }

    /// <summary>
    /// Llamado al interactuar con E en el mundo.
    /// </summary>
    public void Interact()
    {
        if (isBeingCarried) return;

        ICarrier carrier = FindFirstObjectByType<PlayerCarrySystem>();

        // Si el jugador sostiene un objeto en la mano y este ítem está dentro de una estación, redirigir interacción a la estación
        if (carrier != null && carrier.HasItems)
        {
            IInteractable parentStation = transform.parent != null ? transform.parent.GetComponentInParent<IInteractable>() : null;
            if (parentStation != null && !(parentStation is HoldableItem))
            {
                parentStation.Interact();
                return;
            }
        }

        // Si es un ingrediente cocinado y tiene un slot asignado, se mueve al slot
        Ingredient ingredient = GetComponent<Ingredient>();
        if (ingredient != null && ingredient.CurrentState == CookingState.Cooked && cookedTargetSlot != null)
        {
            PlaceAtPoint(cookedTargetSlot);
            if (itemCollider != null) itemCollider.enabled = true;
            Debug.Log($"[HoldableItem] '{itemName}' (Cooked) movido al slot asignado '{cookedTargetSlot.name}'.");
            return;
        }

        if (carrier != null && carrier.CanCarryMore())
        {
            carrier.PickUp(this);
        }
    }

    public string GetInteractPrompt()
    {
        return isBeingCarried ? "" : $"Recoger {itemName}";
    }

    public void OnPickedUp(Transform holdPoint)
    {
        isBeingCarried = true;

        if (originalScale == Vector3.zero) originalScale = transform.localScale;

        if (itemCollider != null) itemCollider.enabled = false;
        if (itemRigidbody != null)
        {
            itemRigidbody.isKinematic = true;
            itemRigidbody.linearVelocity = Vector2.zero;
            itemRigidbody.angularVelocity = 0f;
        }

        transform.SetParent(holdPoint);
        transform.localPosition = holdOffset;
        transform.localRotation = Quaternion.identity;

        Vector3 parentLossy = holdPoint.lossyScale;
        if (parentLossy.x != 0 && parentLossy.y != 0 && parentLossy.z != 0)
        {
            transform.localScale = new Vector3(
                originalScale.x / parentLossy.x,
                originalScale.y / parentLossy.y,
                originalScale.z / parentLossy.z
            );
        }
        else
        {
            transform.localScale = originalScale;
        }
    }

    public void OnDropped(Vector3 dropPosition)
    {
        isBeingCarried = false;

        transform.SetParent(null);
        transform.position = dropPosition;
        transform.localScale = originalScale;

        if (itemCollider != null) itemCollider.enabled = true;
        if (itemRigidbody != null)
        {
            itemRigidbody.isKinematic = false;
        }
    }
}
