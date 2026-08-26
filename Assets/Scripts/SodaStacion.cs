using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Estación independiente SodaStacion.
/// Toma 1 ingrediente de soda (sodaIngredient) e inicia un Quick Time Event (QTE).
/// El jugador debe volver a interactuar cuando el indicador esté en el centro verde.
/// En caso de éxito deposita un SodaProduct directamente en el slot de acumulación; en caso de error se pierde el ingrediente.
/// </summary>
public class SodaStacion : MonoBehaviour, IInteractable
{
    [Header("Configuración de Ingrediente y Producto")]
    [Tooltip("Ingrediente de Soda crudo/base requerido (ej. SodaIngredient).")]
    [SerializeField] private IngredientSO rawSodaIngredient;

    [Tooltip("Producto de Soda final resultante de un QTE exitoso.")]
    [SerializeField] private ProductSO sodaProductSO;

    [Tooltip("(Opcional) Prefab de Soda usado si sodaProductSO no tiene ResultPrefab asignado.")]
    [SerializeField] private GameObject sodaProductPrefab;

    [Header("Slot de Acumulación (Salida)")]
    [Tooltip("Transform/Slot asignado a donde se moverán los productos de Soda producidos para acumularse.")]
    [SerializeField] private Transform productOutputSlot;
    [Tooltip("Desplazamiento vertical entre productos apilados en el slot de salida.")]
    [SerializeField] private Vector3 outputSlotStackOffset = new Vector3(0f, 0.4f, 0f);

    [Header("Componente QTE UI")]
    [Tooltip("Referencia al componente de UI que gestiona la barra de QTE.")]
    [SerializeField] private SodaQTEUI qteUI;


    [Header("Inventario Global / Auto-Carga")]
    [Tooltip("Si es true, permite consumir el ingrediente del inventario global al interactuar con las manos vacías.")]
    [SerializeField] private bool useGlobalInventory = true;

    [Header("Eventos de Retroalimentación")]
    public UnityEvent onQTESucceeded;
    public UnityEvent onQTEFailed;

    [Header("Configuración de Mejoras")]
    [Tooltip("Proporción ampliada de la zona verde de éxito del QTE al activar la mejora (ej. 0.35 = 35% del ancho de la barra). Modificable en el Inspector.")]
    [SerializeField] private float upgradedGreenCenterRatio = 0.35f;

    private bool isQTEActive = false;
    private int currentUpgradeLevel = 0;

    public bool IsQTEActive => isQTEActive;
    public float UpgradedGreenCenterRatio { get => upgradedGreenCenterRatio; set => upgradedGreenCenterRatio = value; }
    public int CurrentUpgradeLevel => currentUpgradeLevel;

    /// <summary>
    /// Cantidad de productos de soda acumulados actualmente en el slot de salida.
    /// </summary>
    public int AccumulatedSlotCount => productOutputSlot != null ? productOutputSlot.childCount : 0;

    /// <summary>
    /// Aplica el nivel de mejora a la estación de soda.
    /// Nivel 1: Desbloqueo estación Soda (manejado en UI).
    /// Nivel 2: Agranda la zona de éxito del centro verde del QTE.
    /// </summary>
    public void SetUpgradeLevel(int level)
    {
        currentUpgradeLevel = Mathf.Max(0, level);
        if (qteUI != null)
        {
            qteUI.UpgradedGreenCenterRatio = upgradedGreenCenterRatio;
            qteUI.SetUpgradeLevel(currentUpgradeLevel);
        }
    }

    public GameObject EffectiveProductPrefab
    {
        get
        {
            if (sodaProductSO != null && sodaProductSO.ResultPrefab != null)
                return sodaProductSO.ResultPrefab;
            return sodaProductPrefab;
        }
    }

    private void Awake()
    {
        if (qteUI == null)
        {
            qteUI = GetComponentInChildren<SodaQTEUI>(true);
        }
        if (productOutputSlot != null)
        {
            StationOutputSlot slotComp = productOutputSlot.GetComponent<StationOutputSlot>();
            if (slotComp == null) slotComp = productOutputSlot.gameObject.AddComponent<StationOutputSlot>();
            slotComp.StationOwner = this;
            slotComp.StackOffset = outputSlotStackOffset;
        }
    }

    public void Interact()
    {
        ICarrier carrier = FindFirstObjectByType<PlayerCarrySystem>();

        // CASO A: El QTE está actualmente en curso (2da Interacción)
        if (isQTEActive)
        {
            ProcessQTEInteraction(carrier);
            return;
        }

        // CASO B: El QTE NO está activo (1ra Interacción: Intentar iniciar preparación)
        ICarryable itemInHand = carrier != null ? carrier.GetCarriedItem() : null;

        Ingredient ingredientInHand = itemInHand != null ? itemInHand.gameObject.GetComponent<Ingredient>() : null;

        // 1. Verificar si el jugador sostiene el ingrediente de soda correcto
        if (ingredientInHand != null)
        {
            if (rawSodaIngredient != null && ingredientInHand.Data != rawSodaIngredient)
            {
                Debug.LogWarning($"[SodaStacion] Ingrediente no válido. Esta estación requiere '{rawSodaIngredient.IngredientName}'. Se tiene: '{ingredientInHand.Data?.IngredientName}'");
                return;
            }

            // Consumir ingrediente de la mano del jugador
            carrier.TakeCarriedItem();
            Destroy(ingredientInHand.gameObject);
            StartQTEProcess();
            return;
        }

        // 2. Si no tiene en las manos y se permite el inventario global
        if (useGlobalInventory && rawSodaIngredient != null)
        {
            IKitchenInventory inventory = FindFirstObjectByType<GlobalKitchenInventory>();
            if (inventory != null && inventory.TryConsumeIngredient(rawSodaIngredient))
            {
                StartQTEProcess();
                return;
            }
            else
            {
                Debug.LogWarning($"[SodaStacion] No hay '{rawSodaIngredient.IngredientName}' en el inventario global de la cocina.");
            }
        }
        else
        {
            Debug.Log("[SodaStacion] Necesitas sostener un ingrediente de soda para activar la estación.");
        }
    }

    private void StartQTEProcess()
    {
        if (qteUI == null)
        {
            Debug.LogError("[SodaStacion] ¡No se ha asignado la referencia al componente SodaQTEUI!");
            return;
        }

        isQTEActive = true;
        qteUI.StartQTE();
        Debug.Log("[SodaStacion] ¡QTE Iniciado! Haz clic izquierdo cuando el indicador pase por la zona verde.");
    }

    private void ProcessQTEInteraction(ICarrier carrier)
    {
        if (qteUI == null) return;

        bool success = qteUI.IsIndicatorInGreenZone();
        qteUI.StopQTE();
        isQTEActive = false;

        if (success)
        {
            Debug.Log("[SodaStacion] ¡ÉXITO! Interacción perfecta en la zona verde.");
            onQTESucceeded?.Invoke();
            DeliverSodaProduct(carrier);
        }
        else
        {
            Debug.LogWarning("[SodaStacion] ¡ERROR! Interacción fuera de la zona verde. Ingrediente desperdiciado.");
            onQTEFailed?.Invoke();
        }
    }

    public bool IsSodaProduct(ICarryable item)
    {
        if (item == null) return false;
        GameObject obj = item.gameObject;

        SellableProduct sellable = obj.GetComponent<SellableProduct>();
        if (sellable != null && sodaProductSO != null && sellable.ProductData == sodaProductSO)
            return true;

        Ingredient ing = obj.GetComponent<Ingredient>();
        if (ing != null)
        {
            if (rawSodaIngredient != null && ing.Data == rawSodaIngredient && ing.CurrentState == CookingState.Cooked)
                return true;
        }

        string objName = obj.name.Replace("(Clone)", "").Trim();
        if (sodaProductSO != null)
        {
            if (objName.Equals(sodaProductSO.ProductName, System.StringComparison.OrdinalIgnoreCase) ||
                objName.Equals(sodaProductSO.name, System.StringComparison.OrdinalIgnoreCase))
                return true;
            if (sodaProductSO.ResultPrefab != null && objName.Equals(sodaProductSO.ResultPrefab.name, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        if (sodaProductPrefab != null && objName.Equals(sodaProductPrefab.name, System.StringComparison.OrdinalIgnoreCase))
            return true;

        if (sodaProductSO != null && item.ItemName.Equals(sodaProductSO.ProductName, System.StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private void DeliverSodaProduct(ICarrier carrier)
    {
        GameObject productPrefab = EffectiveProductPrefab;
        if (productPrefab == null)
        {
            Debug.LogError("[SodaStacion] ¡No se ha configurado un Prefab para SodaProduct ni en el ProductSO ni en la Estación!");
            return;
        }

        GameObject sodaObj = Instantiate(productPrefab, transform.position, Quaternion.identity);
        ICarryable carryable = sodaObj.GetComponent<ICarryable>();

        if (carryable != null)
        {
            // Prioridad 1: Enviar DIRECTAMENTE al slot de acumulación de la estación
            if (productOutputSlot != null)
            {
                carryable.PlaceAtPoint(productOutputSlot);
                Collider2D col = sodaObj.GetComponent<Collider2D>();
                if (col != null) col.enabled = true;
                Debug.Log($"[SodaStacion] Producto de Soda enviado directamente al slot de acumulación '{productOutputSlot.name}'. Total acumulados: {AccumulatedSlotCount}");
            }
            // Prioridad 2: Si no hay slot de acumulación asignado, entregar a las manos del jugador
            else if (carrier != null && carrier.CanCarryMore())
            {
                carrier.PickUp(carryable);
                Debug.Log("[SodaStacion] Producto de Soda entregado a las manos del jugador (no hay slot asignado).");
            }
            else
            {
                Debug.LogWarning("[SodaStacion] Sin slot de acumulación y manos llenas. El objeto permaneció en la estación.");
            }
        }
    }

    public string GetInteractPrompt()
    {
        if (isQTEActive)
        {
            return "¡Presionar en VERDE!";
        }
        return "Servir Soda (QTE)";
    }

    /// <summary>
    /// Limpia la estación de soda, cancelando el QTE activo y destruyendo los productos acumulados en el slot de salida.
    /// </summary>
    public void ResetStation()
    {
        if (isQTEActive)
        {
            if (qteUI != null) qteUI.StopQTE();
            isQTEActive = false;
        }

        if (productOutputSlot != null)
        {
            for (int i = productOutputSlot.childCount - 1; i >= 0; i--)
            {
                Destroy(productOutputSlot.GetChild(i).gameObject);
            }
        }

        Debug.Log($"[SodaStacion] Estación '{gameObject.name}' reseteada y limpiada.");
    }

    private void OnDrawGizmosSelected()
    {
        if (productOutputSlot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(productOutputSlot.position, new Vector3(0.4f, 0.4f, 0f));
        }
    }
}
