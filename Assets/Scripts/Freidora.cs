using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Estación Freidora refactorizada (cumple SOLID).
/// Soporta 1 o 2 puestos de fritura seleccionables en el Inspector con 'enableSlot2'.
/// Recibe un ingrediente crudo (ej. Papa) y al retirarlo en el momento correcto (Cooked),
/// entrega el producto final de Papas Fritas (Fries).
/// </summary>
public class Freidora : MonoBehaviour, IInteractable
{
    [Header("Configuración de Fritura (ScriptableObjects)")]
    [Tooltip("Ingrediente crudo que recibe la freidora (ej. Papa / Potato).")]
    [SerializeField] private IngredientSO rawIngredientSO;

    [Tooltip("Producto final obtenido al retirar en el momento correcto (ej. Papas Fritas / Fries).")]
    [SerializeField] private ProductSO cookedProductSO;

    [Header("Puesto de Fritura 1 (Principal)")]
    [SerializeField] private Transform fryingPoint1;
    [SerializeField] private CookingProgressBarUI progressBarUI1;

    [Header("Puesto de Fritura 2 (Secundario)")]
    [Tooltip("Activa o desactiva el segundo puesto de fritura en la freidora.")]
    [SerializeField] private bool enableSlot2 = false;
    [SerializeField] private Transform fryingPoint2;
    [SerializeField] private CookingProgressBarUI progressBarUI2;

    [Header("Prefabs de Respaldo (OPCIONALES)")]
    [Tooltip("(Opcional) Usado sólo si rawIngredientSO no tiene asignado un Prefab.")]
    [SerializeField] private GameObject rawIngredientPrefab;

    [Tooltip("(Opcional) Usado sólo si cookedProductSO no tiene asignado un ResultPrefab.")]
    [SerializeField] private GameObject friesProductPrefab;

    [Header("Slot de Destino (Cocinado)")]
    [Tooltip("Slot/Transform asignado en el Inspector a donde se moverán las papas al estar en punto Cooked. Si no está asignado, irán a las manos del jugador.")]
    [SerializeField] private Transform cookedProductSlot;

    [Header("UI Contador de Productos Acumulados")]
    [Tooltip("Componente TextMeshProUGUI (opcional) para mostrar cuántos productos hay acumulados en el slot.")]
    [SerializeField] private TextMeshProUGUI tmpSlotCountText;
    [Tooltip("Componente Text de Unity UI tradicional (opcional) para mostrar cuántos productos hay acumulados en el slot.")]
    [SerializeField] private Text uiSlotCountText;
    [SerializeField] private string slotCountPrefix = "Papas: ";

    [Header("Inventario Global / Auto-Carga")]
    [SerializeField] private bool useGlobalInventory = true;

    private ICookable item1;
    private ICookable item2;
    private int lastSlotCount = -1;

    public bool IsSlot1Occupied => item1 != null;
    public bool IsSlot2Occupied => item2 != null;
    public bool IsFull => IsSlot1Occupied && (!enableSlot2 || IsSlot2Occupied);
    public bool EnableSlot2 { get => enableSlot2; set => enableSlot2 = value; }

    /// <summary>
    /// Cantidad de productos acumulados actualmente en el slot de destino.
    /// </summary>
    public int AccumulatedSlotCount => cookedProductSlot != null ? cookedProductSlot.childCount : 0;

    public GameObject EffectiveRawPrefab => (rawIngredientSO != null && rawIngredientSO.Prefab != null) ? rawIngredientSO.Prefab : rawIngredientPrefab;
    public GameObject EffectiveResultPrefab => (cookedProductSO != null && cookedProductSO.ResultPrefab != null) ? cookedProductSO.ResultPrefab : friesProductPrefab;

    private void Awake()
    {
        if (fryingPoint1 == null) fryingPoint1 = transform;

        if (cookedProductSlot != null)
        {
            StationOutputSlot slotComp = cookedProductSlot.GetComponent<StationOutputSlot>();
            if (slotComp == null) slotComp = cookedProductSlot.gameObject.AddComponent<StationOutputSlot>();
            slotComp.StationOwner = this;
        }
    }

    private void Update()
    {
        // 1. Fritura Puesto 1
        if (item1 != null)
        {
            item1.Cook(Time.deltaTime);
            if (progressBarUI1 != null)
            {
                progressBarUI1.UpdateProgress(
                    item1.GetTotalProgressNormalized(),
                    item1.CookedThresholdNormalized
                );
            }
        }

        // 2. Fritura Puesto 2
        if (enableSlot2 && item2 != null)
        {
            item2.Cook(Time.deltaTime);
            if (progressBarUI2 != null)
            {
                progressBarUI2.UpdateProgress(
                    item2.GetTotalProgressNormalized(),
                    item2.CookedThresholdNormalized
                );
            }
        }

        // 3. Actualizar texto de UI con los productos acumulados en el slot
        UpdateSlotCountUI();
    }

    public void UpdateSlotCountUI()
    {
        int currentCount = AccumulatedSlotCount;
        if (currentCount == lastSlotCount) return;

        lastSlotCount = currentCount;
        bool showText = currentCount > 0;
        string textValue = showText ? $"{slotCountPrefix}{currentCount}" : "";

        if (tmpSlotCountText != null)
        {
            tmpSlotCountText.text = textValue;
            tmpSlotCountText.gameObject.SetActive(showText);
        }
        if (uiSlotCountText != null)
        {
            uiSlotCountText.text = textValue;
            uiSlotCountText.gameObject.SetActive(showText);
        }

        if (cookedProductSlot != null)
        {
            Debug.Log($"[Freidora] Productos acumulados en slot '{cookedProductSlot.name}': {currentCount}");
        }
    }

    public void Interact()
    {
        ICarrier carrier = FindFirstObjectByType<PlayerCarrySystem>();
        Vector3 playerPos = carrier != null ? carrier.transform.position : transform.position;

        ICookable closestOccupied = GetClosestOccupiedItem(playerPos, out float distOccupied);
        Transform closestEmptyPoint = GetClosestEmptyPoint(playerPos, out float distEmpty);

        ICarryable itemInHand = carrier != null ? carrier.GetCarriedItem() : null;
        ICookable cookableInHand = itemInHand != null ? itemInHand.gameObject.GetComponent<ICookable>() : null;

        // CASO 0: El jugador trae en las manos el producto cocinado de papas -> devolver al slot asignado
        if (cookedProductSlot != null && itemInHand != null && IsFreidoraProduct(itemInHand))
        {
            carrier.TakeCarriedItem();
            itemInHand.PlaceAtPoint(cookedProductSlot);

            Collider2D col = itemInHand.gameObject.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;

            UpdateSlotCountUI();
            Debug.Log($"[Freidora] Producto de papas devuelto al slot de acumulación '{cookedProductSlot.name}'. Total acumulados: {AccumulatedSlotCount}");
            return;
        }

        // CASO A: Trae ingrediente válido en las manos y hay un puesto libre -> Freír
        if (cookableInHand != null && cookableInHand.CurrentState == CookingState.Raw && closestEmptyPoint != null)
        {
            if (rawIngredientSO != null && cookableInHand.Data != null && cookableInHand.Data != rawIngredientSO)
            {
                Debug.LogWarning($"[Freidora] ¡Sólo acepta '{rawIngredientSO.IngredientName}'! Traes: '{cookableInHand.Data.IngredientName}'");
                return;
            }

            carrier.TakeCarriedItem();
            PlaceItemInFryer(cookableInHand, closestEmptyPoint);
            return;
        }

        // CASO B: Hay puesto ocupado y el jugador está más cerca de él -> RETIRAR
        if (closestOccupied != null && (closestEmptyPoint == null || distOccupied < distEmpty))
        {
            CookingState stateWhenRetrieved = closestOccupied.CurrentState;

            if (stateWhenRetrieved == CookingState.Raw)
            {
                Debug.Log("[Freidora] Las papas aún se están friendo. Espera a que estén doradas (Cooked) o quemadas (Burnt).");
                return;
            }

            // Si está cocinado (Cooked) y hay un slot asignado en el Inspector
            if (stateWhenRetrieved == CookingState.Cooked && cookedProductSlot != null)
            {
                ICookable targetItem = closestOccupied;
                RemoveItemFromFryer(targetItem);
                Destroy(targetItem.gameObject);

                GameObject resultPrefab = EffectiveResultPrefab;
                if (resultPrefab != null)
                {
                    GameObject friesObj = Instantiate(resultPrefab, transform.position, Quaternion.identity);
                    ICarryable friesCarryable = friesObj.GetComponent<ICarryable>();

                    if (friesCarryable != null)
                    {
                        friesCarryable.PlaceAtPoint(cookedProductSlot);
                        Collider2D col = friesObj.GetComponent<Collider2D>();
                        if (col != null) col.enabled = true;
                        Debug.Log($"[Freidora] Papas Fritas (Cooked) movidas al slot asignado '{cookedProductSlot.name}'.");
                    }
                }
            }
            else if (carrier != null && carrier.CanCarryMore())
            {
                ICookable targetItem = closestOccupied;

                RemoveItemFromFryer(targetItem);

                // Si fue retirado en el MOMENTO CORRECTO (Cooked) -> Entregar Papas Fritas
                if (stateWhenRetrieved == CookingState.Cooked)
                {
                    Destroy(targetItem.gameObject);

                    GameObject resultPrefab = EffectiveResultPrefab;

                    if (resultPrefab != null)
                    {
                        GameObject friesObj = Instantiate(resultPrefab, transform.position, Quaternion.identity);
                        ICarryable friesCarryable = friesObj.GetComponent<ICarryable>();

                        if (friesCarryable != null)
                        {
                            carrier.PickUp(friesCarryable);
                            Debug.Log("[Freidora] ¡Papas Fritas doradas entregadas con éxito al jugador!");
                        }
                    }
                    else
                    {
                        Debug.LogError("[Freidora] ¡No se ha asignado un Prefab para las Papas Fritas ni en ProductSO ni en el Inspector!");
                    }
                }
                else
                {
                    // Si fue retirado antes (Raw) o quemado (Burnt) -> Entregar tal cual
                    carrier.PickUp(targetItem.HoldableItem);
                    Debug.Log($"[Freidora] Jugador retiró ingrediente fritándose en estado: {stateWhenRetrieved}");
                }
            }
            else
            {
                Debug.Log("[Freidora] Las manos del jugador están llenas.");
            }
        }
        // CASO C: Puesto libre y auto-carga desde inventario global
        else if (closestEmptyPoint != null && useGlobalInventory)
        {
            IKitchenInventory inventory = FindFirstObjectByType<GlobalKitchenInventory>();

            if (inventory != null && inventory.TryConsumeIngredient(rawIngredientSO))
            {
                GameObject prefabToSpawn = EffectiveRawPrefab;

                if (prefabToSpawn != null)
                {
                    GameObject newObj = Instantiate(prefabToSpawn, closestEmptyPoint.position, Quaternion.identity);
                    ICookable newCookable = newObj.GetComponent<ICookable>();

                    if (newCookable != null)
                    {
                        PlaceItemInFryer(newCookable, closestEmptyPoint);
                    }
                    else
                    {
                        Debug.LogError("[Freidora] El Prefab no contiene un componente ICookable.");
                    }
                }
                else
                {
                    Debug.LogError("[Freidora] ¡No hay Prefab configurado para el ingrediente crudo!");
                }
            }
            else
            {
                Debug.Log("[Freidora] No hay papas en el inventario de la escena.");
            }
        }
    }

    public bool IsFreidoraProduct(ICarryable item)
    {
        if (item == null) return false;
        GameObject obj = item.gameObject;

        SellableProduct sellable = obj.GetComponent<SellableProduct>();
        if (sellable != null && cookedProductSO != null && sellable.ProductData == cookedProductSO)
            return true;

        Ingredient ing = obj.GetComponent<Ingredient>();
        if (ing != null && ing.CurrentState == CookingState.Cooked)
        {
            if (rawIngredientSO == null || ing.Data == rawIngredientSO || (cookedProductSO != null && ing.Data != null && ing.Data.name == cookedProductSO.name))
                return true;
        }

        string objName = obj.name.Replace("(Clone)", "").Trim();
        if (cookedProductSO != null)
        {
            if (objName.Equals(cookedProductSO.ProductName, System.StringComparison.OrdinalIgnoreCase) ||
                objName.Equals(cookedProductSO.name, System.StringComparison.OrdinalIgnoreCase))
                return true;
            if (cookedProductSO.ResultPrefab != null && objName.Equals(cookedProductSO.ResultPrefab.name, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        if (friesProductPrefab != null && objName.Equals(friesProductPrefab.name, System.StringComparison.OrdinalIgnoreCase))
            return true;

        if (cookedProductSO != null && item.ItemName.Equals(cookedProductSO.ProductName, System.StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private ICookable GetClosestOccupiedItem(Vector3 playerPos, out float distance)
    {
        ICookable closest = null;
        distance = Mathf.Infinity;

        if (item1 != null && fryingPoint1 != null)
        {
            float d1 = Vector3.Distance(playerPos, fryingPoint1.position);
            if (d1 < distance)
            {
                distance = d1;
                closest = item1;
            }
        }

        if (enableSlot2 && item2 != null && fryingPoint2 != null)
        {
            float d2 = Vector3.Distance(playerPos, fryingPoint2.position);
            if (d2 < distance)
            {
                distance = d2;
                closest = item2;
            }
        }

        return closest;
    }

    private Transform GetClosestEmptyPoint(Vector3 playerPos, out float distance)
    {
        Transform closest = null;
        distance = Mathf.Infinity;

        if (item1 == null && fryingPoint1 != null)
        {
            float d1 = Vector3.Distance(playerPos, fryingPoint1.position);
            if (d1 < distance)
            {
                distance = d1;
                closest = fryingPoint1;
            }
        }

        if (enableSlot2 && item2 == null && fryingPoint2 != null)
        {
            float d2 = Vector3.Distance(playerPos, fryingPoint2.position);
            if (d2 < distance)
            {
                distance = d2;
                closest = fryingPoint2;
            }
        }

        return closest;
    }

    private void PlaceItemInFryer(ICookable cookable, Transform targetPoint)
    {
        if (targetPoint == fryingPoint1)
        {
            item1 = cookable;
        }
        else if (targetPoint == fryingPoint2)
        {
            item2 = cookable;
        }

        cookable.HoldableItem.PlaceAtPoint(targetPoint);

        Collider2D col = cookable.gameObject.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Debug.Log($"[Freidora] Ingrediente colocado en {targetPoint.name}. Fritando...");
    }

    private void RemoveItemFromFryer(ICookable cookable)
    {
        if (item1 == cookable)
        {
            item1.transform.SetParent(null);
            item1 = null;
            if (progressBarUI1 != null) progressBarUI1.Hide();
        }
        else if (item2 == cookable)
        {
            item2.transform.SetParent(null);
            item2 = null;
            if (progressBarUI2 != null) progressBarUI2.Hide();
        }
    }

    public string GetInteractPrompt()
    {
        if (!IsFull) return "Freír Papas";
        return "Retirar Papas Fritas";
    }

    /// <summary>
    /// Limpia completamente la freidora, destruyendo los ingredientes friéndose y los acumulados en el slot de salida.
    /// </summary>
    public void ResetStation()
    {
        if (item1 != null)
        {
            Destroy(item1.gameObject);
            item1 = null;
        }
        if (progressBarUI1 != null) progressBarUI1.Hide();

        if (item2 != null)
        {
            Destroy(item2.gameObject);
            item2 = null;
        }
        if (progressBarUI2 != null) progressBarUI2.Hide();

        if (cookedProductSlot != null)
        {
            for (int i = cookedProductSlot.childCount - 1; i >= 0; i--)
            {
                Destroy(cookedProductSlot.GetChild(i).gameObject);
            }
        }

        UpdateSlotCountUI();
        Debug.Log($"[Freidora] Estación '{gameObject.name}' reseteada y limpiada.");
    }

    private void OnDrawGizmosSelected()
    {
        if (fryingPoint1 != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(fryingPoint1.position, new Vector3(0.4f, 0.4f, 0f));
        }

        if (enableSlot2 && fryingPoint2 != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(fryingPoint2.position, new Vector3(0.4f, 0.4f, 0f));
        }
    }
}
