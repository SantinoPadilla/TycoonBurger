using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct CustomerUpgradeStep
{
    [Tooltip("Máximo número de clientes simultáneos permitidos en pantalla para este nivel.")]
    public int maxConcurrentCustomers;

    [Tooltip("Intervalo entre apariciones de clientes (en segundos) para este nivel.")]
    public float spawnInterval;
}

/// <summary>
/// Componente de UI dedicado exclusivamente para la mejora de Llegada de Clientes (CustomerUpgrade) en el panel 'MejorasPanel'.
/// Administra los niveles de la mejora y aplica los valores de 'maxConcurrentCustomers' y 'spawnInterval' al CustomerSpawner:
/// Nivel 1, Nivel 2, Nivel 3... según la lista de pasos configurada en 'levelSteps'.
/// </summary>
public class CustomerUpgradeItemUI : MonoBehaviour
{
    [Header("Configuración de la Mejora (ScriptableObject)")]
    [Tooltip("ScriptableObject de datos de la mejora de clientes (ej. Upgrade_CustomerSpawner).")]
    [SerializeField] private UpgradeDataSO upgradeData;

    [Header("Referencia al CustomerSpawner")]
    [Tooltip("Componente CustomerSpawner al cual aplicar los nuevos límites y tiempos. Si está vacío, se buscará en la escena.")]
    [SerializeField] private CustomerSpawner customerSpawner;

    [Header("Configuración de Parámetros por Nivel de Mejora")]
    [Tooltip("Configuración de maxConcurrentCustomers y spawnInterval por nivel (Índice 0 = Nivel 1, Índice 1 = Nivel 2, etc.).")]
    [SerializeField] private List<CustomerUpgradeStep> levelSteps = new List<CustomerUpgradeStep>()
    {
        new CustomerUpgradeStep { maxConcurrentCustomers = 4, spawnInterval = 4.5f },
        new CustomerUpgradeStep { maxConcurrentCustomers = 5, spawnInterval = 4.0f },
        new CustomerUpgradeStep { maxConcurrentCustomers = 6, spawnInterval = 3.5f }
    };

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
    public List<CustomerUpgradeStep> LevelSteps => levelSteps;

    private void Awake()
    {
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
    /// Actualiza el estado visual de la tarjeta de mejora y aplica los límites al CustomerSpawner.
    /// </summary>
    public void UpdateDisplay(int currentMoney)
    {
        if (upgradeData == null) return;

        UpgradeManager mgr = UpgradeManager.Instance ?? FindFirstObjectByType<UpgradeManager>();
        int currentLevel = (mgr != null) ? mgr.GetUpgradeLevel(upgradeData.UpgradeId) : 0;
        bool isMax = (mgr != null) ? mgr.IsMaxLevel(upgradeData) : (currentLevel >= upgradeData.MaxLevel);

        // Aplicar valores correspondientes al CustomerSpawner
        ApplyUnlocks(currentLevel);

        if (isMax)
        {
            SetText(tmpLevelText, uiLevelText, $"Nivel: {currentLevel} (MÁXIMO)");
            SetText(tmpPriceText, uiPriceText, "COMPRADO");
            SetText(tmpDescText, uiDescText, "Llegada de clientes mejorada al máximo.");
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
    /// Aplica los límites de clientes simultáneos e intervalo de spawn a CustomerSpawner según el nivel actual.
    /// </summary>
    private void ApplyUnlocks(int currentLevel)
    {
        CustomerSpawner spawner = (customerSpawner != null) ? customerSpawner : FindFirstObjectByType<CustomerSpawner>(FindObjectsInactive.Include);
        if (spawner == null) return;

        if (currentLevel > 0 && levelSteps != null && levelSteps.Count > 0)
        {
            int index = Mathf.Clamp(currentLevel - 1, 0, levelSteps.Count - 1);
            CustomerUpgradeStep step = levelSteps[index];
            spawner.SetUpgradeCustomerLimits(step.maxConcurrentCustomers, step.spawnInterval);
        }
        else
        {
            spawner.ClearUpgradeCustomerLimits();
        }
    }

    private void OnBuyButtonClicked()
    {
        if (upgradeData == null) return;

        UpgradeManager mgr = UpgradeManager.Instance ?? FindFirstObjectByType<UpgradeManager>();
        if (mgr == null)
        {
            Debug.LogWarning("[CustomerUpgradeItemUI] No se encontró UpgradeManager en la escena.");
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
