using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente de UI dedicado exclusivamente para la mejora de Capacidad de Carga del Jugador (Player Capacity) en el panel 'MejorasPanel'.
/// Administra el nivel alcanzado y aplica el valor de capacidad máxima a PlayerCarrySystem:
/// Nivel 1: Capacidad asignada en Inspector (level1Capacity, por defecto 2).
/// Nivel 2: Capacidad asignada en Inspector (level2Capacity, por defecto 3).
/// Nivel 3: Capacidad asignada en Inspector (level3Capacity, por defecto 4).
/// </summary>
public class PlayerCapacityUpgradeItemUI : MonoBehaviour
{
    [Header("Configuración de la Mejora (ScriptableObject)")]
    [Tooltip("ScriptableObject de datos de la mejora de capacidad del jugador (ej. Upgrade_PlayerCapacity).")]
    [SerializeField] private UpgradeDataSO upgradeData;

    [Header("Referencia al Sistema de Carga del Jugador")]
    [Tooltip("Componente PlayerCarrySystem al cual aplicar la nueva capacidad de carga. Si está vacío, se buscará en la escena.")]
    [SerializeField] private PlayerCarrySystem playerCarrySystem;

    [Header("Valores de Capacidad por Nivel")]
    [Tooltip("Capacidad por defecto en Nivel 0 (sin mejoras).")]
    [SerializeField] private int defaultCapacity = 1;

    [Tooltip("Cantidad de ítems máximos a llevar en Nivel 1.")]
    [SerializeField] private int level1Capacity = 2;

    [Tooltip("Cantidad de ítems máximos a llevar en Nivel 2.")]
    [SerializeField] private int level2Capacity = 3;

    [Tooltip("Cantidad de ítems máximos a llevar en Nivel 3.")]
    [SerializeField] private int level3Capacity = 4;

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

    public UpgradeDataSO UpgradeData => upgradeData;

    private void Awake()
    {
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
    /// Actualiza el estado visual de la tarjeta de capacidad y aplica la capacidad correspondiente.
    /// </summary>
    public void UpdateDisplay(int currentMoney)
    {
        if (upgradeData == null) return;

        UpgradeManager mgr = UpgradeManager.Instance ?? FindFirstObjectByType<UpgradeManager>();
        int currentLevel = (mgr != null) ? mgr.GetUpgradeLevel(upgradeData.UpgradeId) : 0;
        bool isMax = (mgr != null) ? mgr.IsMaxLevel(upgradeData) : (currentLevel >= upgradeData.MaxLevel);

        // Aplicar capacidad de carga al jugador
        ApplyUnlocks(currentLevel);

        if (isMax)
        {
            SetText(tmpLevelText, uiLevelText, $"Nivel: {currentLevel} (MÁXIMO)");
            SetText(tmpPriceText, uiPriceText, "COMPRADO");
            SetText(tmpDescText, uiDescText, "Capacidad mejorada al máximo.");
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
    /// Aplica el límite de capacidad de carga a PlayerCarrySystem según el nivel alcanzado.
    /// </summary>
    private void ApplyUnlocks(int currentLevel)
    {
        PlayerCarrySystem carrySystem = (playerCarrySystem != null) ? playerCarrySystem : FindFirstObjectByType<PlayerCarrySystem>(FindObjectsInactive.Include);
        if (carrySystem == null) return;

        int targetCapacity = defaultCapacity;
        switch (currentLevel)
        {
            case 1:
                targetCapacity = level1Capacity;
                break;
            case 2:
                targetCapacity = level2Capacity;
                break;
            case 3:
            default:
                if (currentLevel >= 3) targetCapacity = level3Capacity;
                break;
        }

        carrySystem.SetMaxCapacity(targetCapacity);
    }

    private void OnBuyButtonClicked()
    {
        if (upgradeData == null) return;

        UpgradeManager mgr = UpgradeManager.Instance ?? FindFirstObjectByType<UpgradeManager>();
        if (mgr == null)
        {
            Debug.LogWarning("[PlayerCapacityUpgradeItemUI] No se encontró UpgradeManager en la escena.");
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
