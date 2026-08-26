using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Controlador principal de la Interfaz de Usuario para la Tienda de Ingredientes.
/// Trabaja con casillas ShopItemUI acomodadas manualmente en la escena/canvas,
/// administrando visibilidad (activado/desactivado), dinero y compras.
/// </summary>
public class ShopUI : MonoBehaviour
{
    private static ShopUI instance;
    public static ShopUI Instance => instance;

    [Header("Contenedor Principal de UI (Panel / Canvas de Tienda)")]
    [Tooltip("Panel o CanvasGroup raíz que engloba el fondo y toda la UI de la tienda. Si está vacío, se usará este mismo GameObject.")]
    [SerializeField] private GameObject shopPanelContainer;

    [Header("Dinero Actual del Jugador (Tienda)")]
    [SerializeField] private TextMeshProUGUI tmpMoneyText;
    [SerializeField] private Text uiMoneyText;
    [SerializeField] private string moneyPrefix = "$ ";

    [Header("UI General de Dinero (HUD Principal)")]
    [Tooltip("Objeto o texto de dinero del HUD principal que se apaga al abrir la tienda y se vuelve a encender al cerrarla. Si está vacío, se buscará automáticamente el componente MoneyCounterUI en la escena.")]
    [SerializeField] private GameObject hudMoneyUI;

    [Header("Ítems Pre-colocados en Escena")]
    [Tooltip("Lista manual de casillas de ShopItemUI colocadas en la escena. Si está vacía, se buscarán automáticamente en los hijos.")]
    [SerializeField] private List<ShopItemUI> shopItemsInScene = new List<ShopItemUI>();

    [Tooltip("Contenedor opcional donde se encuentran los ShopItemUI (para búsqueda automática).")]
    [SerializeField] private Transform itemsContainer;

    [Header("Botón de Cierre y Opciones")]
    [SerializeField] private Button closeButton;
    [Tooltip("Botón opcional 'ABRIR' para iniciar el siguiente servicio cuando el restaurante está cerrado.")]
    [SerializeField] private Button openRestaurantButton;
    [SerializeField] private TextMeshProUGUI tmpHeaderTitle;
    [SerializeField] private Text uiHeaderTitle;
    [SerializeField] private bool closeWithEscapeKey = true;
    [SerializeField] private bool closeWithInteractKey = true;

    [Header("Configuración de Pestañas (Tabs)")]
    [Tooltip("Panel contenedor de los ingredientes/insumos de la tienda.")]
    [SerializeField] private GameObject insumosPanel;

    [Tooltip("Panel contenedor de las mejoras de la tienda.")]
    [SerializeField] private GameObject mejorasPanel;

    [Tooltip("Botón para seleccionar la pestaña de Insumos/Ingredientes.")]
    [SerializeField] private Button tabInsumosButton;

    [Tooltip("Botón para seleccionar la pestaña de Mejoras.")]
    [SerializeField] private Button tabMejorasButton;

    [Header("Colores Visuales de Pestaña (Opcional)")]
    [SerializeField] private Color activeTabColor = Color.white;
    [SerializeField] private Color inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    private bool isOpen = false;
    private int openedFrame = -1;
    private TiendaEstacion currentStation;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseShop);
        }

        if (openRestaurantButton != null)
        {
            openRestaurantButton.onClick.RemoveAllListeners();
            openRestaurantButton.onClick.AddListener(OnOpenRestaurantButtonClicked);
        }

        if (tabInsumosButton != null)
        {
            tabInsumosButton.onClick.RemoveAllListeners();
            tabInsumosButton.onClick.AddListener(ShowInsumosTab);
        }

        if (tabMejorasButton != null)
        {
            tabMejorasButton.onClick.RemoveAllListeners();
            tabMejorasButton.onClick.AddListener(ShowMejorasTab);
        }

        // Buscar automáticamente el HUD de dinero si no se asignó en el Inspector
        if (hudMoneyUI == null)
        {
            MoneyCounterUI moneyCounter = FindFirstObjectByType<MoneyCounterUI>(FindObjectsInactive.Include);
            if (moneyCounter != null)
            {
                hudMoneyUI = moneyCounter.gameObject;
            }
        }

        CloseShop();
    }

    /// <summary>
    /// Acción ejecutada al hacer clic en el botón 'ABRIR' de la tienda.
    /// </summary>
    public void OnOpenRestaurantButtonClicked()
    {
        if (RestaurantShiftManager.Instance != null)
        {
            RestaurantShiftManager.Instance.OpenRestaurant();
        }
        else
        {
            CloseShop();
        }
    }

    private void Update()
    {
        if (!isOpen) return;

        // Evitar que la misma pulsación de tecla que abrió la tienda la cierre en el mismo frame
        if (Time.frameCount <= openedFrame) return;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (closeWithEscapeKey && keyboard.escapeKey.wasPressedThisFrame)
            {
                CloseShop();
                return;
            }
        }

        var gamepad = Gamepad.current;
        if (gamepad != null && closeWithInteractKey)
        {
            if (gamepad.buttonEast.wasPressedThisFrame || gamepad.buttonNorth.wasPressedThisFrame)
            {
                CloseShop();
            }
        }
    }

    /// <summary>
    /// Alterna la visibilidad del panel de la tienda.
    /// </summary>
    public void ToggleShop(List<ShopItemConfig> items = null, TiendaEstacion station = null)
    {
        if (isOpen)
        {
            CloseShop();
        }
        else
        {
            OpenShop(items, station);
        }
    }

    /// <summary>
    /// Abre el panel de la tienda y actualiza las casillas de ingredientes de la escena.
    /// </summary>
    public void OpenShop(List<ShopItemConfig> items = null, TiendaEstacion station = null)
    {
        currentStation = station;
        isOpen = true;
        openedFrame = Time.frameCount;

        // Ocultar el dinero del HUD principal
        if (hudMoneyUI == null)
        {
            MoneyCounterUI moneyCounter = FindFirstObjectByType<MoneyCounterUI>(FindObjectsInactive.Include);
            if (moneyCounter != null) hudMoneyUI = moneyCounter.gameObject;
        }
        if (hudMoneyUI != null)
        {
            hudMoneyUI.SetActive(false);
        }

        // Activar el GameObject propio y el contenedor visual
        gameObject.SetActive(true);
        if (shopPanelContainer != null)
        {
            shopPanelContainer.SetActive(true);
        }

        // Actualizar título del panel si la tienda se abrió por fin de turno
        bool isClosedPhase = RestaurantShiftManager.Instance != null && RestaurantShiftManager.Instance.IsClosed;
        string headerMsg = isClosedPhase ? "COMPRA INSUMOS PARA EL PRÓXIMO SERVICIO" : "TIENDA DE INSUMOS";
        if (tmpHeaderTitle != null) tmpHeaderTitle.text = headerMsg;
        if (uiHeaderTitle != null) uiHeaderTitle.text = headerMsg;

        if (openRestaurantButton != null)
        {
            openRestaurantButton.gameObject.SetActive(true);
        }

        SubscribeEvents();
        InitializeItemsInScene(items);
        ShowInsumosTab();
        RefreshAllDisplays();
    }

    /// <summary>
    /// Activa la pestaña de Insumos e Ingredientes y oculta el panel de mejoras.
    /// </summary>
    public void ShowInsumosTab()
    {
        if (insumosPanel != null) insumosPanel.SetActive(true);
        if (mejorasPanel != null) mejorasPanel.SetActive(false);

        HighlightTabButton(tabInsumosButton, tabMejorasButton);
    }

    /// <summary>
    /// Activa la pestaña de Mejoras y oculta el panel de insumos.
    /// </summary>
    public void ShowMejorasTab()
    {
        if (insumosPanel != null) insumosPanel.SetActive(false);
        if (mejorasPanel != null) mejorasPanel.SetActive(true);

        HighlightTabButton(tabMejorasButton, tabInsumosButton);
    }

    private void HighlightTabButton(Button selectedButton, Button unselectedButton)
    {
        if (selectedButton != null)
        {
            Graphic targetGraphic = selectedButton.targetGraphic ?? selectedButton.GetComponent<Graphic>();
            if (targetGraphic != null) targetGraphic.color = activeTabColor;
        }

        if (unselectedButton != null)
        {
            Graphic targetGraphic = unselectedButton.targetGraphic ?? unselectedButton.GetComponent<Graphic>();
            if (targetGraphic != null) targetGraphic.color = inactiveTabColor;
        }
    }

    /// <summary>
    /// Cierra el panel de la tienda, oculta la UI completa y desvincula eventos.
    /// </summary>
    public void CloseShop()
    {
        isOpen = false;
        UnsubscribeEvents();

        // Volver a activar el dinero del HUD principal
        if (hudMoneyUI == null)
        {
            MoneyCounterUI moneyCounter = FindFirstObjectByType<MoneyCounterUI>(FindObjectsInactive.Include);
            if (moneyCounter != null) hudMoneyUI = moneyCounter.gameObject;
        }
        if (hudMoneyUI != null)
        {
            hudMoneyUI.SetActive(true);
        }

        // Apagar el panel asignado
        if (shopPanelContainer != null)
        {
            shopPanelContainer.SetActive(false);
        }

        // Apagar el GameObject del script ShopUI para garantizar que toda la interfaz desaparezca
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Intenta comprar un ingrediente descontando dinero y aumentando el stock global de la cocina.
    /// </summary>
    public void TryBuyItem(IngredientSO ingredient, int unitPrice)
    {
        if (ingredient == null) return;

        IMoneyService moneyService = FindFirstObjectByType<MoneyManager>();
        if (moneyService == null)
        {
            Debug.LogWarning("[ShopUI] No se encontró un IMoneyService (MoneyManager) para procesar la compra.");
            return;
        }

        if (moneyService.TrySpendMoney(unitPrice))
        {
            // Añadir ingrediente al inventario global de cocina
            IKitchenInventory globalInventory = FindFirstObjectByType<GlobalKitchenInventory>();
            if (globalInventory != null)
            {
                globalInventory.AddIngredient(ingredient, 1);
            }
            else
            {
                Debug.LogWarning("[ShopUI] No se encontró GlobalKitchenInventory en la escena.");
            }

            RefreshAllDisplays();
            Debug.Log($"[ShopUI] Compra realizada: +1 '{ingredient.IngredientName}' por ${unitPrice}.");
        }
        else
        {
            Debug.Log($"[ShopUI] Saldo insuficiente para comprar '{ingredient.IngredientName}' (${unitPrice}).");
        }
    }

    /// <summary>
    /// Obtiene todas las casillas de ingrediente pre-colocadas en la escena.
    /// </summary>
    public List<ShopItemUI> GetAllShopItems()
    {
        if (shopItemsInScene != null && shopItemsInScene.Count > 0)
        {
            return shopItemsInScene;
        }

        ShopItemUI[] foundItems = null;
        if (itemsContainer != null)
        {
            foundItems = itemsContainer.GetComponentsInChildren<ShopItemUI>(true);
        }
        else
        {
            foundItems = GetComponentsInChildren<ShopItemUI>(true);
        }

        if (foundItems != null)
        {
            shopItemsInScene = new List<ShopItemUI>(foundItems);
        }

        return shopItemsInScene ?? new List<ShopItemUI>();
    }

    private void InitializeItemsInScene(List<ShopItemConfig> stationConfigs)
    {
        List<ShopItemUI> items = GetAllShopItems();

        // Si la estación especificó una lista de productos permitidos
        HashSet<IngredientSO> allowedIngredients = null;

        if (stationConfigs != null && stationConfigs.Count > 0)
        {
            allowedIngredients = new HashSet<IngredientSO>();
            foreach (var cfg in stationConfigs)
            {
                if (cfg.ingredient != null)
                {
                    allowedIngredients.Add(cfg.ingredient);
                }
            }
        }

        foreach (var itemUI in items)
        {
            if (itemUI == null) continue;

            itemUI.InitializeInScene(this);

            // Determinar si este ítem debe ser visible según la estación y su propiedad itemEnabled
            bool isAllowedByStation = (allowedIngredients == null) || (itemUI.CurrentIngredient != null && allowedIngredients.Contains(itemUI.CurrentIngredient));
            bool shouldBeActive = itemUI.IsItemEnabled && isAllowedByStation;

            itemUI.gameObject.SetActive(shouldBeActive);
        }
    }

    public void RefreshAllDisplays()
    {
        int currentMoney = 0;
        IMoneyService moneyService = FindFirstObjectByType<MoneyManager>();
        if (moneyService != null)
        {
            currentMoney = moneyService.CurrentMoney;
        }

        // Actualizar texto de dinero
        string moneyStr = $"{moneyPrefix}{currentMoney}";
        if (tmpMoneyText != null) tmpMoneyText.text = moneyStr;
        if (uiMoneyText != null) uiMoneyText.text = moneyStr;

        // Actualizar cada casilla activa de ingrediente
        IKitchenInventory globalInventory = FindFirstObjectByType<GlobalKitchenInventory>();
        List<ShopItemUI> items = GetAllShopItems();

        foreach (var itemUI in items)
        {
            if (itemUI == null || !itemUI.gameObject.activeSelf || itemUI.CurrentIngredient == null) continue;

            int ownedStock = (globalInventory != null) ? globalInventory.GetIngredientCount(itemUI.CurrentIngredient) : 0;
            itemUI.UpdateDisplay(currentMoney, ownedStock);
        }

        // Actualizar tarjetas de mejora de la freidora en la tienda
        FreidoraUpgradeItemUI[] freidoraUpgrades = GetComponentsInChildren<FreidoraUpgradeItemUI>(true);
        if (freidoraUpgrades != null)
        {
            foreach (var upgradeUI in freidoraUpgrades)
            {
                if (upgradeUI != null)
                {
                    upgradeUI.UpdateDisplay(currentMoney);
                }
            }
        }

        // Actualizar tarjetas de mejora de la plancha en la tienda
        PlanchaUpgradeItemUI[] planchaUpgrades = GetComponentsInChildren<PlanchaUpgradeItemUI>(true);
        if (planchaUpgrades != null)
        {
            foreach (var upgradeUI in planchaUpgrades)
            {
                if (upgradeUI != null)
                {
                    upgradeUI.UpdateDisplay(currentMoney);
                }
            }
        }

        // Actualizar tarjetas de mejora de la estación de soda en la tienda
        SodaUpgradeItemUI[] sodaUpgrades = GetComponentsInChildren<SodaUpgradeItemUI>(true);
        if (sodaUpgrades != null)
        {
            foreach (var upgradeUI in sodaUpgrades)
            {
                if (upgradeUI != null)
                {
                    upgradeUI.UpdateDisplay(currentMoney);
                }
            }
        }

        // Actualizar tarjetas de mejora de la mesa de armado en la tienda
        MesaDeArmadoUpgradeItemUI[] mesaUpgrades = GetComponentsInChildren<MesaDeArmadoUpgradeItemUI>(true);
        if (mesaUpgrades != null)
        {
            foreach (var upgradeUI in mesaUpgrades)
            {
                if (upgradeUI != null)
                {
                    upgradeUI.UpdateDisplay(currentMoney);
                }
            }
        }

        // Actualizar tarjetas de mejora de velocidad del jugador en la tienda
        PlayerSpeedUpgradeItemUI[] speedUpgrades = GetComponentsInChildren<PlayerSpeedUpgradeItemUI>(true);
        if (speedUpgrades != null)
        {
            foreach (var upgradeUI in speedUpgrades)
            {
                if (upgradeUI != null)
                {
                    upgradeUI.UpdateDisplay(currentMoney);
                }
            }
        }

        // Actualizar tarjetas de mejora de capacidad del jugador en la tienda
        PlayerCapacityUpgradeItemUI[] capacityUpgrades = GetComponentsInChildren<PlayerCapacityUpgradeItemUI>(true);
        if (capacityUpgrades != null)
        {
            foreach (var upgradeUI in capacityUpgrades)
            {
                if (upgradeUI != null)
                {
                    upgradeUI.UpdateDisplay(currentMoney);
                }
            }
        }

        // Actualizar tarjetas de mejora de profit / ganancias en la tienda
        ProfitUpgradeItemUI[] profitUpgrades = GetComponentsInChildren<ProfitUpgradeItemUI>(true);
        if (profitUpgrades != null)
        {
            foreach (var upgradeUI in profitUpgrades)
            {
                if (upgradeUI != null)
                {
                    upgradeUI.UpdateDisplay(currentMoney);
                }
            }
        }
    }

    private void SubscribeEvents()
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.onMoneyChanged.AddListener(OnMoneyChanged);
        }

        if (GlobalKitchenInventory.Instance != null)
        {
            GlobalKitchenInventory.Instance.onIngredientStockChanged.AddListener(OnStockChanged);
        }
    }

    private void UnsubscribeEvents()
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.onMoneyChanged.RemoveListener(OnMoneyChanged);
        }

        if (GlobalKitchenInventory.Instance != null)
        {
            GlobalKitchenInventory.Instance.onIngredientStockChanged.RemoveListener(OnStockChanged);
        }
    }

    private void OnMoneyChanged(int newMoney)
    {
        RefreshAllDisplays();
    }

    private void OnStockChanged(IngredientSO ingredient, int newCount)
    {
        RefreshAllDisplays();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }
}
