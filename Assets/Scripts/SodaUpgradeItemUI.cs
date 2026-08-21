using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente de UI dedicado exclusivamente para las mejoras de la Estación de Soda/Gaseosa (SodaStacion) en el panel 'MejorasPanel'.
/// Administra el nivel alcanzado y activa/desactiva automáticamente los elementos vinculados:
/// Nivel 1:
/// 1. Estación de Soda en cocina.
/// 2. Tarjeta del ingrediente de Soda en la tienda de insumos.
/// 3. Slot de Soda en la barra de inventario del HUD.
/// 4. Objeto de Soda en el resumen del turno (ShiftResume).
/// 5. Registra el ProductSO de Soda en los pedidos de CustomerSpawner.
/// </summary>
public class SodaUpgradeItemUI : MonoBehaviour
{
    [Header("Configuración de la Mejora (ScriptableObject)")]
    [Tooltip("ScriptableObject de datos de la mejora de la Soda (ej. Upgrade_Soda).")]
    [SerializeField] private UpgradeDataSO upgradeData;

    [Header("Desbloqueos del Nivel 1 (Escena, Tienda, HUD y Resumen)")]
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

        // Aplicar activación/desactivación de los elementos según el nivel
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
    /// Activa o desactiva los elementos del juego y la UI según el nivel alcanzado en la Estación de Soda.
    /// </summary>
    private void ApplyUnlocks(int currentLevel)
    {
        // Nivel >= 1 desbloquea la estación de Soda y sus componentes vinculados
        bool isUnlocked = (currentLevel >= 1);

        // 1. Estación de Soda en la cocina
        if (sodaStationObject != null && sodaStationObject.activeSelf != isUnlocked)
        {
            sodaStationObject.SetActive(isUnlocked);
        }

        // 2. Tarjeta del ingrediente Soda en la tienda de insumos
        if (sodaShopItem != null && sodaShopItem.gameObject.activeSelf != isUnlocked)
        {
            sodaShopItem.gameObject.SetActive(isUnlocked);
        }

        // 3. Slot del ingrediente Soda en la barra de inventario del HUD
        if (sodaInventorySlot != null && sodaInventorySlot.gameObject.activeSelf != isUnlocked)
        {
            sodaInventorySlot.gameObject.SetActive(isUnlocked);
        }

        // 4. Objeto de Soda vendida en la pantalla de resumen del servicio (ShiftResume)
        if (shiftResumeSodaObject != null && shiftResumeSodaObject.activeSelf != isUnlocked)
        {
            shiftResumeSodaObject.SetActive(isUnlocked);
        }

        // 5. Agregar o remover el ProductSO de Soda de los pedidos de clientes en CustomerSpawner
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

        // Lógica específica para Niveles 2 al 5 (Reservada para futuras mejoras)
        switch (currentLevel)
        {
            case 2:
                // TODO: Lógica para Soda Nivel 2
                break;
            case 3:
                // TODO: Lógica para Soda Nivel 3
                break;
            case 4:
                // TODO: Lógica para Soda Nivel 4
                break;
            case 5:
                // TODO: Lógica para Soda Nivel 5
                break;
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
