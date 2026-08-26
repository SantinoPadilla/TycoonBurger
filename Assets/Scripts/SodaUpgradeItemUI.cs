using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum SodaUpgradeFeature
{
    DesbloquearEstacionYSoda,
    AmpliarZonaVerdeQTE
}

/// <summary>
/// Componente de UI dedicado exclusivamente para las mejoras de la Estación de Soda/Gaseosa (SodaStacion) en el panel 'MejorasPanel'.
/// Administra el nivel alcanzado y activa/desactiva las mejoras según el orden configurado en el Inspector.
/// </summary>
public class SodaUpgradeItemUI : MonoBehaviour
{
    [Header("Configuración de la Mejora (ScriptableObject)")]
    [Tooltip("ScriptableObject de datos de la mejora de la Soda (ej. Upgrade_Soda).")]
    [SerializeField] private UpgradeDataSO upgradeData;

    [Header("Orden de Mejoras (Configurable en Inspector)")]
    [Tooltip("Orden en el que se desbloquean las mejoras de la estación de soda (Índice 0 = Nivel 1, Índice 1 = Nivel 2, etc.).")]
    [SerializeField] private System.Collections.Generic.List<SodaUpgradeFeature> upgradeOrder = new System.Collections.Generic.List<SodaUpgradeFeature>()
    {
        SodaUpgradeFeature.DesbloquearEstacionYSoda,
        SodaUpgradeFeature.AmpliarZonaVerdeQTE
    };

    [Header("Parámetros de Mejora")]
    [Tooltip("Proporción ampliada de la zona verde de éxito del QTE al activar la mejora (ej. 0.35 = 35% del ancho de la barra).")]
    [Range(0.05f, 0.8f)]
    [SerializeField] private float upgradedGreenCenterRatio = 0.35f;

    [Header("Desbloqueos de la Estación e Ingredientes")]
    [Tooltip("1. Objeto o estación física de la SodaStacion en la cocina.")]
    [SerializeField] private GameObject sodaStationObject;

    [Tooltip("2. Tarjeta del ingrediente Soda en el panel de insumos de la tienda (ShopItemUI).")]
    [SerializeField] private ShopItemUI sodaShopItem;

    [Tooltip("3. Slot del ingrediente Soda en la barra de inventario del HUD (KitchenInventorySlotUI).")]
    [SerializeField] private KitchenInventorySlotUI sodaInventorySlot;

    [Tooltip("4. Objeto o casilla de Soda vendida en el panel de resumen de servicio (ShiftSummaryUI).")]
    [SerializeField] private GameObject shiftResumeSodaObject;

    [Tooltip("5. ProductSO de Soda a incorporar al menú de pedidos de los clientes en CustomerSpawner al desbloquear.")]
    [SerializeField] private ProductSO sodaProductSO;

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
    public System.Collections.Generic.List<SodaUpgradeFeature> UpgradeOrder => upgradeOrder;

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
    /// Actualiza el estado visual de la tarjeta de Soda y aplica los desbloqueos correspondientes.
    /// </summary>
    public void UpdateDisplay(int currentMoney)
    {
        if (upgradeData == null) return;

        UpgradeManager mgr = UpgradeManager.Instance ?? FindFirstObjectByType<UpgradeManager>();
        int currentLevel = (mgr != null) ? mgr.GetUpgradeLevel(upgradeData.UpgradeId) : 0;
        bool isMax = (mgr != null) ? mgr.IsMaxLevel(upgradeData) : (currentLevel >= upgradeData.MaxLevel);

        // Aplicar activación/desactivación de los elementos según el orden en Inspector
        ApplyUnlocks(currentLevel);

        if (isMax)
        {
            SetText(tmpLevelText, uiLevelText, $"Nivel: {currentLevel} (MÁXIMO)");
            SetText(tmpPriceText, uiPriceText, "COMPRADO");
            SetText(tmpDescText, uiDescText, "Mejora completada al máximo.");
            SetText(tmpButtonText, uiButtonText, "Máximo");

            if (buyButton != null) buyButton.interactable = false;
        }
        else
        {
            int nextLevel = currentLevel + 1;
            UpgradeLevelConfig nextConfig = upgradeData.GetLevelConfig(nextLevel);

            SetText(tmpLevelText, uiLevelText, currentLevel > 0 ? $"Nivel actual: {currentLevel}" : "Bloqueada");
            SetText(tmpPriceText, uiPriceText, $"${nextConfig.price}");
            SetText(tmpDescText, uiDescText, nextConfig.description);
            SetText(tmpButtonText, uiButtonText, currentLevel == 0 ? "Desbloquear" : "Mejorar");

            if (buyButton != null)
            {
                buyButton.interactable = (currentMoney >= nextConfig.price);
            }
        }
    }

    /// <summary>
    /// Activa o desactiva los elementos del juego y la UI según las mejoras desbloqueadas en 'upgradeOrder'.
    /// </summary>
    private void ApplyUnlocks(int currentLevel)
    {
        System.Collections.Generic.HashSet<SodaUpgradeFeature> activeFeatures = new System.Collections.Generic.HashSet<SodaUpgradeFeature>();
        if (upgradeOrder != null)
        {
            int unlockedCount = Mathf.Clamp(currentLevel, 0, upgradeOrder.Count);
            for (int i = 0; i < unlockedCount; i++)
            {
                activeFeatures.Add(upgradeOrder[i]);
            }
        }

        bool isUnlocked = activeFeatures.Contains(SodaUpgradeFeature.DesbloquearEstacionYSoda);

        if (sodaStationObject != null && sodaStationObject.activeSelf != isUnlocked)
        {
            sodaStationObject.SetActive(isUnlocked);
        }

        if (sodaShopItem != null && sodaShopItem.gameObject.activeSelf != isUnlocked)
        {
            sodaShopItem.gameObject.SetActive(isUnlocked);
        }

        if (sodaInventorySlot != null && sodaInventorySlot.gameObject.activeSelf != isUnlocked)
        {
            sodaInventorySlot.gameObject.SetActive(isUnlocked);
        }

        if (shiftResumeSodaObject != null && shiftResumeSodaObject.activeSelf != isUnlocked)
        {
            shiftResumeSodaObject.SetActive(isUnlocked);
        }

        CustomerSpawner spawner = FindFirstObjectByType<CustomerSpawner>(FindObjectsInactive.Include);
        if (spawner != null && sodaProductSO != null)
        {
            if (isUnlocked)
            {
                spawner.AddAvailableProduct(sodaProductSO);
            }
            else
            {
                spawner.RemoveAvailableProduct(sodaProductSO);
            }
        }

        SodaStacion station = (sodaStationObject != null) ? sodaStationObject.GetComponent<SodaStacion>() : FindFirstObjectByType<SodaStacion>(FindObjectsInactive.Include);
        if (station != null)
        {
            station.SetUpgradeFeatures(activeFeatures, upgradedGreenCenterRatio);
        }
    }

    private void OnBuyButtonClicked()
    {
        if (upgradeData == null) return;

        UpgradeManager mgr = UpgradeManager.Instance ?? FindFirstObjectByType<UpgradeManager>();
        if (mgr == null)
        {
            Debug.LogWarning("[SodaUpgradeItemUI] No se encontró UpgradeManager en la escena.");
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
