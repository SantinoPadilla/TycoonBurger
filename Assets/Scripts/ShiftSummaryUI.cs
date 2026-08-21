using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Estructura de vinculación manual de UI para un producto específico en el resumen del servicio.
/// Permite asociar un ProductSO (o su nombre) a componentes de texto TMPro o Text de Unity.
/// </summary>
[System.Serializable]
public struct ProductTextBinding
{
    [Tooltip("ScriptableObject del producto (ej. Hamburguesa, Papas, Soda).")]
    public ProductSO productSO;

    [Tooltip("Nombre del producto en caso de no usar ScriptableObject (ej. Hamburguesa, Papas Fritas, Soda).")]
    public string customProductName;

    [Header("Referencias de Texto Visual")]
    public TextMeshProUGUI tmpCountText;
    public Text uiCountText;

    [Tooltip("Prefijo opcional visual (ej. 'x' o '').")]
    public string prefix;
}

/// <summary>
/// Componente de UI para mostrar el resumen de métricas del servicio al finalizar el turno (Shift Resume).
/// Permite vincular campos de texto (TMPro o Text tradicional) manualmente desde el Inspector,
/// incluyendo un recuento independiente por cada producto vendido.
/// </summary>
public class ShiftSummaryUI : MonoBehaviour
{
    [Header("Referencias UI de Título y Turno")]
    [SerializeField] private TextMeshProUGUI tmpShiftTitleText;
    [SerializeField] private Text uiShiftTitleText;
    [SerializeField] private string titlePrefix = "Resumen del Servicio #";

    [Header("Referencias UI de Dinero Generado")]
    [SerializeField] private TextMeshProUGUI tmpMoneyEarnedText;
    [SerializeField] private Text uiMoneyEarnedText;
    [SerializeField] private string moneyPrefix = "+$";

    [Header("Referencias UI de Clientes Atendidos")]
    [SerializeField] private TextMeshProUGUI tmpSuccessfulCustomersText;
    [SerializeField] private Text uiSuccessfulCustomersText;

    [Header("Referencias UI de Clientes Perdedores / Fallidos")]
    [SerializeField] private TextMeshProUGUI tmpFailedCustomersText;
    [SerializeField] private Text uiFailedCustomersText;

    [Header("Textos Independientes por Producto (Manual)")]
    [Tooltip("Asigna aquí las casillas de texto independientes para cada producto de tu panel.")]
    [SerializeField] private List<ProductTextBinding> productBindings = new List<ProductTextBinding>();

    [Header("Desglose Global de Productos (Opcional - Texto Único)")]
    [SerializeField] private TextMeshProUGUI tmpProductBreakdownText;
    [SerializeField] private Text uiProductBreakdownText;

    private void Start()
    {
        SubscribeToShiftManager();
    }

    private void SubscribeToShiftManager()
    {
        if (RestaurantShiftManager.Instance != null)
        {
            RestaurantShiftManager.Instance.onShiftSummaryReady.AddListener(DisplaySummary);

            if (RestaurantShiftManager.Instance.IsClosed && RestaurantShiftManager.Instance.LastShiftSummary != null)
            {
                DisplaySummary(RestaurantShiftManager.Instance.LastShiftSummary);
            }
        }
        else
        {
            Invoke(nameof(SubscribeToShiftManager), 0.1f);
        }
    }

    private void OnDestroy()
    {
        if (RestaurantShiftManager.Instance != null)
        {
            RestaurantShiftManager.Instance.onShiftSummaryReady.RemoveListener(DisplaySummary);
        }
    }

    /// <summary>
    /// Recibe los datos del resumen del turno y actualiza los componentes de texto visuales.
    /// </summary>
    public void DisplaySummary(ShiftSummaryData data)
    {
        if (data == null) return;

        // 1. Título de servicio
        SetText(tmpShiftTitleText, uiShiftTitleText, $"{titlePrefix}{data.shiftNumber}");

        // 2. Dinero generado en el turno
        SetText(tmpMoneyEarnedText, uiMoneyEarnedText, $"{moneyPrefix}{data.moneyEarned}");

        // 3. Clientes atendidos correctamente
        SetText(tmpSuccessfulCustomersText, uiSuccessfulCustomersText, data.successfulCustomers.ToString());

        // 4. Clientes no atendidos / perdidos
        SetText(tmpFailedCustomersText, uiFailedCustomersText, data.failedCustomers.ToString());

        // 5. Textos independientes por cada producto
        if (productBindings != null)
        {
            foreach (var binding in productBindings)
            {
                int soldCount = 0;

                if (binding.productSO != null)
                {
                    // 1. Probar por ProductName (propiedad del SO)
                    soldCount = data.GetProductSalesCount(binding.productSO.ProductName);

                    // 2. Fallback por name (nombre del archivo asset del SO)
                    if (soldCount == 0 && !string.IsNullOrEmpty(binding.productSO.name))
                    {
                        soldCount = data.GetProductSalesCount(binding.productSO.name);
                    }
                }

                // 3. Fallback por customProductName si se especificó en el inspector
                if (soldCount == 0 && !string.IsNullOrEmpty(binding.customProductName))
                {
                    soldCount = data.GetProductSalesCount(binding.customProductName);
                }

                string pfx = binding.prefix ?? "";
                SetText(binding.tmpCountText, binding.uiCountText, $"{pfx}{soldCount}");
            }
        }

        // 6. Desglose global en texto único (opcional)
        string breakdown = data.GetFormattedProductBreakdown();
        SetText(tmpProductBreakdownText, uiProductBreakdownText, breakdown);

        Debug.Log($"[ShiftSummaryUI] Resumen del servicio #{data.shiftNumber} desplegado con textos independientes.");
    }

    private void SetText(TextMeshProUGUI tmpText, Text uiText, string content)
    {
        if (tmpText != null) tmpText.text = content;
        if (uiText != null) uiText.text = content;
    }
}
