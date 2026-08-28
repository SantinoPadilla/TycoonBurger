using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum PlanchaUpgradeFeature
{
    SegundoSlot,
    VelocidadCoccion,
    RetiradoAutomatico
}

/// <summary>
/// Componente de UI dedicado exclusivamente para las mejoras de la Plancha de Cocina (CookingGrill) en el panel 'MejorasPanel'.
/// Administra el nivel alcanzado y activa/desactiva las mejoras según el orden configurado en el Inspector.
/// </summary>
public class PlanchaUpgradeItemUI : MonoBehaviour
{
    [Header("Configuración de la Mejora (ScriptableObject)")]
    [Tooltip("ScriptableObject de datos de la mejora de la Plancha (ej. Upgrade_Plancha).")]
    [SerializeField] private UpgradeDataSO upgradeData;

    [Header("Orden de Mejoras (Configurable en Inspector)")]
    [Tooltip("Orden en el que se desbloquean las mejoras de la plancha (Índice 0 = Nivel 1, Índice 1 = Nivel 2, etc.).")]
    [SerializeField] private System.Collections.Generic.List<PlanchaUpgradeFeature> upgradeOrder = new System.Collections.Generic.List<PlanchaUpgradeFeature>()
    {
        PlanchaUpgradeFeature.SegundoSlot,
        PlanchaUpgradeFeature.VelocidadCoccion,
        PlanchaUpgradeFeature.RetiradoAutomatico
    };

    [Header("Parámetros de Mejora")]
    [Tooltip("Multiplicador de velocidad de cocción al desbloquear la mejora de velocidad (ej. 1.5 = 50% más rápido).")]
    [SerializeField] private float cookSpeedMultiplier = 1.5f;

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
    public System.Collections.Generic.List<PlanchaUpgradeFeature> UpgradeOrder => upgradeOrder;

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
            bool isUnlockedByDay = (mgr == null) || mgr.IsLevelUnlockedByDay(upgradeData, nextLevel);
            int reqDay = nextConfig.requiredDay > 0 ? nextConfig.requiredDay : 1;

            SetText(tmpLevelText, uiLevelText, currentLevel > 0 ? $"Nivel actual: {currentLevel}" : (isUnlockedByDay ? "Sin mejoras" : $"Bloqueada (Día {reqDay})"));
            SetText(tmpPriceText, uiPriceText, $"${nextConfig.price}");
            SetText(tmpDescText, uiDescText, nextConfig.description);

            if (!isUnlockedByDay)
            {
                SetText(tmpButtonText, uiButtonText, $"Unlock Day {reqDay}");
                if (buyButton != null) buyButton.interactable = false;
            }
            else
            {
                SetText(tmpButtonText, uiButtonText, "Buy");
                if (buyButton != null)
                {
                    buyButton.interactable = (currentMoney >= nextConfig.price);
                }
            }
        }
    }

    /// <summary>
    /// Activa o desactiva los elementos del juego y la UI según las mejoras desbloqueadas en 'upgradeOrder'.
    /// </summary>
    private void ApplyUnlocks(int currentLevel)
    {
        System.Collections.Generic.HashSet<PlanchaUpgradeFeature> activeFeatures = new System.Collections.Generic.HashSet<PlanchaUpgradeFeature>();
        if (upgradeOrder != null)
        {
            int unlockedCount = Mathf.Clamp(currentLevel, 0, upgradeOrder.Count);
            for (int i = 0; i < unlockedCount; i++)
            {
                activeFeatures.Add(upgradeOrder[i]);
            }
        }

        bool slot2Unlocked = activeFeatures.Contains(PlanchaUpgradeFeature.SegundoSlot);

        if (planchaSlot2ExtensionObject != null && planchaSlot2ExtensionObject.activeSelf != slot2Unlocked)
        {
            planchaSlot2ExtensionObject.SetActive(slot2Unlocked);
        }

        CookingGrill grillComp = (planchaStation != null) ? planchaStation : FindFirstObjectByType<CookingGrill>(FindObjectsInactive.Include);
        if (grillComp != null)
        {
            grillComp.SetUpgradeFeatures(activeFeatures, cookSpeedMultiplier);
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
