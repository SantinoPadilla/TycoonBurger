using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Estación Mesa de Armado refactorizada (cumple SOLID).
/// Interacciona mediante interfaces e integra ProductSO (Receta).
/// </summary>
public class MesaDeArmado : MonoBehaviour, IInteractable
{
    [Header("Configuración de Receta (ScriptableObject)")]
    [Tooltip("ScriptableObject de la receta a ensamblar en esta mesa.")]
    [SerializeField] private ProductSO recipeSO;

    [Header("Configuración de la Mesa")]
    [SerializeField] private Transform assemblyPoint;
    [SerializeField] private CookingProgressBarUI progressBarUI;
    [SerializeField] private GameObject completeBurgerPrefab;

    [Header("Tiempos de Ensamblado")]
    [Tooltip("Tiempo en segundos que el jugador debe MANTENER PRESIONADA la tecla E.")]
    [SerializeField] private float assemblyTime = 3f;
    [SerializeField] private float maxInteractionDistance = 2f;

    [Header("Slot de Destino (Cocinado/Ensamblado)")]
    [Tooltip("Slot/Transform asignado en el Inspector a donde se moverán los productos ensamblados para acumularse. Si no está asignado, irán a la mesa/manos del jugador.")]
    [SerializeField] private Transform completedProductSlot;

    [Header("UI Contador de Productos Acumulados")]
    [Tooltip("Componente TextMeshProUGUI (opcional) para mostrar cuántos productos hay acumulados en el slot.")]
    [SerializeField] private TextMeshProUGUI tmpSlotCountText;
    [Tooltip("Componente Text de Unity UI tradicional (opcional) para mostrar cuántos productos hay acumulados en el slot.")]
    [SerializeField] private Text uiSlotCountText;
    [SerializeField] private string slotCountPrefix = "Hamburguesas: ";

    private ICookable placedPatty;
    private bool hasBun = false;
    private GameObject completedBurgerObj;

    private float currentAssemblyTimer = 0f;
    private int lastSlotCount = -1;

    public bool HasIngredientsOnTable => placedPatty != null && hasBun;
    public bool IsCompleted => completedBurgerObj != null;

    /// <summary>
    /// Cantidad de productos acumulados actualmente en el slot de destino.
    /// </summary>
    public int AccumulatedSlotCount => completedProductSlot != null ? completedProductSlot.childCount : 0;

    public GameObject ResultPrefab => (recipeSO != null && recipeSO.ResultPrefab != null) ? recipeSO.ResultPrefab : completeBurgerPrefab;
    public IngredientSO RequiredBun => recipeSO != null ? recipeSO.RequiredBunIngredient : null;

    private void Awake()
    {
        if (assemblyPoint == null) assemblyPoint = transform;

        if (completedProductSlot != null)
        {
            StationOutputSlot slotComp = completedProductSlot.GetComponent<StationOutputSlot>();
            if (slotComp == null) slotComp = completedProductSlot.gameObject.AddComponent<StationOutputSlot>();
            slotComp.StationOwner = this;
        }
    }

    private void Update()
    {
        if (HasIngredientsOnTable && !IsCompleted)
        {
            ICarrier carrier = FindFirstObjectByType<PlayerCarrySystem>();

            bool isPlayerNearby = carrier != null &&
                Vector3.Distance(transform.position, carrier.transform.position) <= maxInteractionDistance;

            bool isHoldingInteract = IsInteractKeyHeld();

            if (isPlayerNearby && isHoldingInteract)
            {
                currentAssemblyTimer += Time.deltaTime;

                if (progressBarUI != null)
                {
                    progressBarUI.UpdateProgress(currentAssemblyTimer / assemblyTime, 0.5f);
                }

                if (currentAssemblyTimer >= assemblyTime)
                {
                    CompleteAssembly();
                }
            }
            else
            {
                if (currentAssemblyTimer > 0f)
                {
                    currentAssemblyTimer = 0f;
                    if (progressBarUI != null) progressBarUI.Hide();
                    Debug.Log("[MesaDeArmado] Se soltó el Click Izquierdo. Progreso reseteado. Ingredientes a salvo.");
                }
            }
        }

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

        if (completedProductSlot != null)
        {
            Debug.Log($"[MesaDeArmado] Productos acumulados en slot '{completedProductSlot.name}': {currentCount}");
        }
    }

    public void Interact()
    {
        ICarrier carrier = FindFirstObjectByType<PlayerCarrySystem>();

        if (carrier == null) return;

        ICarryable itemInHand = carrier.GetCarriedItem();

        // CASO 0: El jugador trae en las manos un producto ensamblado de esta mesa -> devolver al slot asignado
        if (completedProductSlot != null && itemInHand != null && IsAssemblyProduct(itemInHand))
        {
            carrier.TakeCarriedItem();
            itemInHand.PlaceAtPoint(completedProductSlot);

            Collider2D col = itemInHand.gameObject.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;

            UpdateSlotCountUI();
            Debug.Log($"[MesaDeArmado] Producto ensamblado devuelto al slot de acumulación '{completedProductSlot.name}'. Total acumulados: {AccumulatedSlotCount}");
            return;
        }

        // CASO 1: Producto final completado en la mesa -> El jugador lo recoge
        if (completedBurgerObj != null)
        {
            if (carrier.CanCarryMore())
            {
                ICarryable finalItem = completedBurgerObj.GetComponent<ICarryable>();
                completedBurgerObj = null;

                if (finalItem != null)
                {
                    carrier.PickUp(finalItem);
                    Debug.Log("[MesaDeArmado] Jugador recogió el producto ensamblado.");
                }
            }
            else
            {
                Debug.Log("[MesaDeArmado] Las manos del jugador están llenas.");
            }
            return;
        }

        // CASO 2: La mesa no tiene ingredientes cargados -> Validar e iniciar ensamblado
        if (!HasIngredientsOnTable)
        {
            if (itemInHand == null)
            {
                Debug.Log("[MesaDeArmado] Necesitas llevar un ingrediente en las manos para la receta.");
                return;
            }

            ICookable cookableInHand = itemInHand.gameObject.GetComponent<ICookable>();

            if (cookableInHand == null)
            {
                Debug.Log($"[MesaDeArmado] El objeto '{itemInHand.ItemName}' no es un ingrediente válido.");
                return;
            }

            IKitchenInventory inventory = FindFirstObjectByType<GlobalKitchenInventory>();

            // Validar requerimientos según la receta (ProductSO)
            if (recipeSO != null && recipeSO.RequiredIngredients != null && recipeSO.RequiredIngredients.Count > 0)
            {
                var primaryReq = recipeSO.RequiredIngredients[0];

                // Si el ingrediente principal requiere estar cocinado (ej. carne/papa)
                if (primaryReq.requiredState != CookingState.Raw && cookableInHand.CurrentState != primaryReq.requiredState)
                {
                    Debug.LogWarning($"[MesaDeArmado] ¡Se requiere el ingrediente en estado {primaryReq.requiredState}! Estado actual: {cookableInHand.CurrentState}");
                    return;
                }

                if (primaryReq.ingredient != null && cookableInHand.Data != null && cookableInHand.Data != primaryReq.ingredient)
                {
                    Debug.LogWarning($"[MesaDeArmado] ¡Se requiere '{primaryReq.ingredient.IngredientName}'! Traes: '{cookableInHand.Data.IngredientName}'");
                    return;
                }

                // Verificar stock global para ingredientes secundarios (ej. Pan)
                for (int i = 1; i < recipeSO.RequiredIngredients.Count; i++)
                {
                    var extraReq = recipeSO.RequiredIngredients[i];
                    if (extraReq.ingredient != null)
                    {
                        if (inventory == null || !inventory.HasIngredient(extraReq.ingredient))
                        {
                            Debug.LogWarning($"[MesaDeArmado] No hay '{extraReq.ingredient.IngredientName}' disponible en el inventario global.");
                            return;
                        }
                    }
                }

                // Consumir ingredientes secundarios del inventario global
                for (int i = 1; i < recipeSO.RequiredIngredients.Count; i++)
                {
                    var extraReq = recipeSO.RequiredIngredients[i];
                    if (extraReq.ingredient != null && inventory != null)
                    {
                        inventory.TryConsumeIngredient(extraReq.ingredient);
                    }
                }

                carrier.TakeCarriedItem();
                placedPatty = cookableInHand;
                hasBun = true;
                currentAssemblyTimer = 0f;

                placedPatty.HoldableItem.PlaceAtPoint(assemblyPoint);
                Collider2D col = placedPatty.gameObject.GetComponent<Collider2D>();
                if (col != null) col.enabled = false;

                Debug.Log($"[MesaDeArmado] Ingrediente principal colocado. ¡Mantén Click Izquierdo para ensamblar '{recipeSO.ProductName}'!");
            }
            else
            {
                // Fallback por defecto si no hay receta SO asignada
                CookingState requiredState = CookingState.Cooked;

                if (cookableInHand.CurrentState != requiredState)
                {
                    Debug.LogWarning($"[MesaDeArmado] ¡Se requiere el ingrediente en estado {requiredState}! Estado actual: {cookableInHand.CurrentState}");
                    return;
                }

                if (inventory != null && inventory.HasIngredient(RequiredBun) && inventory.TryConsumeIngredient(RequiredBun))
                {
                    carrier.TakeCarriedItem();
                    placedPatty = cookableInHand;
                    hasBun = true;
                    currentAssemblyTimer = 0f;

                    placedPatty.HoldableItem.PlaceAtPoint(assemblyPoint);
                    Collider2D col = placedPatty.gameObject.GetComponent<Collider2D>();
                    if (col != null) col.enabled = false;
                }
            }
        }
    }

    private void CompleteAssembly()
    {
        if (placedPatty != null)
        {
            Destroy(placedPatty.gameObject);
            placedPatty = null;
        }

        hasBun = false;
        currentAssemblyTimer = 0f;

        if (progressBarUI != null) progressBarUI.Hide();

        GameObject prefabToSpawn = ResultPrefab;

        if (prefabToSpawn != null)
        {
            completedBurgerObj = Instantiate(prefabToSpawn, transform.position, Quaternion.identity);
            ICarryable finalHoldable = completedBurgerObj.GetComponent<ICarryable>();

            if (finalHoldable != null)
            {
                if (completedProductSlot != null)
                {
                    finalHoldable.PlaceAtPoint(completedProductSlot);
                    Collider2D col = completedBurgerObj.GetComponent<Collider2D>();
                    if (col != null) col.enabled = true;

                    completedBurgerObj = null;
                    Debug.Log($"[MesaDeArmado] Producto ensamblado movido al slot asignado '{completedProductSlot.name}'.");
                }
                else
                {
                    finalHoldable.PlaceAtPoint(assemblyPoint);
                }
            }

            Debug.Log("[MesaDeArmado] ¡Producto ensamblado con éxito!");
        }
        else
        {
            Debug.LogError("[MesaDeArmado] ¡No se ha asignado un ResultPrefab en ProductSO ni en el Inspector!");
        }
    }

    public bool IsAssemblyProduct(ICarryable item)
    {
        if (item == null) return false;
        GameObject obj = item.gameObject;

        SellableProduct sellable = obj.GetComponent<SellableProduct>();
        if (sellable != null && recipeSO != null && sellable.ProductData == recipeSO)
            return true;

        string objName = obj.name.Replace("(Clone)", "").Trim();
        if (recipeSO != null)
        {
            if (objName.Equals(recipeSO.ProductName, System.StringComparison.OrdinalIgnoreCase) ||
                objName.Equals(recipeSO.name, System.StringComparison.OrdinalIgnoreCase))
                return true;
            if (recipeSO.ResultPrefab != null && objName.Equals(recipeSO.ResultPrefab.name, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        if (completeBurgerPrefab != null && objName.Equals(completeBurgerPrefab.name, System.StringComparison.OrdinalIgnoreCase))
            return true;

        if (recipeSO != null && item.ItemName.Equals(recipeSO.ProductName, System.StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private bool IsInteractKeyHeld()
    {
        bool isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed && !isPointerOverUI) return true;

        var gamepad = Gamepad.current;
        if (gamepad != null && gamepad.buttonNorth.isPressed) return true;

        return false;
    }

    public string GetInteractPrompt()
    {
        if (IsCompleted) return "Recoger Producto Ensamblado";
        if (HasIngredientsOnTable) return "Mantén [Click Izquierdo] para Ensamblar";
        return "Colocar Carne Cocinada + Pan";
    }

    /// <summary>
    /// Limpia completamente la mesa de armado, destruyendo ingredientes en proceso y el producto final ensamblado.
    /// </summary>
    public void ResetStation()
    {
        if (placedPatty != null)
        {
            Destroy(placedPatty.gameObject);
            placedPatty = null;
        }

        if (completedBurgerObj != null)
        {
            Destroy(completedBurgerObj);
            completedBurgerObj = null;
        }

        hasBun = false;
        currentAssemblyTimer = 0f;
        if (progressBarUI != null) progressBarUI.Hide();

        if (assemblyPoint != null)
        {
            for (int i = assemblyPoint.childCount - 1; i >= 0; i--)
            {
                Destroy(assemblyPoint.GetChild(i).gameObject);
            }
        }

        if (completedProductSlot != null)
        {
            for (int i = completedProductSlot.childCount - 1; i >= 0; i--)
            {
                Destroy(completedProductSlot.GetChild(i).gameObject);
            }
        }

        UpdateSlotCountUI();
        Debug.Log($"[MesaDeArmado] Estación '{gameObject.name}' reseteada y limpiada.");
    }

    private void OnDrawGizmosSelected()
    {
        if (assemblyPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(assemblyPoint.position, new Vector3(0.5f, 0.5f, 0f));
        }
    }
}
