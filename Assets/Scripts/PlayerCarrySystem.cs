using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gestiona la capacidad de carga del jugador (objetos en las manos).
/// Implementa ICarrier y opera mediante la interfaz ICarryable.
/// </summary>
public class PlayerCarrySystem : MonoBehaviour, ICarrier
{
    [Header("Capacidad de Carga")]
    [Tooltip("Cantidad máxima de objetos que el jugador puede llevar en las manos a la vez.")]
    [SerializeField] private int maxCapacity = 1;

    [Header("Punto de Agarre (Manos)")]
    [Tooltip("Transform donde se posicionarán los objetos cargados (ej. manos o sobre la cabeza).")]
    [SerializeField] private Transform holdPoint;

    [Tooltip("Desplazamiento vertical entre objetos si la capacidad es mayor a 1.")]
    [SerializeField] private Vector3 stackOffset = new Vector3(0f, 0.4f, 0f);

    [Header("Soltar Objetos")]
    [Tooltip("Distancia hacia el frente del jugador donde se soltará el objeto.")]
    [SerializeField] private float dropDistance = 0.8f;

    [Header("Eventos")]
    public UnityEvent<ICarryable> onItemPickedUp;
    public UnityEvent<ICarryable> onItemDropped;

    private List<ICarryable> carriedItems = new List<ICarryable>();

    public int MaxCapacity => maxCapacity;
    public int CurrentCarriedCount => carriedItems.Count;
    public bool IsFull => carriedItems.Count >= maxCapacity;
    public bool HasItems => carriedItems.Count > 0;

    private void Awake()
    {
        if (holdPoint == null)
        {
            holdPoint = transform;
        }
    }

    /// <summary>
    /// Actualiza dinámicamente la capacidad máxima de carga del jugador.
    /// </summary>
    public void SetMaxCapacity(int capacity)
    {
        maxCapacity = Mathf.Max(1, capacity);
        Debug.Log($"[PlayerCarrySystem] Capacidad máxima actualizada a: {maxCapacity}");
    }

    public bool CanCarryMore()
    {
        return carriedItems.Count < maxCapacity;
    }

    public bool PickUp(ICarryable item)
    {
        if (item == null || IsFull) return false;

        carriedItems.Add(item);

        Vector3 targetOffset = stackOffset * (carriedItems.Count - 1);

        item.OnPickedUp(holdPoint);
        item.transform.localPosition += targetOffset;

        Debug.Log($"[PlayerCarrySystem] Objeto '{item.ItemName}' recogido. Capacidad: {carriedItems.Count}/{maxCapacity}");
        onItemPickedUp?.Invoke(item);

        return true;
    }

    public ICarryable GetCarriedItem()
    {
        if (!HasItems) return null;
        return carriedItems[carriedItems.Count - 1];
    }

    public ICarryable TakeCarriedItem()
    {
        if (!HasItems) return null;
        int lastIndex = carriedItems.Count - 1;
        ICarryable item = carriedItems[lastIndex];
        carriedItems.RemoveAt(lastIndex);
        return item;
    }

    public ICarryable DropItem()
    {
        if (!HasItems) return null;

        ICarryable itemToDrop = TakeCarriedItem();

        Vector3 dropPos = transform.position + (Vector3)GetDropDirection();

        itemToDrop.OnDropped(dropPos);

        Debug.Log($"[PlayerCarrySystem] Objeto '{itemToDrop.ItemName}' soltado. Capacidad restante: {carriedItems.Count}/{maxCapacity}");
        onItemDropped?.Invoke(itemToDrop);

        return itemToDrop;
    }

    public void DropAllItems()
    {
        while (HasItems)
        {
            DropItem();
        }
    }

    /// <summary>
    /// Elimina y destruye todos los objetos que el jugador lleva en las manos actualmente.
    /// Útil para la limpieza al finalizar el turno.
    /// </summary>
    public void ClearCarriedItems()
    {
        while (HasItems)
        {
            ICarryable item = TakeCarriedItem();
            if (item != null && item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }
        Debug.Log("[PlayerCarrySystem] Las manos del jugador han sido vaciadas y limpiadas.");
    }

    private Vector2 GetDropDirection()
    {
        TopDownPlayerController2D controller = GetComponent<TopDownPlayerController2D>();
        if (controller != null && controller.FacingDirection != Vector2.zero)
        {
            return controller.FacingDirection.normalized * dropDistance;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null && rb.linearVelocity != Vector2.zero)
        {
            return rb.linearVelocity.normalized * dropDistance;
        }

        return Vector2.down * dropDistance;
    }

    private void OnDrawGizmosSelected()
    {
        if (holdPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(holdPoint.position, 0.2f);
        }
    }
}
