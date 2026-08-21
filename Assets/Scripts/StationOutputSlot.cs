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

        // CASO A: El jugador sostiene un objeto en las manos -> Intentar DEVOLVERLO al slot
        if (carrier.HasItems)
        {
            ICarryable itemInHand = carrier.GetCarriedItem();
            if (itemInHand == null) return;

            if (IsItemAccepted(itemInHand))
            {
                carrier.TakeCarriedItem();
                itemInHand.PlaceAtPoint(transform);

                Collider2D col = itemInHand.gameObject.GetComponent<Collider2D>();
                if (col != null) col.enabled = true;

                NotifyStationUIUpdate();
                Debug.Log($"[StationOutputSlot] Objeto '{itemInHand.ItemName}' colocado de vuelta en el slot '{gameObject.name}'. Total acumulados: {transform.childCount}");
            }
            else
            {
                Debug.LogWarning($"[StationOutputSlot] El objeto '{itemInHand.ItemName}' no es compatible con el slot '{gameObject.name}'.");
            }
            return;
        }

        // CASO B: El jugador tiene las manos vacías -> RETIRAR un producto acumulado del slot
        if (transform.childCount > 0 && carrier.CanCarryMore())
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                ICarryable carryable = child.GetComponent<ICarryable>();
                if (carryable != null)
                {
                    carrier.PickUp(carryable);
                    NotifyStationUIUpdate();
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

    private void NotifyStationUIUpdate()
    {
        if (stationOwner == null) FindStationOwner();

        if (stationOwner is CookingGrill grill) grill.UpdateSlotCountUI();
        else if (stationOwner is Freidora freidora) freidora.UpdateSlotCountUI();
        else if (stationOwner is SodaStacion soda) soda.UpdateSlotCountUI();
        else if (stationOwner is MesaDeArmado mesa) mesa.UpdateSlotCountUI();
    }

    public string GetInteractPrompt()
    {
        ICarrier carrier = FindFirstObjectByType<PlayerCarrySystem>();
        if (carrier != null && carrier.HasItems) return "Colocar en Slot";
        if (transform.childCount > 0) return "Retirar Producto del Slot";
        return "Slot de Salida";
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(0.6f, 0.6f, 0f));
    }
}
