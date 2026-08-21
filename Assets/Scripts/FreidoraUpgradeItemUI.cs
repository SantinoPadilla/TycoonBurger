using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente de UI dedicado exclusivamente para la mejora de la Freidora en el panel 'MejorasPanel'.
/// Administra el nivel alcanzado y activa/desactiva automáticamente los 4 elementos vinculados:
/// 1. Estación Freidora en cocina.
/// 2. Tarjeta Papa en tienda de insumos.
/// 3. Slot Papa en barra del HUD.
/// 4. Objeto Papas en el resumen del turno (ShiftResume).
/// </summary>
public class FreidoraUpgradeItemUI : MonoBehaviour
{
    [Header("Configuración de la Mejora (ScriptableObject)")]
    [Tooltip("ScriptableObject de datos de la mejora de la Freidora (ej. Upgrade_Freidora).")]
    [SerializeField] private UpgradeDataSO upgradeData;

    [Header("Desbloqueos del Nivel 1 (Escena, Tienda, HUD y Resumen)")]
    [Tooltip("1. Objeto o estación física de la Freidora en la cocina.")]
    [SerializeField] private GameObject freidoraStationObject;

    [Tooltip("2. Tarjeta del ingrediente Papa en el panel de insumos de la tienda (ShopItemUI).")]
    [SerializeField] private ShopItemUI potatoShopItem;

    [Tooltip("3. Slot del ingrediente Papa en la barra de inventario del HUD (KitchenInventorySlotUI).")]
    [SerializeField] private KitchenInventorySlotUI potatoInventorySlot;

    [Tooltip("4. Objeto o casilla de Papas Fritas vendidas en el panel de resumen de servicio (ShiftSummaryUI).")]
    [SerializeField] private GameObject shiftResumeFriesObject;

    [Tooltip("5. ProductSO de Papas Fritas (Fries) a incorporar al menú de pedidos de los clientes en CustomerSpawner al desbloquear.")]
    [SerializeField] private ProductSO friesProductSO;

    [Header("Desbloqueos del Nivel 2 (Extensión / Slot 2)")]
    [Tooltip("GameObject en la escena que representa la extensión física o slot 2 secundario de la Freidora.")]
    [SerializeField] private GameObject freidoraSlot2ExtensionObject;

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
    /// Actualiza el estado visual de la tarjeta de freidora y aplica los desbloqueos correspondientes.
    /// </summary>
    public void UpdateDisplay(int currentMoney)
    {
        if (upgradeData == null) return;

        UpgradeManager mgr = UpgradeManager.Instance ?? FindFirstObjectByType<UpgradeManager>();
        int currentLevel = (mgr != null) ? mgr.GetUpgradeLevel(upgradeData.UpgradeId) : 0;
        bool isMax = (mgr != null) ? mgr.IsMaxLevel(upgradeData) : (currentLevel >= upgradeData.MaxLevel);

        // Aplicar activación/desactivación de los 4 elementos según el nivel
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
    /// Activa o desactiva los elementos del juego y la UI según el nivel alcanzado en la Freidora.
    /// </summary>
    private void ApplyUnlocks(int currentLevel)
    {
        // Regla: Nivel >= 1 desbloquea la freidora y sus 4 componentes
        bool isUnlocked = (currentLevel >= 1);

        // 1. Estación Freidora en la cocina
        if (freidoraStationObject != null && freidoraStationObject.activeSelf != isUnlocked)
        {
            freidoraStationObject.SetActive(isUnlocked);
        }

        // 2. Tarjeta Papa en tienda de insumos
        if (potatoShopItem != null && potatoShopItem.gameObject.activeSelf != isUnlocked)
        {
            potatoShopItem.gameObject.SetActive(isUnlocked);
        }

        // 3. Slot Papa en la barra de inventario del HUD
        if (potatoInventorySlot != null && potatoInventorySlot.gameObject.activeSelf != isUnlocked)
        {
            potatoInventorySlot.gameObject.SetActive(isUnlocked);
        }

        // 4. Objeto de Papas Fritas vendidas en la pantalla de resumen del servicio (ShiftResume)
        if (shiftResumeFriesObject != null && shiftResumeFriesObject.activeSelf != isUnlocked)
        {
            shiftResumeFriesObject.SetActive(isUnlocked);
        }

        // 5. Agregar o remover el ProductSO de Papas Fritas de los pedidos de clientes en CustomerSpawner
        CustomerSpawner spawner = FindFirstObjectByType<CustomerSpawner>(FindObjectsInactive.Include);
        if (spawner != null && friesProductSO != null)
        {
            if (isUnlocked)
            {
                spawner.AddAvailableProduct(friesProductSO);
            }
            else
            {
                spawner.RemoveAvailableProduct(friesProductSO);
            }
        }

        // 6. Nivel 2: Activar extensión física/slot 2 de la Freidora en la cocina
        bool lvl2Unlocked = (currentLevel >= 2);

        if (freidoraSlot2ExtensionObject != null && freidoraSlot2ExtensionObject.activeSelf != lvl2Unlocked)
        {
            freidoraSlot2ExtensionObject.SetActive(lvl2Unlocked);
        }

        // Habilitar la lógica del segundo puesto en el script Freidora
        Freidora freidoraComp = (freidoraStationObject != null) ? freidoraStationObject.GetComponent<Freidora>() : FindFirstObjectByType<Freidora>(FindObjectsInactive.Include);
        if (freidoraComp != null && freidoraComp.EnableSlot2 != lvl2Unlocked)
        {
            freidoraComp.EnableSlot2 = lvl2Unlocked;
        }

        // Lógica específica para Niveles 3 al 5 (Reservada para futuras mejoras)
        switch (currentLevel)
        {
            case 3:
                // TODO: Lógica para Freidora Nivel 3
                break;
            case 4:
                // TODO: Lógica para Freidora Nivel 4
                break;
            case 5:
                // TODO: Lógica para Freidora Nivel 5
                break;
        }
    }

    private void OnBuyButtonClicked()
    {
        if (upgradeData == null) return;

        UpgradeManager mgr = UpgradeManager.Instance ?? FindFirstObjectByType<UpgradeManager>();
        if (mgr == null)
        {
            Debug.LogWarning("[FreidoraUpgradeItemUI] No se encontró UpgradeManager en la escena.");
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
