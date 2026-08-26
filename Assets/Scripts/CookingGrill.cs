using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Plancha de Cocina refactorizada (cumple SOLID).
/// Interactúa mediante interfaces (ICookable, ICarryable, ICarrier, IKitchenInventory)
/// y utiliza IngredientSO para determinar qué ingrediente cocina.
/// </summary>
public class CookingGrill : MonoBehaviour, IInteractable
{
    [Header("Configuración de Ingrediente (ScriptableObject)")]
    [Tooltip("ScriptableObject del ingrediente que cocina esta plancha.")]
    [SerializeField] private IngredientSO ingredientSO;

    [Header("Puesto de Cocción 1 (Principal)")]
    [SerializeField] private Transform cookingPoint1;
    [SerializeField] private CookingProgressBarUI progressBarUI1;

    [Header("Puesto de Cocción 2 (Secundario)")]
    [Tooltip("Activa o desactiva el segundo espacio de cocción en la plancha.")]
    [SerializeField] private bool enableSlot2 = false;
    [SerializeField] private Transform cookingPoint2;
    [SerializeField] private CookingProgressBarUI progressBarUI2;

    [Header("Inventario Global / Auto-Carga")]
    [SerializeField] private bool useGlobalInventory = true;
    [Tooltip("Prefab del ingrediente que se instanciará al tomar del inventario global (opcional si el IngredientSO tiene el prefab).")]
    [SerializeField] private GameObject burgerPrefab;

    [Header("Slot de Destino (Cocinado)")]
    [Tooltip("Slot/Transform asignado en el Inspector a donde se moverá la hamburguesa al estar cocinada (Cooked). Si no está asignado, irá a las manos del jugador.")]
    [SerializeField] private Transform cookedBurgerSlot;
    [Tooltip("Desplazamiento vertical entre productos apilados en el slot de salida.")]
    [SerializeField] private Vector3 outputSlotStackOffset = new Vector3(0f, 0.4f, 0f);

    [Header("Configuración de Mejoras")]
    [Tooltip("Multiplicador de velocidad de cocción al activar la mejora de reducción de tiempo (ej. 1.5 = 50% más rápido). Modificable en el Inspector.")]
    [SerializeField] private float cookSpeedMultiplier = 1.5f;

    [Tooltip("Si es verdadero, al estar la hamburguesa cocinada (Cooked) se moverá automáticamente al slot de ingredientes acumulados.")]
    [SerializeField] private bool autoRemoveCooked = false;

    private ICookable ingredient1;
    private ICookable ingredient2;
    private int currentUpgradeLevel = 0;

    public bool IsSlot1Occupied => ingredient1 != null;
    public bool IsSlot2Occupied => ingredient2 != null;
    public bool IsFull => IsSlot1Occupied && (!enableSlot2 || IsSlot2Occupied);
    public bool EnableSlot2 { get => enableSlot2; set => enableSlot2 = value; }
    public float CookSpeedMultiplier { get => cookSpeedMultiplier; set => cookSpeedMultiplier = value; }
    public bool AutoRemoveCooked { get => autoRemoveCooked; set => autoRemoveCooked = value; }
    public int CurrentUpgradeLevel => currentUpgradeLevel;
    public float EffectiveCookSpeedMultiplier => (currentUpgradeLevel >= 2) ? cookSpeedMultiplier : 1.0f;

    /// <summary>
    /// Cantidad de productos acumulados actualmente en el slot de destino.
    /// </summary>
    public int AccumulatedSlotCount => cookedBurgerSlot != null ? cookedBurgerSlot.childCount : 0;

    public GameObject EffectivePrefab => (ingredientSO != null && ingredientSO.Prefab != null) ? ingredientSO.Prefab : burgerPrefab;

    /// <summary>
    /// Aplica el nivel de mejora a la plancha de cocina.
    /// Nivel 1: Activa el slot 2.
    /// Nivel 2: Reduce el tiempo de cocción (cookSpeedMultiplier).
    /// Nivel 3: Activa el retirado automático al slot de productos acumulados.
    /// </summary>
    public void SetUpgradeLevel(int level)
    {
        currentUpgradeLevel = Mathf.Max(0, level);
        enableSlot2 = (currentUpgradeLevel >= 1);
        if (currentUpgradeLevel >= 3)
        {
            autoRemoveCooked = true;
        }
    }

    private void Awake()
    {
        if (cookingPoint1 == null) cookingPoint1 = transform;

        if (cookedBurgerSlot != null)
        {
            StationOutputSlot slotComp = cookedBurgerSlot.GetComponent<StationOutputSlot>();
            if (slotComp == null) slotComp = cookedBurgerSlot.gameObject.AddComponent<StationOutputSlot>();
            slotComp.StationOwner = this;
            slotComp.StackOffset = outputSlotStackOffset;
        }
    }

    private void Update()
    {
        // 1. Actualizar cocción del Puesto 1
        if (ingredient1 != null)
        {
            float speedMult = EffectiveCookSpeedMultiplier;
            ingredient1.Cook(Time.deltaTime * speedMult);
            if (progressBarUI1 != null)
            {
                progressBarUI1.UpdateProgress(
                    ingredient1.GetTotalProgressNormalized(),
                    ingredient1.CookedThresholdNormalized
                );
            }

            // Retirado automático al estar cocinada (Cooked)
            if ((autoRemoveCooked || currentUpgradeLevel >= 3) && ingredient1.CurrentState == CookingState.Cooked && cookedBurgerSlot != null)
            {
                ICookable itemToMove = ingredient1;
                RemoveIngredientFromGrill(itemToMove);
                itemToMove.HoldableItem.PlaceAtPoint(cookedBurgerSlot);

                Collider2D col = itemToMove.gameObject.GetComponent<Collider2D>();
                if (col != null) col.enabled = true;

                Debug.Log($"[CookingGrill] (Auto-Retirado) Hamburguesa cocinada movida automáticamente al slot '{cookedBurgerSlot.name}'.");
            }
        }

        // 2. Actualizar cocción del Puesto 2 (si está habilitado)
        if (enableSlot2 && ingredient2 != null)
        {
            float speedMult = EffectiveCookSpeedMultiplier;
            ingredient2.Cook(Time.deltaTime * speedMult);
            if (progressBarUI2 != null)
            {
                progressBarUI2.UpdateProgress(
                    ingredient2.GetTotalProgressNormalized(),
                    ingredient2.CookedThresholdNormalized
                );
            }

            // Retirado automático al estar cocinada (Cooked)
            if ((autoRemoveCooked || currentUpgradeLevel >= 3) && ingredient2.CurrentState == CookingState.Cooked && cookedBurgerSlot != null)
            {
                ICookable itemToMove = ingredient2;
                RemoveIngredientFromGrill(itemToMove);
                itemToMove.HoldableItem.PlaceAtPoint(cookedBurgerSlot);

                Collider2D col = itemToMove.gameObject.GetComponent<Collider2D>();
                if (col != null) col.enabled = true;

                Debug.Log($"[CookingGrill] (Auto-Retirado) Hamburguesa cocinada movida automáticamente al slot '{cookedBurgerSlot.name}'.");
            }
        }
    }

    public void Interact()
    {
        ICarrier carrier = FindFirstObjectByType<PlayerCarrySystem>();
        Vector3 playerPos = carrier != null ? carrier.transform.position : transform.position;

        ICookable closestOccupied = GetClosestOccupiedBurger(playerPos, out float distOccupied);
        Transform closestEmptyPoint = GetClosestEmptyCookingPoint(playerPos, out float distEmpty);

        ICarryable itemInHand = carrier != null ? carrier.GetCarriedItem() : null;
        ICookable cookableInHand = itemInHand != null ? itemInHand.gameObject.GetComponent<ICookable>() : null;

        // Caso A: Trae ingrediente cocinable en las manos (crudo) y hay puesto libre -> Cocinarlo
        if (cookableInHand != null && cookableInHand.CurrentState == CookingState.Raw && closestEmptyPoint != null)
        {
            carrier.TakeCarriedItem();
            PlaceBurgerInPoint(cookableInHand, closestEmptyPoint);
            return;
        }

        // Caso B: Si hay puesto ocupado y el jugador está más cerca de él -> RETIRAR
        if (closestOccupied != null && (closestEmptyPoint == null || distOccupied < distEmpty))
        {
            if (closestOccupied.CurrentState == CookingState.Raw)
            {
                Debug.Log("[CookingGrill] El ingrediente aún se está cocinando. Espera a que esté listo (Cooked) o quemado (Burnt).");
                return;
            }
            // Si la hamburguesa está en su punto de cocción (Cooked) y hay un slot asignado en el Inspector
            if (closestOccupied.CurrentState == CookingState.Cooked && cookedBurgerSlot != null)
            {
                RemoveIngredientFromGrill(closestOccupied);
                closestOccupied.HoldableItem.PlaceAtPoint(cookedBurgerSlot);

                Collider2D col = closestOccupied.gameObject.GetComponent<Collider2D>();
                if (col != null) col.enabled = true;

                Debug.Log($"[CookingGrill] Hamburguesa cocinada (Cooked) movida al slot asignado '{cookedBurgerSlot.name}' en lugar de las manos del jugador.");
            }
            else if (carrier != null && carrier.CanCarryMore())
            {
                RemoveIngredientFromGrill(closestOccupied);
                carrier.PickUp(closestOccupied.HoldableItem);
                Debug.Log($"[CookingGrill] Jugador retiró el ingrediente más cercano (estado: {closestOccupied.CurrentState})");
            }
            else
            {
                Debug.Log("[CookingGrill] Las manos del jugador están llenas.");
            }
        }
        // De lo contrario, si está más cerca del puesto libre -> COCINAR NUEVO
        else if (closestEmptyPoint != null)
        {
            if (useGlobalInventory)
            {
                IKitchenInventory inventory = FindFirstObjectByType<GlobalKitchenInventory>();

                if (inventory != null && inventory.TryConsumeIngredient(ingredientSO))
                {
                    GameObject prefabToSpawn = EffectivePrefab;
                    if (prefabToSpawn != null)
                    {
                        GameObject newObj = Instantiate(prefabToSpawn, transform.position, Quaternion.identity);
                        ICookable newCookable = newObj.GetComponent<ICookable>();

                        if (newCookable != null)
                        {
                            PlaceBurgerInPoint(newCookable, closestEmptyPoint);
                        }
                        else
                        {
                            Debug.LogError("[CookingGrill] El Prefab no contiene un componente que implemente ICookable.");
                        }
                    }
                    else
                    {
                        Debug.LogError("[CookingGrill] No se ha configurado un Prefab ni en IngredientSO ni en el Inspector.");
                    }
                }
                else
                {
                    Debug.Log("[CookingGrill] No hay stock disponible en el inventario de la escena.");
                }
            }
        }
    }

    public bool IsCookedBurgerProduct(ICarryable item)
    {
        if (item == null) return false;
        GameObject obj = item.gameObject;

        ICookable cookable = obj.GetComponent<ICookable>();
        if (cookable != null)
        {
            if (cookable.CurrentState != CookingState.Cooked) return false;

            if (ingredientSO == null) return true;
            if (cookable.Data == null) return true;
            if (cookable.Data == ingredientSO || cookable.Data.IngredientName.Equals(ingredientSO.IngredientName, System.StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        if (ingredientSO != null)
        {
            string objName = obj.name.Replace("(Clone)", "").Trim();
            if (objName.Equals(ingredientSO.IngredientName, System.StringComparison.OrdinalIgnoreCase) ||
                objName.Equals(ingredientSO.name, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private ICookable GetClosestOccupiedBurger(Vector3 playerPos, out float distance)
    {
        ICookable closest = null;
        distance = Mathf.Infinity;

        if (ingredient1 != null && cookingPoint1 != null)
        {
            float d1 = Vector3.Distance(playerPos, cookingPoint1.position);
            if (d1 < distance)
            {
                distance = d1;
                closest = ingredient1;
            }
        }

        if (enableSlot2 && ingredient2 != null && cookingPoint2 != null)
        {
            float d2 = Vector3.Distance(playerPos, cookingPoint2.position);
            if (d2 < distance)
            {
                distance = d2;
                closest = ingredient2;
            }
        }

        return closest;
    }

    private Transform GetClosestEmptyCookingPoint(Vector3 playerPos, out float distance)
    {
        Transform closest = null;
        distance = Mathf.Infinity;

        if (ingredient1 == null && cookingPoint1 != null)
        {
            float d1 = Vector3.Distance(playerPos, cookingPoint1.position);
            if (d1 < distance)
            {
                distance = d1;
                closest = cookingPoint1;
            }
        }

        if (enableSlot2 && ingredient2 == null && cookingPoint2 != null)
        {
            float d2 = Vector3.Distance(playerPos, cookingPoint2.position);
            if (d2 < distance)
            {
                distance = d2;
                closest = cookingPoint2;
            }
        }

        return closest;
    }

    private void PlaceBurgerInPoint(ICookable cookable, Transform targetPoint)
    {
        if (targetPoint == cookingPoint1)
        {
            ingredient1 = cookable;
        }
        else if (targetPoint == cookingPoint2)
        {
            ingredient2 = cookable;
        }

        cookable.HoldableItem.PlaceAtPoint(targetPoint);

        Collider2D col = cookable.gameObject.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Debug.Log($"[CookingGrill] Ingrediente colocado en el puesto {targetPoint.name}.");
    }

    private void RemoveIngredientFromGrill(ICookable cookable)
    {
        if (ingredient1 == cookable)
        {
            ingredient1.transform.SetParent(null);
            ingredient1 = null;
            if (progressBarUI1 != null) progressBarUI1.Hide();
        }
        else if (ingredient2 == cookable)
        {
            ingredient2.transform.SetParent(null);
            ingredient2 = null;
            if (progressBarUI2 != null) progressBarUI2.Hide();
        }
    }

    public string GetInteractPrompt()
    {
        if (!IsFull) return "Cocinar Ingrediente";
        return "Retirar Ingrediente";
    }

    /// <summary>
    /// Limpia completamente la plancha, destruyendo los ingredientes en cocción y los acumulados en el slot de salida.
    /// </summary>
    public void ResetStation()
    {
        if (ingredient1 != null)
        {
            Destroy(ingredient1.gameObject);
            ingredient1 = null;
        }
        if (progressBarUI1 != null) progressBarUI1.Hide();

        if (ingredient2 != null)
        {
            Destroy(ingredient2.gameObject);
            ingredient2 = null;
        }
        if (progressBarUI2 != null) progressBarUI2.Hide();

        if (cookedBurgerSlot != null)
        {
            for (int i = cookedBurgerSlot.childCount - 1; i >= 0; i--)
            {
                Destroy(cookedBurgerSlot.GetChild(i).gameObject);
            }
        }

        Debug.Log($"[CookingGrill] Estación '{gameObject.name}' reseteada y limpiada.");
    }

    private void OnDrawGizmosSelected()
    {
        if (cookingPoint1 != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(cookingPoint1.position, new Vector3(0.4f, 0.4f, 0f));
        }

        if (enableSlot2 && cookingPoint2 != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(cookingPoint2.position, new Vector3(0.4f, 0.4f, 0f));
        }
    }
}
