using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente de UI dedicado exclusivamente para las mejoras de la Plancha de Cocina (CookingGrill) en el panel 'MejorasPanel'.
/// Administra el nivel alcanzado y activa/desactiva automáticamente los elementos vinculados:
/// Nivel 1: Desbloquea el segundo puesto de cocción de la plancha (objeto visual en escena y habilitación en CookingGrill).
/// </summary>
public class PlanchaUpgradeItemUI : MonoBehaviour
{
    [Header("Configuración de la Mejora (ScriptableObject)")]
    [Tooltip("ScriptableObject de datos de la mejora de la Plancha (ej. Upgrade_Plancha).")]
    [SerializeField] private UpgradeDataSO upgradeData;

    [Header("Estación Plancha en Cocina")]
    [Tooltip("Estación o componente CookingGrill al que se le aplicará el desbloqueo del slot 2.")]
    [SerializeField] private CookingGrill planchaStation;

    [Header("Desbloqueos del Nivel 1 (Slot 2)")]
    [Tooltip("GameObject en la escena que representa la extensión física o slot 2 secundario de la Plancha.")]
    [SerializeField] private GameObject planchaSlot2ExtensionObject;

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
    /// Actualiza el estado visual de la tarjeta de la plancha y aplica los desbloqueos correspondientes.
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
    /// Activa o desactiva los elementos del juego y la UI según el nivel alcanzado en la Plancha.
    /// </summary>
    private void ApplyUnlocks(int currentLevel)
    {
        // Nivel 1: Activar extensión física / slot 2 de la Plancha en la cocina y habilitar lógica en CookingGrill
        bool lvl1Unlocked = (currentLevel >= 1);

        if (planchaSlot2ExtensionObject != null && planchaSlot2ExtensionObject.activeSelf != lvl1Unlocked)
        {
            planchaSlot2ExtensionObject.SetActive(lvl1Unlocked);
        }

        // Habilitar la lógica de mejoras en el script CookingGrill (Nivel 1: Slot 2, Nivel 2: Tiempo Reducido, Nivel 3: Retirado Automático)
        CookingGrill grillComp = (planchaStation != null) ? planchaStation : FindFirstObjectByType<CookingGrill>(FindObjectsInactive.Include);
        if (grillComp != null)
        {
            grillComp.SetUpgradeLevel(currentLevel);
        }
    }

    private void OnBuyButtonClicked()
    {
        if (upgradeData == null) return;

        UpgradeManager mgr = UpgradeManager.Instance ?? FindFirstObjectByType<UpgradeManager>();
        if (mgr == null)
        {
            Debug.LogWarning("[PlanchaUpgradeItemUI] No se encontró UpgradeManager en la escena.");
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
