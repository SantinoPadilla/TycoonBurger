using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente para cada casilla o tarjeta de ingrediente pre-colocada en el panel de la Tienda.
/// Permite configurar el ingrediente, precio y estado directamente desde el Inspector de Unity.
/// </summary>
public class ShopItemUI : MonoBehaviour
{
    [Header("Configuración Manual del Ingrediente en Escena")]
    [Tooltip("ScriptableObject del ingrediente asignado a esta casilla de la tienda.")]
    [SerializeField] private IngredientSO ingredientSO;

    [Tooltip("Precio personalizado (Si es <= 0, se usará el BuyPrice del IngredientSO).")]
    [SerializeField] private int customPrice = 0;

    [Tooltip("Permite activar o desactivar esta casilla en la tienda directamente desde el Inspector.")]
    [SerializeField] private bool itemEnabled = true;

    [Header("Referencias UI Visuales")]
    [SerializeField] private Image iconImage;

    [Header("Textos (Soporta TMPro o Text genérico)")]
    [SerializeField] private TextMeshProUGUI tmpNameText;
    [SerializeField] private Text uiNameText;

    [SerializeField] private TextMeshProUGUI tmpStockText;
    [SerializeField] private Text uiStockText;

    [SerializeField] private TextMeshProUGUI tmpPriceText;
    [SerializeField] private Text uiPriceText;

    [Header("Botón de Compra")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI tmpButtonText;
    [SerializeField] private Text uiButtonText;

    private ShopUI shopUI;

    public IngredientSO CurrentIngredient => ingredientSO;
    public int EffectivePrice => (customPrice > 0) ? customPrice : (ingredientSO != null ? ingredientSO.BuyPrice : 5);
    public bool IsItemEnabled => itemEnabled;

    private void Awake()
    {
        InitializeInScene();
    }

    private void OnValidate()
    {
        // Actualizar visualización estática en el Editor si se modifica el ScriptableObject
        if (ingredientSO != null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = ingredientSO.Icon;
                iconImage.enabled = (ingredientSO.Icon != null);
            }
            SetText(tmpNameText, uiNameText, ingredientSO.IngredientName);
        }
    }

    /// <summary>
    /// Configura e inicializa la casilla de ingrediente leyendo sus parámetros del Inspector o parámetros pasados.
    /// </summary>
    public void InitializeInScene(ShopUI parentShopUI = null)
    {
        if (parentShopUI != null)
        {
            shopUI = parentShopUI;
        }
        else if (shopUI == null)
        {
            shopUI = GetComponentInParent<ShopUI>();
            if (shopUI == null)
            {
                shopUI = FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
            }
        }

        // Configurar Icono
        if (iconImage != null && ingredientSO != null)
        {
            iconImage.sprite = ingredientSO.Icon;
            iconImage.enabled = (ingredientSO.Icon != null);
        }

        // Configurar Nombre
        string nameStr = ingredientSO != null ? ingredientSO.IngredientName : "Ingrediente";
        SetText(tmpNameText, uiNameText, nameStr);

        // Configurar Botón
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyButtonClicked);
        }

        SetText(tmpButtonText, uiButtonText, "Comprar");
    }

    /// <summary>
    /// Asignación opcional por código (mantiene compatibilidad).
    /// </summary>
    public void Setup(IngredientSO ingredient, int price, ShopUI parentShopUI)
    {
        ingredientSO = ingredient;
        customPrice = price;
        InitializeInScene(parentShopUI);
    }

    /// <summary>
    /// Actualiza la información dinámica (Stock poseído, Dinero disponible y estado del botón).
    /// </summary>
    public void UpdateDisplay(int currentMoney, int ownedStock)
    {
        int finalPrice = EffectivePrice;
        SetText(tmpStockText, uiStockText, $"Tienes: {ownedStock}");
        SetText(tmpPriceText, uiPriceText, $"${finalPrice}");

        if (buyButton != null)
        {
            buyButton.interactable = (currentMoney >= finalPrice);
        }
    }

    private void OnBuyButtonClicked()
    {
        if (shopUI == null)
        {
            shopUI = GetComponentInParent<ShopUI>() ?? FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
        }

        if (shopUI != null && ingredientSO != null)
        {
            shopUI.TryBuyItem(ingredientSO, EffectivePrice);
        }
    }

    private void SetText(TextMeshProUGUI tmpText, Text uiText, string content)
    {
        if (tmpText != null) tmpText.text = content;
        if (uiText != null) uiText.text = content;
    }
}
