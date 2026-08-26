using UnityEngine;

/// <summary>
/// Componente interacturable para slots de entrada acumulables en una estación (ej. carne cocinada en MesaDeArmado).
/// A diferencia de los slots de salida que usan texto UI, este slot acumula los ingredientes visualmente 
/// apilándolos uno arriba del otro usando un desplazamiento vertical configurable (stackOffset).
/// Implements IInteractable.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StationInputSlot : MonoBehaviour, IInteractable
{
    [Header("Referencias de la Estación Propietaria")]
    [Tooltip("Estación principal a la que pertenece este slot (ej. MesaDeArmado). Si no se asigna, se buscará automáticamente.")]
    [SerializeField] private MonoBehaviour stationOwner;

    [Header("Configuración de Apilado Visual")]
    [Tooltip("Desplazamiento vertical relativo por cada objeto acumulado en el slot (ej. (0, 0.4, 0)).")]
    [SerializeField] private Vector3 stackOffset = new Vector3(0f, 0.4f, 0f);

    [Tooltip("Capacidad máxima de objetos que se pueden acumular en este slot.")]
    [SerializeField] private int maxCapacity = 5;

    [Header("Filtros de Ingredientes Aceptados (Opcional)")]
    [SerializeField] private IngredientSO acceptedIngredientSO;
    [SerializeField] private GameObject acceptedPrefab;

    private Collider2D slotCollider;

    public MonoBehaviour StationOwner
    {
        get => stationOwner;
        set => stationOwner = value;
    }

    public Vector3 StackOffset
    {
        get => stackOffset;
        set { stackOffset = value; UpdateStackVisuals(); }
    }

    public int MaxCapacity => maxCapacity;
    public int CurrentCount => transform.childCount;
    public bool IsFull => transform.childCount >= maxCapacity;
    public bool HasItems => transform.childCount > 0;

    private void Awake()
    {
        slotCollider = GetComponent<Collider2D>();
        if (slotCollider != null)
        {
            slotCollider.isTrigger = true;
        }
        else
        {
            BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.8f, 0.8f);
            slotCollider = box;
        }

        if (stationOwner == null)
        {
            FindStationOwner();
        }
    }

    private void OnTransformChildrenChanged()
    {
        UpdateStackVisuals();
    }

    public void FindStationOwner()
    {
        MesaDeArmado mesa = GetComponentInParent<MesaDeArmado>();
        if (mesa != null) { stationOwner = mesa; return; }

        CookingGrill grill = GetComponentInParent<CookingGrill>();
        if (grill != null) { stationOwner = grill; return; }
    }

    /// <summary>
    /// Recalcula y aplica las posiciones locales de todos los ingredientes hijos en el slot
    /// apilándolos verticalmente uno arriba del otro.
    /// </summary>
    public void UpdateStackVisuals()
    {
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            // Aplicar offset vertical y pequeño ajuste Z para orden de renderizado en 2D
            child.localPosition = stackOffset * i + new Vector3(0f, 0f, -0.01f * i);
            child.localRotation = Quaternion.identity;

            Collider2D col = child.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
        }
    }

    /// <summary>
    /// Determina si un objeto llevable es aceptado en este slot de entrada.
    /// </summary>
    public bool IsItemAccepted(ICarryable item)
    {
        if (item == null) return false;
        GameObject obj = item.gameObject;

        // 1. Validar por Prefab específico asignado en Inspector
        if (acceptedPrefab != null)
        {
            string objName = obj.name.Replace("(Clone)", "").Trim();
            if (!objName.Equals(acceptedPrefab.name, System.StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // 2. Validar por ScriptableObject asignado en Inspector
        if (acceptedIngredientSO != null)
        {
            Ingredient ing = obj.GetComponent<Ingredient>();
            if (ing != null && ing.Data == acceptedIngredientSO && ing.CurrentState == CookingState.Cooked)
                return true;
            return false;
        }

        // 3. Validar con la estación propietaria (MesaDeArmado)
        if (stationOwner == null) FindStationOwner();

        if (stationOwner is MesaDeArmado mesa)
        {
            return mesa.IsValidCookedPatty(item);
        }

        // Fallback genérico: debe ser un ingrediente en estado Cooked
        ICookable cookable = obj.GetComponent<ICookable>();
        return cookable != null && cookable.CurrentState == CookingState.Cooked;
    }

    /// <summary>
    /// Intenta depositar y apilar un objeto en el slot de entrada.
    /// </summary>
    public bool TryDepositItem(ICarryable item)
    {
        if (item == null || IsFull || !IsItemAccepted(item)) return false;

        item.PlaceAtPoint(transform);
        UpdateStackVisuals();

        Debug.Log($"[StationInputSlot] Objeto '{item.ItemName}' depositado en slot de entrada '{gameObject.name}'. Total apilados: {CurrentCount}/{maxCapacity}");
        return true;
    }

    /// <summary>
    /// Retira y devuelve el objeto situado en la parte superior del apilado.
    /// </summary>
    public ICarryable PopItem()
    {
        if (!HasItems) return null;

        int topIndex = transform.childCount - 1;
        Transform topChild = transform.GetChild(topIndex);
        ICarryable item = topChild.GetComponent<ICarryable>();

        if (item != null)
        {
            topChild.SetParent(null);
            UpdateStackVisuals();
            return item;
        }

        return null;
    }

    /// <summary>
    /// Inspecciona el ingrediente superior del apilado sin retirarlo.
    /// </summary>
    public ICarryable PeekItem()
    {
        if (!HasItems) return null;
        Transform topChild = transform.GetChild(transform.childCount - 1);
        return topChild.GetComponent<ICarryable>();
    }

    public void Interact()
    {
        ICarrier carrier = FindFirstObjectByType<PlayerCarrySystem>();
        if (carrier == null) return;

        ICarryable itemInHand = carrier.GetCarriedItem();

        // CASO 1: El jugador lleva un ingrediente válido -> Depositar en el apilado del slot
        if (itemInHand != null)
        {
            if (IsItemAccepted(itemInHand))
            {
                if (!IsFull)
                {
                    ICarryable itemToDeposit = carrier.TakeCarriedItem();
                    TryDepositItem(itemToDeposit);
                    
                    // Notificar a MesaDeArmado si está lista para procesar armado automático
                    if (stationOwner is MesaDeArmado mesa)
                    {
                        mesa.CheckAutoAssembly();
                    }
                }
                else
                {
                    Debug.Log($"[StationInputSlot] El slot de entrada '{gameObject.name}' está lleno ({maxCapacity}/{maxCapacity}).");
                }
            }
            else
            {
                Debug.Log($"[StationInputSlot] El objeto '{itemInHand.ItemName}' no es un ingrediente válido para este slot.");
            }
            return;
        }

        // CASO 2: Manos del jugador libres -> Retirar la carne cocinada superior del apilado
        if (HasItems && carrier.CanCarryMore())
        {
            ICarryable poppedItem = PopItem();
            if (poppedItem != null)
            {
                carrier.PickUp(poppedItem);
                Debug.Log($"[StationInputSlot] Jugador retiró '{poppedItem.ItemName}' del slot de entrada '{gameObject.name}'.");
            }
        }
    }

    /// <summary>
    /// Limpia y destruye todos los ingredientes almacenados en este slot.
    /// </summary>
    public void ResetSlot()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        Debug.Log($"[StationInputSlot] Slot de entrada '{gameObject.name}' limpiado.");
    }

    public string GetInteractPrompt()
    {
        ICarrier carrier = FindFirstObjectByType<PlayerCarrySystem>();
        ICarryable itemInHand = carrier != null ? carrier.GetCarriedItem() : null;

        if (itemInHand != null && IsItemAccepted(itemInHand))
        {
            return IsFull ? "Slot de Carne Lleno" : "Colocar Carne Cocinada en Slot";
        }

        if (HasItems && carrier != null && carrier.CanCarryMore())
        {
            return "Retirar Carne Cocinada del Slot";
        }

        return "Slot de Carne Cocinada";
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position, new Vector3(0.6f, 0.6f, 0f));

        if (stackOffset != Vector3.zero && maxCapacity > 0)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.4f);
            for (int i = 1; i < maxCapacity; i++)
            {
                Gizmos.DrawWireCube(transform.position + stackOffset * i, new Vector3(0.5f, 0.3f, 0f));
            }
        }
    }
}
