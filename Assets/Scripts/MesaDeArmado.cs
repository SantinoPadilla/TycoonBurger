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

    [Header("Configuración de Mejoras")]
    [Tooltip("Tiempo de armado reducido en segundos al desbloquear el Nivel 1 de mejora (modificable en el Inspector).")]
    [SerializeField] private float upgradedAssemblyTime = 1.5f;

    [Tooltip("Si es verdadero (Nivel 2 de mejora), el ensamblado se realiza automáticamente al recibir el ingrediente carne cocida.")]
    [SerializeField] private bool autoAssembleOnPattyReceived = false;

    [Header("Slot de Entrada (Carne Cocinada Acumulable)")]
    [Tooltip("Slot/Transform asignado en el Inspector a donde se colocarán y acumularán las carnes cocinadas apiladas antes de armar.")]
    [SerializeField] private Transform cookedPattyInputSlot;
    [Tooltip("Desplazamiento vertical entre carnes apiladas en el slot de entrada (ej. (0, 0.4, 0)).")]
    [SerializeField] private Vector3 pattyStackOffset = new Vector3(0f, 0.4f, 0f);
    [Tooltip("Capacidad máxima de carnes cocinadas acumulables en el slot de entrada.")]
    [SerializeField] private int maxPattyCapacity = 5;

    [Header("Slot de Destino (Cocinado/Ensamblado)")]
    [Tooltip("Slot/Transform asignado en el Inspector a donde se moverán los productos ensamblados para acumularse. Si no está asignado, irán a la mesa/manos del jugador.")]
    [SerializeField] private Transform completedProductSlot;
    [Tooltip("Desplazamiento vertical entre productos apilados en el slot de salida.")]
    [SerializeField] private Vector3 outputSlotStackOffset = new Vector3(0f, 0.4f, 0f);

    private ICookable placedPatty;
    private bool hasBun = false;
    private GameObject completedBurgerObj;

    private float currentAssemblyTimer = 0f;
    private int currentUpgradeLevel = 0;
    private StationInputSlot inputSlotComponent;

    public bool HasIngredientsOnTable => placedPatty != null && hasBun;
    public bool IsCompleted => completedBurgerObj != null;
    public Transform CookedPattyInputSlot => cookedPattyInputSlot;
    public StationInputSlot InputSlotComponent => inputSlotComponent;
    public int AccumulatedPattyInputCount => (inputSlotComponent != null) ? inputSlotComponent.CurrentCount : (cookedPattyInputSlot != null ? cookedPattyInputSlot.childCount : 0);

    public float UpgradedAssemblyTime { get => upgradedAssemblyTime; set => upgradedAssemblyTime = value; }
    public bool AutoAssembleOnPattyReceived { get => autoAssembleOnPattyReceived; set => autoAssembleOnPattyReceived = value; }
    public int CurrentUpgradeLevel => currentUpgradeLevel;
    public float EffectiveAssemblyTime => (currentUpgradeLevel >= 1 || upgradedAssemblyTime < assemblyTime && currentUpgradeLevel > 0) ? upgradedAssemblyTime : (currentUpgradeLevel >= 1 ? upgradedAssemblyTime : assemblyTime);

    /// <summary>
    /// Cantidad de productos acumulados actualmente en el slot de destino.
    /// </summary>
    public int AccumulatedSlotCount => completedProductSlot != null ? completedProductSlot.childCount : 0;

    public GameObject ResultPrefab => (recipeSO != null && recipeSO.ResultPrefab != null) ? recipeSO.ResultPrefab : completeBurgerPrefab;
    public IngredientSO RequiredBun => recipeSO != null ? recipeSO.RequiredBunIngredient : null;

    /// <summary>
    /// Aplica el nivel de mejora a la mesa de armado.
    /// Nivel 1: Tiempo de armado reducido.
    /// Nivel 2: Tiempo de armado reducido y armado automático al recibir el ingrediente carne cocida.
    /// </summary>
    public void SetUpgradeLevel(int level)
    {
        currentUpgradeLevel = Mathf.Max(0, level);
        if (currentUpgradeLevel >= 2)
        {
            autoAssembleOnPattyReceived = true;
        }
    }

    private void Awake()
    {
        if (assemblyPoint == null) assemblyPoint = transform;

        if (cookedPattyInputSlot != null)
        {
            inputSlotComponent = cookedPattyInputSlot.GetComponent<StationInputSlot>();
            if (inputSlotComponent == null) inputSlotComponent = cookedPattyInputSlot.gameObject.AddComponent<StationInputSlot>();
            inputSlotComponent.StationOwner = this;
            inputSlotComponent.StackOffset = pattyStackOffset;
        }

        if (completedProductSlot != null)
        {
            StationOutputSlot slotComp = completedProductSlot.GetComponent<StationOutputSlot>();
            if (slotComp == null) slotComp = completedProductSlot.gameObject.AddComponent<StationOutputSlot>();
            slotComp.StationOwner = this;
            slotComp.StackOffset = outputSlotStackOffset;
        }
    }

    public bool IsValidCookedPatty(ICarryable item)
    {
        if (item == null) return false;
        ICookable cookable = item.gameObject.GetComponent<ICookable>();
        if (cookable == null) return false;

        if (recipeSO != null && recipeSO.RequiredIngredients != null && recipeSO.RequiredIngredients.Count > 0)
        {
            var primaryReq = recipeSO.RequiredIngredients[0];
            if (primaryReq.requiredState != CookingState.Raw && cookable.CurrentState != primaryReq.requiredState)
                return false;
            if (primaryReq.ingredient != null && cookable.Data != null && cookable.Data != primaryReq.ingredient)
                return false;
            return true;
        }

        return cookable.CurrentState == CookingState.Cooked;
    }

    public bool HasPattyInInputSlot()
    {
        return AccumulatedPattyInputCount > 0;
    }

    public ICookable PopPattyFromInputSlot()
    {
        if (inputSlotComponent != null && inputSlotComponent.HasItems)
        {
            ICarryable item = inputSlotComponent.PopItem();
            return item != null ? item.gameObject.GetComponent<ICookable>() : null;
        }
        else if (cookedPattyInputSlot != null && cookedPattyInputSlot.childCount > 0)
        {
            Transform topChild = cookedPattyInputSlot.GetChild(cookedPattyInputSlot.childCount - 1);
            ICookable cookable = topChild.GetComponent<ICookable>();
            topChild.SetParent(null);
            return cookable;
        }
        return null;
    }

    public void CheckAutoAssembly()
    {
        if (HasIngredientsOnTable || IsCompleted) return;

        if ((autoAssembleOnPattyReceived || currentUpgradeLevel >= 2) && HasPattyInInputSlot())
        {
            TryStartAssemblyFromInputSlot();
        }
    }

    public bool TryStartAssemblyFromInputSlot()
    {
        if (HasIngredientsOnTable || IsCompleted || !HasPattyInInputSlot()) return false;

        IKitchenInventory inventory = FindFirstObjectByType<GlobalKitchenInventory>();

        if (recipeSO != null && recipeSO.RequiredIngredients != null && recipeSO.RequiredIngredients.Count > 0)
        {
            for (int i = 1; i < recipeSO.RequiredIngredients.Count; i++)
            {
                var extraReq = recipeSO.RequiredIngredients[i];
                if (extraReq.ingredient != null)
                {
                    if (inventory == null || !inventory.HasIngredient(extraReq.ingredient))
                    {
                        Debug.LogWarning($"[MesaDeArmado] No hay '{extraReq.ingredient.IngredientName}' disponible en el inventario global.");
                        return false;
                    }
                }
            }

            ICookable pattyToAssemble = PopPattyFromInputSlot();
            if (pattyToAssemble == null) return false;

            for (int i = 1; i < recipeSO.RequiredIngredients.Count; i++)
            {
                var extraReq = recipeSO.RequiredIngredients[i];
                if (extraReq.ingredient != null && inventory != null)
                {
                    inventory.TryConsumeIngredient(extraReq.ingredient);
                }
            }

            placedPatty = pattyToAssemble;
            hasBun = true;
            currentAssemblyTimer = 0f;

            placedPatty.HoldableItem.PlaceAtPoint(assemblyPoint);
            Collider2D col = placedPatty.gameObject.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            Debug.Log($"[MesaDeArmado] Carne tomada del slot de entrada apilado. ¡Iniciando ensamblado de '{recipeSO.ProductName}'!");
            return true;
        }
        else
        {
            if (inventory != null && inventory.HasIngredient(RequiredBun))
            {
                ICookable pattyToAssemble = PopPattyFromInputSlot();
                if (pattyToAssemble == null) return false;

                inventory.TryConsumeIngredient(RequiredBun);
                placedPatty = pattyToAssemble;
                hasBun = true;
                currentAssemblyTimer = 0f;

                placedPatty.HoldableItem.PlaceAtPoint(assemblyPoint);
                Collider2D col = placedPatty.gameObject.GetComponent<Collider2D>();
                if (col != null) col.enabled = false;

                return true;
            }
        }

        return false;
    }

    private void Update()
    {
        if (!HasIngredientsOnTable && !IsCompleted)
        {
            CheckAutoAssembly();
        }

        if (HasIngredientsOnTable && !IsCompleted)
        {
            float targetAssemblyTime = (currentUpgradeLevel >= 1) ? upgradedAssemblyTime : assemblyTime;

            // Nivel 2: Armado automático al recibir la carne cocida
            if (autoAssembleOnPattyReceived || currentUpgradeLevel >= 2)
            {
                currentAssemblyTimer += Time.deltaTime;

                if (progressBarUI != null)
                {
                    progressBarUI.UpdateProgress(currentAssemblyTimer / targetAssemblyTime, 0.5f);
                }

                if (currentAssemblyTimer >= targetAssemblyTime)
                {
                    CompleteAssembly();
                }
            }
            else
            {
                // Nivel 0 o Nivel 1: Requiere que el jugador esté cerca y mantenga el botón presionado
                ICarrier carrier = FindFirstObjectByType<PlayerCarrySystem>();

                bool isPlayerNearby = carrier != null &&
                    Vector3.Distance(transform.position, carrier.transform.position) <= maxInteractionDistance;

                bool isHoldingInteract = IsInteractKeyHeld();

                if (isPlayerNearby && isHoldingInteract)
                {
                    currentAssemblyTimer += Time.deltaTime;

                    if (progressBarUI != null)
                    {
                        progressBarUI.UpdateProgress(currentAssemblyTimer / targetAssemblyTime, 0.5f);
                    }

                    if (currentAssemblyTimer >= targetAssemblyTime)
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
        }
    }

    public void Interact()
    {
        ICarrier carrier = FindFirstObjectByType<PlayerCarrySystem>();

        if (carrier == null) return;

        ICarryable itemInHand = carrier.GetCarriedItem();

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
            // 2A: Si hay carne cocinada apilada en el slot de entrada, tomarla para iniciar armado
            if (HasPattyInInputSlot())
            {
                if (TryStartAssemblyFromInputSlot())
                {
                    return;
                }
            }

            // 2B: Si no hay carnes en el slot de entrada, intentar tomar carne de las manos del jugador
            if (itemInHand == null)
            {
                Debug.Log("[MesaDeArmado] Necesitas llevar una carne cocinada en las manos o tenerla en el slot de entrada.");
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

        CheckAutoAssembly();
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
        if (HasIngredientsOnTable)
        {
            if (autoAssembleOnPattyReceived || currentUpgradeLevel >= 2)
                return "Ensamblando automáticamente...";
            return "Mantén [Click Izquierdo] para Ensamblar";
        }
        if (HasPattyInInputSlot())
        {
            return "Ensamblar Carne Cocinada + Pan";
        }
        return "Colocar Carne Cocinada + Pan";
    }

    /// <summary>
    /// Limpia completamente la mesa de armado, destruyendo ingredientes en proceso, los apilados y el producto final ensamblado.
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

        if (inputSlotComponent != null)
        {
            inputSlotComponent.ResetSlot();
        }
        else if (cookedPattyInputSlot != null)
        {
            for (int i = cookedPattyInputSlot.childCount - 1; i >= 0; i--)
            {
                Destroy(cookedPattyInputSlot.GetChild(i).gameObject);
            }
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

        Debug.Log($"[MesaDeArmado] Estación '{gameObject.name}' reseteada y limpiada.");
    }

    private void OnDrawGizmosSelected()
    {
        if (assemblyPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(assemblyPoint.position, new Vector3(0.5f, 0.5f, 0f));
        }

        if (cookedPattyInputSlot != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(cookedPattyInputSlot.position, new Vector3(0.5f, 0.5f, 0f));
        }
    }
}
