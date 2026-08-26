using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gestor principal del ciclo de servicio (Abierto / Cerrado) del restaurante.
/// Controla la duración del turno de trabajo, pausando la generación de clientes al cerrar
/// y abriendo automáticamente la tienda para preparar el siguiente servicio.
/// </summary>
public class RestaurantShiftManager : MonoBehaviour
{
    public enum RestaurantState
    {
        Open,
        Closed
    }

    private static RestaurantShiftManager instance;
    public static RestaurantShiftManager Instance => instance;

    [Header("Configuración de Turno")]
    [Tooltip("Duración base en segundos para el turno/servicio del restaurante.")]
    [SerializeField] private float shiftDuration = 30f;

    [Tooltip("Si es true, el primer turno comenzará automáticamente al iniciar la escena.")]
    [SerializeField] private bool autoStartFirstShift = true;

    [Header("Referencias a Sistemas")]
    [Tooltip("Referencia opcional a CustomerSpawner. Si está vacía, se buscará automáticamente.")]
    [SerializeField] private CustomerSpawner customerSpawner;

    [Tooltip("Referencia opcional a ShopUI. Si está vacía, se buscará automáticamente.")]
    [SerializeField] private ShopUI shopUI;

    [Header("Eventos")]
    public UnityEvent onShiftStarted;
    public UnityEvent onShiftEnded;
    public UnityEvent<float, float> onShiftTimerUpdated; // (RemainingSeconds, TotalDuration)
    public UnityEvent<RestaurantState> onStateChanged;
    public UnityEvent<ShiftSummaryData> onShiftSummaryReady;
    public UnityEvent<int, int> onCustomersUpdated; // (successfulCustomers, failedCustomers)

    private RestaurantState currentState = RestaurantState.Closed;
    private float shiftTimer = 0f;
    private int currentShiftNumber = 0;
    private ShiftSummaryData currentShiftSummary = new ShiftSummaryData();

    public RestaurantState CurrentState => currentState;
    public bool IsOpen => currentState == RestaurantState.Open;
    public bool IsClosed => currentState == RestaurantState.Closed;
    public float RemainingTime => Mathf.Max(0f, shiftTimer);
    public float ShiftDuration => shiftDuration + ShiftTimeUpgradeItemUI.CurrentBonusTime;
    public int CurrentShiftNumber => currentShiftNumber;
    public ShiftSummaryData LastShiftSummary => currentShiftSummary;

    /// <summary>
    /// Calcula la duración total en segundos para el servicio (duración base + mejora de tiempo comprada).
    /// </summary>
    public float GetShiftDurationForDay(int dayNumber)
    {
        return ShiftDuration;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        FindReferencesIfNeeded();

        if (autoStartFirstShift)
        {
            OpenRestaurant();
        }
        else
        {
            CloseRestaurant(triggerShop: false);
        }
    }

    private void Update()
    {
        if (currentState != RestaurantState.Open) return;

        shiftTimer -= Time.deltaTime;
        onShiftTimerUpdated?.Invoke(RemainingTime, ShiftDuration);

        if (shiftTimer <= 0f)
        {
            shiftTimer = 0f;
            CloseRestaurant(triggerShop: true);
        }
    }

    /// <summary>
    /// Inicia un nuevo turno/servicio. Reinicia el tiempo, las métricas y abre el restaurante.
    /// </summary>
    public void OpenRestaurant()
    {
        FindReferencesIfNeeded();

        currentState = RestaurantState.Open;
        currentShiftNumber++;
        float activeShiftDuration = ShiftDuration;
        shiftTimer = activeShiftDuration;

        // Reiniciar estadísticas para el nuevo servicio
        currentShiftSummary.Reset(currentShiftNumber);

        Debug.Log($"[RestaurantShiftManager] ¡Restaurante ABIERTO! Iniciando servicio #{currentShiftNumber} ({activeShiftDuration}s - Base: {shiftDuration}s, Bonus Mejora: {ShiftTimeUpgradeItemUI.CurrentBonusTime}s).");

        if (shopUI != null && shopUI.IsOpen)
        {
            shopUI.CloseShop();
        }

        if (customerSpawner != null)
        {
            customerSpawner.StartSpawning();
        }

        onStateChanged?.Invoke(currentState);
        onShiftStarted?.Invoke();
        onShiftTimerUpdated?.Invoke(shiftTimer, activeShiftDuration);
    }

    /// <summary>
    /// Cierra el servicio actual. Detiene clientes, compila el resumen y abre la tienda de insumos.
    /// </summary>
    public void CloseRestaurant(bool triggerShop = true)
    {
        FindReferencesIfNeeded();

        currentState = RestaurantState.Closed;
        shiftTimer = 0f;

        Debug.Log($"[RestaurantShiftManager] ¡Restaurante CERRADO! Servicio #{currentShiftNumber} finalizado. Ganancias: ${currentShiftSummary.moneyEarned}, Clientes: {currentShiftSummary.successfulCustomers}");

        if (customerSpawner != null)
        {
            customerSpawner.StopSpawning();
            customerSpawner.DismissAllCustomers();
        }

        // Limpiar completamente la cocina, estaciones y manos del jugador
        ClearAllKitchenItemsAndStations();

        onStateChanged?.Invoke(currentState);
        onShiftEnded?.Invoke();
        onShiftTimerUpdated?.Invoke(0f, shiftDuration);
        onShiftSummaryReady?.Invoke(currentShiftSummary);

        if (triggerShop && shopUI != null)
        {
            StartCoroutine(OpenShopNextFrame());
        }
    }

    /// <summary>
    /// Registra la venta de una unidad de un producto durante el turno actual.
    /// </summary>
    public void RegisterProductSold(string productName)
    {
        if (string.IsNullOrEmpty(productName)) return;
        currentShiftSummary.RecordProductSale(productName, 1);
        Debug.Log($"[RestaurantShiftManager] Producto vendido registrado: '{productName}'. Total vendido en el turno: {currentShiftSummary.GetProductSalesCount(productName)}");
    }

    /// <summary>
    /// Registra la atención exitosa de un cliente y el dinero abonado en el turno actual.
    /// </summary>
    public void RegisterCustomerServed(int orderTotal)
    {
        currentShiftSummary.successfulCustomers++;
        if (orderTotal > 0)
        {
            currentShiftSummary.moneyEarned += orderTotal;
        }
        Debug.Log($"[RestaurantShiftManager] Cliente atendido con éxito. Ganancias del turno: ${currentShiftSummary.moneyEarned}");
        onCustomersUpdated?.Invoke(currentShiftSummary.successfulCustomers, currentShiftSummary.failedCustomers);
    }

    /// <summary>
    /// Registra un cliente perdido/no atendido durante el turno actual.
    /// </summary>
    public void RegisterCustomerFailed()
    {
        currentShiftSummary.failedCustomers++;
        Debug.Log($"[RestaurantShiftManager] Cliente perdido registrado. Total perdidos en el turno: {currentShiftSummary.failedCustomers}");
        onCustomersUpdated?.Invoke(currentShiftSummary.successfulCustomers, currentShiftSummary.failedCustomers);
    }

    /// <summary>
    /// Limpia todas las estaciones de cocina y elimina objetos en las manos del jugador o tirados en la escena.
    /// </summary>
    public void ClearAllKitchenItemsAndStations()
    {
        // 1. Manos del jugador
        PlayerCarrySystem[] carriers = FindObjectsByType<PlayerCarrySystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var carrier in carriers)
        {
            if (carrier != null) carrier.ClearCarriedItems();
        }

        // 2. Planchas de cocina
        CookingGrill[] grills = FindObjectsByType<CookingGrill>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var grill in grills)
        {
            if (grill != null) grill.ResetStation();
        }

        // 3. Freidoras
        Freidora[] freidoras = FindObjectsByType<Freidora>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var freidora in freidoras)
        {
            if (freidora != null) freidora.ResetStation();
        }

        // 4. Mesas de armado
        MesaDeArmado[] mesas = FindObjectsByType<MesaDeArmado>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mesa in mesas)
        {
            if (mesa != null) mesa.ResetStation();
        }

        // 5. Estaciones de soda
        SodaStacion[] sodas = FindObjectsByType<SodaStacion>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var soda in sodas)
        {
            if (soda != null) soda.ResetStation();
        }

        // 6. Limpieza de cualquier objeto suelto HoldableItem en la escena
        HoldableItem[] items = FindObjectsByType<HoldableItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in items)
        {
            if (item != null && item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }

        // 7. Slots de entrada independientes
        StationInputSlot[] inputSlots = FindObjectsByType<StationInputSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var slot in inputSlots)
        {
            if (slot != null) slot.ResetSlot();
        }

        // 8. Slots de salida independientes
        StationOutputSlot[] outputSlots = FindObjectsByType<StationOutputSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var slot in outputSlots)
        {
            if (slot != null)
            {
                for (int i = slot.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(slot.transform.GetChild(i).gameObject);
                }
            }
        }

        // 9. Limpieza exhaustiva de cualquier objeto restante con componentes de producto o ingrediente
        Ingredient[] ingredients = FindObjectsByType<Ingredient>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var ing in ingredients)
        {
            if (ing != null && ing.gameObject != null) Destroy(ing.gameObject);
        }

        SellableProduct[] sellables = FindObjectsByType<SellableProduct>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var sp in sellables)
        {
            if (sp != null && sp.gameObject != null) Destroy(sp.gameObject);
        }

        HamburguesaCompleta[] burgers = FindObjectsByType<HamburguesaCompleta>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in burgers)
        {
            if (b != null && b.gameObject != null) Destroy(b.gameObject);
        }

        Debug.Log("[RestaurantShiftManager] ¡Limpieza completa de la cocina realizada al cerrar!");
    }

    private IEnumerator OpenShopNextFrame()
    {
        // Esperar un frame por si hay limpiezas de UI pendientes
        yield return null;
        if (shopUI != null && !shopUI.IsOpen)
        {
            shopUI.OpenShop();
        }
    }

    private void FindReferencesIfNeeded()
    {
        if (customerSpawner == null)
        {
            customerSpawner = FindFirstObjectByType<CustomerSpawner>(FindObjectsInactive.Include);
        }

        if (shopUI == null)
        {
            shopUI = FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
        }
    }
}
