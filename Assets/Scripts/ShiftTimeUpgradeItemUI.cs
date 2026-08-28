using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente de UI dedicado exclusivamente para la mejora de Tiempo de Turno (ShiftTimeUpgrade) en el panel 'MejorasPanel'.
/// Administra los niveles de la mejora y agrega segundos adicionales al servicio cuando es comprada:
/// 5 Niveles por defecto con valores en segundos configurables en el Inspector a través de 'timeBonusSteps'.
/// </summary>
public class ShiftTimeUpgradeItemUI : MonoBehaviour
{
    private static ShiftTimeUpgradeItemUI instance;
    public static ShiftTimeUpgradeItemUI Instance => instance;

    [Header("Configuración de la Mejora (ScriptableObject)")]
    [Tooltip("ScriptableObject de datos de la mejora de tiempo de turno (ej. Upgrade_ShiftTime).")]
    [SerializeField] private UpgradeDataSO upgradeData;

    [Header("Configuración de Tiempo Adicional por Nivel (Segundos)")]
    [Tooltip("Lista de segundos adicionales otorgados a la duración del servicio según el nivel alcanzado (Índice 0 = Nivel 1, Índice 1 = Nivel 2, etc.).")]
    [SerializeField] private List<float> timeBonusSteps = new List<float>() { 10f, 20f, 30f, 40f, 50f };

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
    /// Tiempo adicional en segundos otorgado actualmente por esta mejora.
    /// </summary>
    public static float CurrentBonusTime { get; private set; } = 0f;

    public UpgradeDataSO UpgradeData => upgradeData;
    public List<float> TimeBonusSteps => timeBonusSteps;

    private void Awake()
    {
        if (instance == null) instance = this;
        InitializeInScene();
    }

    private void Start()
    {
        int currentMoney = 0;
        IMoneyService moneyService = FindFirstObjectByType<MoneyManager>();
        if (moneyService != null) currentMoney = moneyService.CurrentMoney;
        UpdateDisplay(currentMoney);
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
    /// Actualiza el estado visual de la tarjeta de tiempo de turno y aplica el bonus correspondiente.
    /// </summary>
    public void UpdateDisplay(int currentMoney)
    {
        if (upgradeData == null) return;

        UpgradeManager mgr = UpgradeManager.Instance ?? FindFirstObjectByType<UpgradeManager>();
        int currentLevel = (mgr != null) ? mgr.GetUpgradeLevel(upgradeData.UpgradeId) : 0;
        bool isMax = (mgr != null) ? mgr.IsMaxLevel(upgradeData) : (currentLevel >= upgradeData.MaxLevel);

        // Aplicar bonus de tiempo activo
        ApplyUnlocks(currentLevel);

        if (isMax)
        {
            SetText(tmpLevelText, uiLevelText, $"Nivel: {currentLevel} (MÁXIMO)");
            SetText(tmpPriceText, uiPriceText, "COMPRADO");
            SetText(tmpDescText, uiDescText, "Tiempo de turno mejorado al máximo.");
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
    /// Aplica el tiempo adicional según la lista 'timeBonusSteps' y actualiza la propiedad estática global.
    /// </summary>
    private void ApplyUnlocks(int currentLevel)
    {
        float bonusTime = 0f;
        if (currentLevel > 0 && timeBonusSteps != null && timeBonusSteps.Count > 0)
        {
            int index = Mathf.Clamp(currentLevel - 1, 0, timeBonusSteps.Count - 1);
            bonusTime = timeBonusSteps[index];
        }

        CurrentBonusTime = bonusTime;
    }

    private void OnBuyButtonClicked()
    {
        if (upgradeData == null) return;

        UpgradeManager mgr = UpgradeManager.Instance ?? FindFirstObjectByType<UpgradeManager>();
        if (mgr == null)
        {
            Debug.LogWarning("[ShiftTimeUpgradeItemUI] No se encontró UpgradeManager en la escena.");
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
