using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente de UI dedicado exclusivamente para la mejora de Ganancias / Profit en el panel 'MejorasPanel'.
/// Administra el nivel alcanzado y multiplica el valor de venta de los productos mediante un porcentaje configurable:
/// Nivel 1: Porcentaje asignado en Inspector (level1ProfitPercentage, ej. +10%).
/// Nivel 2: Porcentaje asignado en Inspector (level2ProfitPercentage, ej. +25%).
/// Nivel 3: Porcentaje asignado en Inspector (level3ProfitPercentage, ej. +50%).
/// </summary>
public class ProfitUpgradeItemUI : MonoBehaviour
{
    private static ProfitUpgradeItemUI instance;
    public static ProfitUpgradeItemUI Instance => instance;

    [Header("Configuración de la Mejora (ScriptableObject)")]
    [Tooltip("ScriptableObject de datos de la mejora de ganancias (ej. Upgrade_Profit).")]
    [SerializeField] private UpgradeDataSO upgradeData;

    [Header("Porcentajes de Incremento de Ganancia por Nivel (%)")]
    [Tooltip("Porcentaje adicional de dinero obtenido en las ventas para el Nivel 1 (ej. 10 para +10%).")]
    [SerializeField] private float level1ProfitPercentage = 10f;

    [Tooltip("Porcentaje adicional de dinero obtenido en las ventas para el Nivel 2 (ej. 25 para +25%).")]
    [SerializeField] private float level2ProfitPercentage = 25f;

    [Tooltip("Porcentaje adicional de dinero obtenido en las ventas para el Nivel 3 (ej. 50 para +50%).")]
    [SerializeField] private float level3ProfitPercentage = 50f;

    [Header("Referencias Visuales UI")]
    [SerializeField] private Image iconImage;

    [Header("Textos (Soporta TMPro o Text tradicional)")]
    [SerializeField] private TextMeshProUGUI tmpNameText;
    [SerializeField] private Text uiNameText;

    [SerializeField] private TextMeshProUGUI tmpLevelText;
    [SerializeField] private Text uiLevelText;

    [SerializeField] private TextMeshProUGUI tmpDescText;
    [SerializeField] private Text uiDescText;

    [SerializeField] private TextMeshProUGUI tmpPriceText;
    [SerializeField] private Text uiPriceText;

    [Header("Botón de Compra")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI tmpButtonText;
    [SerializeField] private Text uiButtonText;

    /// <summary>
    /// Multiplicador activo global de ganancias (1.0f = sin bonus, 1.1f = +10%, 1.5f = +50%, etc.).
    /// </summary>
    public static float CurrentProfitMultiplier { get; private set; } = 1.0f;

    /// <summary>
    /// Porcentaje de bonus activo (+0%, +10%, +25%, +50%, etc.).
    /// </summary>
    public static float CurrentProfitPercentage { get; private set; } = 0f;

    public UpgradeDataSO UpgradeData => upgradeData;

    private void Awake()
    {
        if (instance == null) instance = this;
        InitializeInScene();
    }

    private void OnValidate()
    {
        if (upgradeData != null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = upgradeData.Icon;
                iconImage.enabled = (upgradeData.Icon != null);
            }
            SetText(tmpNameText, uiNameText, upgradeData.UpgradeName);
        }
    }

    /// <summary>
    /// Configura e inicializa la tarjeta visual leyendo los datos del ScriptableObject.
    /// </summary>
    public void InitializeInScene()
    {
        if (upgradeData != null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = upgradeData.Icon;
                iconImage.enabled = (upgradeData.Icon != null);
            }
            SetText(tmpNameText, uiNameText, upgradeData.UpgradeName);
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyButtonClicked);
        }
    }

    /// <summary>
    /// Actualiza el estado visual de la tarjeta de profit y aplica el multiplicador correspondiente.
    /// </summary>
    public void UpdateDisplay(int currentMoney)
    {
        if (upgradeData == null) return;

        UpgradeManager mgr = UpgradeManager.Instance ?? FindFirstObjectByType<UpgradeManager>();
        int currentLevel = (mgr != null) ? mgr.GetUpgradeLevel(upgradeData.UpgradeId) : 0;
        bool isMax = (mgr != null) ? mgr.IsMaxLevel(upgradeData) : (currentLevel >= upgradeData.MaxLevel);

        // Aplicar multiplicador global de ganancias
        ApplyUnlocks(currentLevel);

        if (isMax)
        {
            SetText(tmpLevelText, uiLevelText, $"Nivel: {currentLevel} (MÁXIMO)");
            SetText(tmpPriceText, uiPriceText, "COMPRADO");
            SetText(tmpDescText, uiDescText, "Ganancias multiplicadas al máximo.");
            SetText(tmpButtonText, uiButtonText, "Máximo");

            if (buyButton != null) buyButton.interactable = false;
        }
        else
        {
            int nextLevel = currentLevel + 1;
            UpgradeLevelConfig nextConfig = upgradeData.GetLevelConfig(nextLevel);

            SetText(tmpLevelText, uiLevelText, currentLevel > 0 ? $"Nivel actual: {currentLevel}" : "Sin mejoras");
            SetText(tmpPriceText, uiPriceText, $"${nextConfig.price}");
            SetText(tmpDescText, uiDescText, nextConfig.description);
            SetText(tmpButtonText, uiButtonText, currentLevel == 0 ? "Comprar Lvl 1" : "Mejorar");

            if (buyButton != null)
            {
                buyButton.interactable = (currentMoney >= nextConfig.price);
            }
        }
    }

    /// <summary>
    /// Aplica el porcentaje de ganancia según el nivel alcanzado y actualiza el multiplicador global.
    /// </summary>
    private void ApplyUnlocks(int currentLevel)
    {
        float percentage = 0f;
        switch (currentLevel)
        {
            case 1:
                percentage = level1ProfitPercentage;
                break;
            case 2:
                percentage = level2ProfitPercentage;
                break;
            case 3:
            default:
                if (currentLevel >= 3) percentage = level3ProfitPercentage;
                break;
        }

        CurrentProfitPercentage = percentage;
        CurrentProfitMultiplier = 1.0f + (percentage / 100.0f);
    }

    /// <summary>
    /// Calcula el precio final redondeado aplicando el multiplicador de ganancia activo.
    /// </summary>
    public static int ApplyProfitMultiplier(int basePrice)
    {
        if (basePrice <= 0) return basePrice;
        float multiplier = CurrentProfitMultiplier > 0f ? CurrentProfitMultiplier : 1.0f;
        return Mathf.RoundToInt(basePrice * multiplier);
    }

    private void OnBuyButtonClicked()
    {
        if (upgradeData == null) return;

        UpgradeManager mgr = UpgradeManager.Instance ?? FindFirstObjectByType<UpgradeManager>();
        if (mgr == null)
        {
            Debug.LogWarning("[ProfitUpgradeItemUI] No se encontró UpgradeManager en la escena.");
            return;
        }

        if (mgr.TryBuyNextLevel(upgradeData))
        {
            int currentMoney = 0;
            IMoneyService moneyService = FindFirstObjectByType<MoneyManager>();
            if (moneyService != null) currentMoney = moneyService.CurrentMoney;

            UpdateDisplay(currentMoney);

            // Refrescar tienda global si existe
            ShopUI shop = GetComponentInParent<ShopUI>() ?? FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
            if (shop != null)
            {
                shop.RefreshAllDisplays();
            }
        }
    }

    private void SetText(TextMeshProUGUI tmpText, Text uiText, string content)
    {
        if (tmpText != null) tmpText.text = content;
        if (uiText != null) uiText.text = content;
    }
}
