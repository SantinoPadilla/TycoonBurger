using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum CustomerState
{
    Entering,
    WaitingToOrder,
    Waiting,
    LeavingHappy,
    LeavingAngry,
    LeavingDismissed
}

/// <summary>
/// Componente para el Cliente en el juego.
/// Se desplaza al mostrador, muestra su pedido (1 a 3 productos), espera con una barra de paciencia,
/// recibe productos uno a uno del jugador mediante IInteractable y paga al completarse el pedido.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Customer : MonoBehaviour, IInteractable
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float stopDistanceThreshold = 0.05f;

    [Header("Configuración de Paciencia")]
    [Tooltip("Tiempo de espera total para entregar el pedido antes de que el cliente se retire molesto.")]
    [SerializeField] private float maxPatienceTime = 20f;

    [Tooltip("Tiempo máximo de espera para que el jugador tome el pedido antes de que el cliente se retire molesto.")]
    [SerializeField] private float maxTakeOrderTime = 15f;

    [Header("Referencias UI")]
    [SerializeField] private CustomerUI customerUI;

    [Header("Eventos")]
    public UnityEvent<int> onOrderCompleted;
    public UnityEvent onOrderFailed;

    private CustomerState currentState = CustomerState.Entering;
    private List<ProductSO> requestedProducts = new List<ProductSO>();
    private List<ProductSO> remainingProducts = new List<ProductSO>();
    private int totalOrderPrice = 0;
    private float currentPatienceTimer = 0f;

    private Transform targetWaitingSpot;
    private Transform targetExitPoint;

    private System.Action<Customer, Transform> onCustomerLeftCallback;

    public CustomerState CurrentState => currentState;
    public Transform AssignedWaitingSpot => targetWaitingSpot;
    public int TotalOrderPrice => totalOrderPrice;

    private void Awake()
    {
        if (customerUI == null)
        {
            customerUI = GetComponentInChildren<CustomerUI>(true);
        }
    }

    /// <summary>
    /// Inicializa los datos del cliente, generando un pedido aleatorio y asignando sus puntos de destino.
    /// </summary>
    public void InitializeCustomer(
        List<ProductSO> availableProducts,
        Transform waitingSpot,
        Transform exitPoint,
        System.Action<Customer, Transform> onLeftCallback)
    {
        targetWaitingSpot = waitingSpot;
        targetExitPoint = exitPoint;
        onCustomerLeftCallback = onLeftCallback;

        GenerateRandomOrder(availableProducts);

        currentPatienceTimer = maxPatienceTime;
        currentState = CustomerState.Entering;

        if (customerUI != null)
        {
            customerUI.HideOrderUI();
        }
    }

    private void GenerateRandomOrder(List<ProductSO> availableProducts)
    {
        requestedProducts.Clear();
        remainingProducts.Clear();
        totalOrderPrice = 0;

        if (availableProducts == null || availableProducts.Count == 0)
        {
            Debug.LogWarning("[Customer] No hay productos disponibles asignados para generar un pedido.");
            return;
        }

        // Generar un pedido de 1, 2 o 3 productos aleatorios
        int productCount = Random.Range(1, 4);

        for (int i = 0; i < productCount; i++)
        {
            ProductSO randomProduct = availableProducts[Random.Range(0, availableProducts.Count)];
            if (randomProduct != null)
            {
                requestedProducts.Add(randomProduct);
                remainingProducts.Add(randomProduct);
                totalOrderPrice += randomProduct.SellPrice;
            }
        }

        Debug.Log($"[Customer] Pedido generado: {requestedProducts.Count} productos. Precio total: ${totalOrderPrice}");
    }

    private void Update()
    {
        switch (currentState)
        {
            case CustomerState.Entering:
                MoveTowardsPoint(targetWaitingSpot != null ? targetWaitingSpot.position : transform.position, () =>
                {
                    // Llegó al punto de espera en el mostrador
                    currentState = CustomerState.WaitingToOrder;
                    currentPatienceTimer = maxTakeOrderTime > 0f ? maxTakeOrderTime : maxPatienceTime;
                    if (customerUI != null)
                    {
                        customerUI.ShowPatienceBarOnly();
                        customerUI.UpdatePatience(1f);
                    }
                    Debug.Log("[Customer] Llegó al mostrador y espera a que el jugador tome su pedido.");
                });
                break;

            case CustomerState.WaitingToOrder:
                // Espera a que el jugador tome el pedido con la barra de paciencia activa
                float totalTakeOrderTime = maxTakeOrderTime > 0f ? maxTakeOrderTime : maxPatienceTime;
                currentPatienceTimer -= Time.deltaTime;
                float takeOrderPatienceNorm = currentPatienceTimer / totalTakeOrderTime;

                if (customerUI != null)
                {
                    customerUI.UpdatePatience(takeOrderPatienceNorm);
                }

                if (currentPatienceTimer <= 0f)
                {
                    LeaveAngry();
                }
                break;

            case CustomerState.Waiting:
                // Actualizar barra de paciencia
                currentPatienceTimer -= Time.deltaTime;
                float patienceNormalized = currentPatienceTimer / maxPatienceTime;

                if (customerUI != null)
                {
                    customerUI.UpdatePatience(patienceNormalized);
                }

                // Si se agota el tiempo
                if (currentPatienceTimer <= 0f)
                {
                    LeaveAngry();
                }
                break;

            case CustomerState.LeavingHappy:
            case CustomerState.LeavingAngry:
            case CustomerState.LeavingDismissed:
                Vector3 exitPos = targetExitPoint != null ? targetExitPoint.position : transform.position + Vector3.left * 10f;
                MoveTowardsPoint(exitPos, () =>
                {
                    // Llegó al punto de salida, se destruye el objeto
                    onCustomerLeftCallback?.Invoke(this, targetWaitingSpot);
                    Destroy(gameObject);
                });
                break;
        }
    }

    private void MoveTowardsPoint(Vector3 targetPos, System.Action onArrived)
    {
        Vector3 currentPos = transform.position;
        // Ignorar la Z si es un juego 2D
        targetPos.z = currentPos.z;

        float distance = Vector2.Distance(currentPos, targetPos);
        if (distance <= stopDistanceThreshold)
        {
            transform.position = targetPos;
            onArrived?.Invoke();
        }
        else
        {
            transform.position = Vector3.MoveTowards(currentPos, targetPos, moveSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Interacción del jugador con el cliente (tecla E).
    /// Si está en WaitingToOrder, toma el pedido y activa el tiempo de paciencia.
    /// Si está en Waiting, intenta entregar el producto sostenido en la mano del jugador.
    /// </summary>
    public void Interact()
    {
        if (currentState == CustomerState.WaitingToOrder)
        {
            // Tomar el pedido manualmente
            currentState = CustomerState.Waiting;
            currentPatienceTimer = maxPatienceTime;

            if (customerUI != null)
            {
                customerUI.UpdateOrderIcons(remainingProducts);
                customerUI.UpdatePatience(1f);
                customerUI.ShowOrderUI();
            }

            Debug.Log("[Customer] ¡Pedido tomado! Comenzó el tiempo de espera del cliente.");
            return;
        }

        if (currentState != CustomerState.Waiting) return;

        ICarrier carrier = FindFirstObjectByType<PlayerCarrySystem>();
        if (carrier == null || !carrier.HasItems)
        {
            Debug.Log("[Customer] Necesitas tener un producto en las manos para entregármelo.");
            return;
        }

        ICarryable itemInHand = carrier.GetCarriedItem();
        if (itemInHand == null) return;

        // Buscar si el producto en mano coincide con alguno de los productos restantes en el pedido
        int matchingIndex = FindMatchingProductIndex(itemInHand);

        if (matchingIndex >= 0)
        {
            // Producto ACEPTADO
            ProductSO acceptedProduct = remainingProducts[matchingIndex];
            remainingProducts.RemoveAt(matchingIndex);

            // Retirar el producto de las manos del jugador y destruirlo
            carrier.TakeCarriedItem();
            Destroy(itemInHand.gameObject);

            Debug.Log($"[Customer] ¡Producto '{acceptedProduct.ProductName}' recibido! Quedan {remainingProducts.Count} productos pendientes.");

            // Actualizar interfaz del globo
            if (customerUI != null)
            {
                customerUI.UpdateOrderIcons(remainingProducts);
            }

            // Si se entregaron todos los productos, el pedido está completo
            if (remainingProducts.Count == 0)
            {
                LeaveHappy();
            }
        }
        else
        {
            // Producto RECHAZADO
            Debug.Log($"[Customer] No pedí este producto ('{itemInHand.ItemName}'). El cliente lo rechazó.");
        }
    }

    public string GetInteractPrompt()
    {
        if (currentState == CustomerState.WaitingToOrder) return "Tomar pedido";
        if (currentState == CustomerState.Waiting) return "Entregar pedido";
        return "";
    }

    private int FindMatchingProductIndex(ICarryable item)
    {
        GameObject itemObj = item.gameObject;

        // 1. Verificar por componente SellableProduct
        SellableProduct sellable = itemObj.GetComponent<SellableProduct>();
        if (sellable != null && sellable.ProductData != null)
        {
            for (int i = 0; i < remainingProducts.Count; i++)
            {
                if (remainingProducts[i] == sellable.ProductData) return i;
            }
        }

        // 2. Verificar por componente HamburguesaCompleta
        HamburguesaCompleta burger = itemObj.GetComponent<HamburguesaCompleta>();
        if (burger != null && burger.ProductData != null)
        {
            for (int i = 0; i < remainingProducts.Count; i++)
            {
                if (remainingProducts[i] == burger.ProductData) return i;
            }
        }

        // 3. Comparación por nombre (ItemName o ProductName)
        string itemName = item.ItemName;
        for (int i = 0; i < remainingProducts.Count; i++)
        {
            if (remainingProducts[i] != null && remainingProducts[i].ProductName.Equals(itemName, System.StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private void LeaveHappy()
    {
        currentState = CustomerState.LeavingHappy;

        if (customerUI != null)
        {
            customerUI.HideOrderUI();
        }

        int finalOrderPrice = ProfitUpgradeItemUI.ApplyProfitMultiplier(totalOrderPrice);

        // Registrar atención exitosa y los productos del pedido completo en el gestor de turnos
        RestaurantShiftManager shiftManager = RestaurantShiftManager.Instance != null ? RestaurantShiftManager.Instance : FindFirstObjectByType<RestaurantShiftManager>();
        if (shiftManager != null)
        {
            shiftManager.RegisterCustomerServed(finalOrderPrice);

            foreach (ProductSO product in requestedProducts)
            {
                if (product != null)
                {
                    shiftManager.RegisterProductSold(product.ProductName);
                }
            }
        }

        // Pagar total del pedido al MoneyManager
        IMoneyService moneyService = FindFirstObjectByType<MoneyManager>();
        if (moneyService != null)
        {
            moneyService.AddMoney(finalOrderPrice);
            Debug.Log($"[Customer] ¡Pedido pagado! El cliente pagó ${finalOrderPrice} (Precio base: ${totalOrderPrice}).");
        }
        else
        {
            Debug.LogWarning("[Customer] No se encontró MoneyManager para abonar el pago.");
        }

        // Incrementar reputación por cliente feliz
        ReputationManager reputationManager = ReputationManager.Instance != null ? ReputationManager.Instance : FindFirstObjectByType<ReputationManager>();
        if (reputationManager != null)
        {
            reputationManager.AddHappyCustomerReputation();
        }

        onOrderCompleted?.Invoke(finalOrderPrice);
    }

    private void LeaveAngry()
    {
        currentState = CustomerState.LeavingAngry;

        if (customerUI != null)
        {
            customerUI.HideOrderUI();
        }

        // Registrar cliente fallido por tiempo en el gestor de turnos
        RestaurantShiftManager shiftManager = RestaurantShiftManager.Instance != null ? RestaurantShiftManager.Instance : FindFirstObjectByType<RestaurantShiftManager>();
        if (shiftManager != null)
        {
            shiftManager.RegisterCustomerFailed();
        }

        // Restar reputación por cliente furioso
        ReputationManager reputationManager = ReputationManager.Instance != null ? ReputationManager.Instance : FindFirstObjectByType<ReputationManager>();
        if (reputationManager != null)
        {
            reputationManager.RemoveAngryCustomerReputation();
        }

        Debug.Log("[Customer] ¡Tiempo agotado! El cliente se marcha molesto sin pagar.");
        onOrderFailed?.Invoke();
    }

    /// <summary>
    /// Fuerza al cliente a cancelar su espera y retirarse caminando inmediatamente hacia la salida.
    /// Útil para el fin del turno de servicio. No se considera un cliente fallido/enojado.
    /// </summary>
    public void ForceLeaveImmediately()
    {
        if (currentState == CustomerState.LeavingHappy || 
            currentState == CustomerState.LeavingAngry || 
            currentState == CustomerState.LeavingDismissed) return;

        currentState = CustomerState.LeavingDismissed;

        if (customerUI != null)
        {
            customerUI.HideOrderUI();
        }

        Debug.Log("[Customer] Servicio finalizado. El cliente se retira neutralmente hacia la salida.");
    }
}
