using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente de UI dedicado exclusivamente para la mejora de Velocidad del Jugador (Player Speed) en el panel 'MejorasPanel'.
/// Administra el nivel alcanzado y aplica el incremento de velocidad al TopDownPlayerController2D:
/// Nivel 1: Incremento asignado en Inspector (level1SpeedBonus).
/// Nivel 2: Incremento asignado en Inspector (level2SpeedBonus).
/// Nivel 3: Incremento asignado en Inspector (level3SpeedBonus).
/// </summary>
public class PlayerSpeedUpgradeItemUI : MonoBehaviour
{
    [Header("Configuración de la Mejora (ScriptableObject)")]
    [Tooltip("ScriptableObject de datos de la mejora de velocidad del jugador (ej. Upgrade_PlayerSpeed).")]
    [SerializeField] private UpgradeDataSO upgradeData;

    [Header("Referencia al Jugador")]
    [Tooltip("Controlador del jugador al cual aplicar el incremento de velocidad. Si está vacío, se buscará en la escena.")]
    [SerializeField] private TopDownPlayerController2D playerController;

    [Header("Valores de Incremento de Velocidad por Nivel")]
    [Tooltip("Valor de velocidad adicional sumado a la velocidad base en Nivel 1.")]
    [SerializeField] private float level1SpeedBonus = 1.5f;

    [Tooltip("Valor de velocidad adicional sumado a la velocidad base en Nivel 2.")]
    [SerializeField] private float level2SpeedBonus = 3.0f;

    [Tooltip("Valor de velocidad adicional sumado a la velocidad base en Nivel 3.")]
    [SerializeField] private float level3SpeedBonus = 4.5f;

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
    /// Actualiza el estado visual de la tarjeta de velocidad y aplica los incrementos correspondientes.
    /// </summary>
    public void UpdateDisplay(int currentMoney)
    {
        if (upgradeData == null) return;

        UpgradeManager mgr = UpgradeManager.Instance ?? FindFirstObjectByType<UpgradeManager>();
        int currentLevel = (mgr != null) ? mgr.GetUpgradeLevel(upgradeData.UpgradeId) : 0;
        bool isMax = (mgr != null) ? mgr.IsMaxLevel(upgradeData) : (currentLevel >= upgradeData.MaxLevel);

        // Aplicar bonificación de velocidad al jugador
        ApplyUnlocks(currentLevel);

        if (isMax)
        {
            SetText(tmpLevelText, uiLevelText, $"Nivel: {currentLevel} (MÁXIMO)");
            SetText(tmpPriceText, uiPriceText, "COMPRADO");
            SetText(tmpDescText, uiDescText, "Velocidad mejorada al máximo.");
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
    /// Aplica el incremento de velocidad al componente TopDownPlayerController2D según el nivel alcanzado.
    /// </summary>
    private void ApplyUnlocks(int currentLevel)
    {
        TopDownPlayerController2D player = (playerController != null) ? playerController : FindFirstObjectByType<TopDownPlayerController2D>(FindObjectsInactive.Include);
        if (player == null) return;

        float bonus = 0f;
        switch (currentLevel)
        {
            case 1:
                bonus = level1SpeedBonus;
                break;
            case 2:
                bonus = level2SpeedBonus;
                break;
            case 3:
            default:
                if (currentLevel >= 3) bonus = level3SpeedBonus;
                break;
        }

        player.SetSpeedBonus(bonus);
    }

    private void OnBuyButtonClicked()
    {
        if (upgradeData == null) return;

        UpgradeManager mgr = UpgradeManager.Instance ?? FindFirstObjectByType<UpgradeManager>();
        if (mgr == null)
        {
            Debug.LogWarning("[PlayerSpeedUpgradeItemUI] No se encontró UpgradeManager en la escena.");
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
