using UnityEngine;

/// <summary>
/// Componente interactuable asignado al GameObject de un slot de acumulación de una estación (CookingGrill, Freidora, SodaStacion).
/// Posee un Collider2D Trigger para permitir que el jugador interactúe directamente con la zona del slot,
/// retirando o devolviendo productos sin interferir con el collider principal de la máquina.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StationOutputSlot : MonoBehaviour, IInteractable
{
    [Header("Referencias de la Estación Padre (Opcional)")]
    [Tooltip("Estación principal a la que pertenece este slot. Si no se asigna, se buscará automáticamente en los padres.")]
    [SerializeField] private MonoBehaviour stationOwner;

    [Header("Configuración de Apilado Visual")]
    [Tooltip("Desplazamiento vertical relativo por cada producto acumulado en este slot de salida (ej. (0, 0.4, 0)).")]
    [SerializeField] private Vector3 stackOffset = new Vector3(0f, 0.4f, 0f);

    [Header("ScriptableObjects Aceptados (Opcional para filtrado directo)")]
    [SerializeField] private IngredientSO acceptedIngredientSO;
    [SerializeField] private ProductSO acceptedProductSO;
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

        UpdateStackVisuals();
    }

    private void OnTransformChildrenChanged()
    {
        UpdateStackVisuals();
    }

    /// <summary>
    /// Recalcula y aplica las posiciones locales de todos los productos acumulados
    /// apilándolos verticalmente uno arriba del otro.
    /// </summary>
    public void UpdateStackVisuals()
    {
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            child.localPosition = stackOffset * i + new Vector3(0f, 0f, -0.01f * i);
            child.localRotation = Quaternion.identity;

            Collider2D col = child.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
        }
    }

    public void FindStationOwner()
    {
        CookingGrill grill = GetComponentInParent<CookingGrill>();
        if (grill != null) { stationOwner = grill; return; }

        Freidora freidora = GetComponentInParent<Freidora>();
        if (freidora != null) { stationOwner = freidora; return; }

        SodaStacion soda = GetComponentInParent<SodaStacion>();
        if (soda != null) { stationOwner = soda; return; }

        MesaDeArmado mesa = GetComponentInParent<MesaDeArmado>();
        if (mesa != null) { stationOwner = mesa; return; }
    }

    public void Interact()
    {
        ICarrier carrier = FindFirstObjectByType<PlayerCarrySystem>();
        if (carrier == null) return;

        // RETIRAR un producto del slot (una vez retirado no se permite devolver objetos al slot de salida)
        if (transform.childCount > 0 && carrier.CanCarryMore())
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                ICarryable carryable = child.GetComponent<ICarryable>();
                if (carryable != null)
                {
                    carrier.PickUp(carryable);
                    Debug.Log($"[StationOutputSlot] Jugador retiró '{carryable.ItemName}' del slot '{gameObject.name}'.");
                    return;
                }
            }
        }
    }

    public bool IsItemAccepted(ICarryable item)
    {
        if (item == null) return false;
        GameObject obj = item.gameObject;

        // 1. Validar por Prefab específico asignado en el Inspector de este Slot
        if (acceptedPrefab != null)
        {
            string objName = obj.name.Replace("(Clone)", "").Trim();
            if (!objName.Equals(acceptedPrefab.name, System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Si es un ingrediente cocinable, exigir que esté en estado Cooked
            ICookable cookable = obj.GetComponent<ICookable>();
            if (cookable != null && cookable.CurrentState != CookingState.Cooked)
            {
                return false;
            }

            return true;
        }

        // 2. Validar por ScriptableObject de ingrediente asignado en Inspector
        if (acceptedIngredientSO != null)
        {
            Ingredient ing = obj.GetComponent<Ingredient>();
            if (ing != null && ing.Data == acceptedIngredientSO && ing.CurrentState == CookingState.Cooked)
                return true;
            return false;
        }

        // 3. Validar por ScriptableObject de producto asignado en Inspector
        if (acceptedProductSO != null)
        {
            SellableProduct sellable = obj.GetComponent<SellableProduct>();
            if (sellable != null && sellable.ProductData == acceptedProductSO)
                return true;
            return false;
        }

        // 4. Validar automáticamente según el tipo de estación propietaria
        if (stationOwner == null) FindStationOwner();

        if (stationOwner is CookingGrill grill)
        {
            return grill.IsCookedBurgerProduct(item);
        }
        else if (stationOwner is Freidora freidora)
        {
            return freidora.IsFreidoraProduct(item);
        }
        else if (stationOwner is SodaStacion soda)
        {
            return soda.IsSodaProduct(item);
        }
        else if (stationOwner is MesaDeArmado mesa)
        {
            return mesa.IsAssemblyProduct(item);
        }

        return false;
    }

    public string GetInteractPrompt()
    {
        ICarrier carrier = FindFirstObjectByType<PlayerCarrySystem>();
        if (transform.childCount > 0 && carrier != null && carrier.CanCarryMore())
        {
            return "Retirar Producto del Slot";
        }
        return "Slot de Salida";
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(0.6f, 0.6f, 0f));

        if (stackOffset != Vector3.zero)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
            for (int i = 1; i < 5; i++)
            {
                Gizmos.DrawWireCube(transform.position + stackOffset * i, new Vector3(0.5f, 0.3f, 0f));
            }
        }
    }
}
