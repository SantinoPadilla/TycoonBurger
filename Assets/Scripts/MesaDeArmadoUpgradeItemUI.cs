using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum MesaDeArmadoUpgradeFeature
{
    ReduccionTiempoArmado,
    EnsambladoAutomatico
}

/// <summary>
/// Componente de UI dedicado exclusivamente para las mejoras de la Mesa de Armado (MesaDeArmado) en el panel 'MejorasPanel'.
/// Administra el nivel alcanzado y activa las capacidades según el orden configurado en el Inspector.
/// </summary>
public class MesaDeArmadoUpgradeItemUI : MonoBehaviour
{
    [Header("Configuración de la Mejora (ScriptableObject)")]
    [Tooltip("ScriptableObject de datos de la mejora de la Mesa de Armado (ej. Upgrade_MesaArmado).")]
    [SerializeField] private UpgradeDataSO upgradeData;

    [Header("Orden de Mejoras (Configurable en Inspector)")]
    [Tooltip("Orden en el que se desbloquean las mejoras de la mesa de armado (Índice 0 = Nivel 1, Índice 1 = Nivel 2, etc.).")]
    [SerializeField] private System.Collections.Generic.List<MesaDeArmadoUpgradeFeature> upgradeOrder = new System.Collections.Generic.List<MesaDeArmadoUpgradeFeature>()
    {
        MesaDeArmadoUpgradeFeature.ReduccionTiempoArmado,
        MesaDeArmadoUpgradeFeature.EnsambladoAutomatico
    };

    [Header("Parámetros de Mejora")]
    [Tooltip("Tiempo de armado reducido en segundos al desbloquear la mejora de velocidad (ej. 1.5 s).")]
    [SerializeField] private float upgradedAssemblyTime = 1.5f;

    [Header("Estación Mesa de Armado en Cocina")]
    [Tooltip("Estación o componente MesaDeArmado al que se le aplicarán los niveles de mejora.")]
    [SerializeField] private MesaDeArmado mesaStation;

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
    public System.Collections.Generic.List<MesaDeArmadoUpgradeFeature> UpgradeOrder => upgradeOrder;

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
    /// Actualiza el estado visual de la tarjeta de la mesa de armado y aplica los desbloqueos correspondientes.
    /// </summary>
    public void UpdateDisplay(int currentMoney)
    {
        if (upgradeData == null) return;

        UpgradeManager mgr = UpgradeManager.Instance ?? FindFirstObjectByType<UpgradeManager>();
        int currentLevel = (mgr != null) ? mgr.GetUpgradeLevel(upgradeData.UpgradeId) : 0;
        bool isMax = (mgr != null) ? mgr.IsMaxLevel(upgradeData) : (currentLevel >= upgradeData.MaxLevel);

        // Aplicar nivel a la estación MesaDeArmado
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
    /// Activa o desactiva la lógica de mejoras en el script MesaDeArmado según las mejoras activas en 'upgradeOrder'.
    /// </summary>
    private void ApplyUnlocks(int currentLevel)
    {
        System.Collections.Generic.HashSet<MesaDeArmadoUpgradeFeature> activeFeatures = new System.Collections.Generic.HashSet<MesaDeArmadoUpgradeFeature>();
        if (upgradeOrder != null)
        {
            int unlockedCount = Mathf.Clamp(currentLevel, 0, upgradeOrder.Count);
            for (int i = 0; i < unlockedCount; i++)
            {
                activeFeatures.Add(upgradeOrder[i]);
            }
        }

        MesaDeArmado station = (mesaStation != null) ? mesaStation : FindFirstObjectByType<MesaDeArmado>(FindObjectsInactive.Include);
        if (station != null)
        {
            station.SetUpgradeFeatures(activeFeatures, upgradedAssemblyTime);
        }
    }

    private void OnBuyButtonClicked()
    {
        if (upgradeData == null) return;

        UpgradeManager mgr = UpgradeManager.Instance ?? FindFirstObjectByType<UpgradeManager>();
        if (mgr == null)
        {
            Debug.LogWarning("[MesaDeArmadoUpgradeItemUI] No se encontró UpgradeManager en la escena.");
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
