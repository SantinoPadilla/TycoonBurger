using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct DayCustomerLimit
{
    [Tooltip("Día/Servicio a partir del cual aplica este límite.")]
    public int dayNumber;

    [Tooltip("Máximo número de clientes simultáneos permitidos a partir de este día.")]
    public int maxConcurrentCustomers;

    public DayCustomerLimit(int dayNumber, int maxConcurrentCustomers)
    {
        this.dayNumber = dayNumber;
        this.maxConcurrentCustomers = maxConcurrentCustomers;
    }
}

/// <summary>
/// Gestor y generador (Spawner) de clientes.
/// Administra el tiempo entre apariciones, asigna puntos de espera disponibles frente al mostrador
/// y coordina la liberación de lugares cuando los clientes se retiran.
/// </summary>
public class CustomerSpawner : MonoBehaviour
{
    [Header("Prefab de Cliente")]
    [Tooltip("Prefab del cliente con el componente Customer y CustomerUI.")]
    [SerializeField] private GameObject customerPrefab;

    [Header("Configuración de Menú / Productos")]
    [Tooltip("Lista de productos (ProductSO) que los clientes pueden pedir.")]
    [SerializeField] private List<ProductSO> availableProducts = new List<ProductSO>();

    [Header("Puntos de Navegación en Escena")]
    [Tooltip("Punto fuera de pantalla donde aparecen los clientes.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Lista de lugares/puntos de espera frente al mostrador.")]
    [SerializeField] private List<Transform> waitingSpots = new List<Transform>();

    [Tooltip("Punto fuera de pantalla por donde se retiran los clientes.")]
    [SerializeField] private Transform exitPoint;

    [Header("Configuración de Temporizador y Límites")]
    [Tooltip("Tiempo en segundos entre la aparición de cada cliente.")]
    [SerializeField] private float spawnInterval = 5f;

    [Tooltip("Paciencia inicial en segundos asignada a los clientes.")]
    [SerializeField] private float customerPatienceTime = 25f;

    [Tooltip("Máximo número de clientes simultáneos permitidos en pantalla (usado como fallback si customerLimitsByDay está vacío).")]
    [SerializeField] private int maxConcurrentCustomers = 3;

    [Header("Progreso de Clientes por Día")]
    [Tooltip("Reglas para escalar la cantidad máxima de clientes simultáneos según el número de día/turno.\n" +
             "Ejemplo: Día 1 -> 1 cliente, Día 10 -> 2 clientes, Día 15 -> 3 clientes.")]
    [SerializeField] private List<DayCustomerLimit> customerLimitsByDay = new List<DayCustomerLimit>();

    [Tooltip("Si es true, los clientes aparecerán automáticamente al iniciar la escena.")]
    [SerializeField] private bool autoSpawn = true;

    private List<Customer> activeCustomers = new List<Customer>();
    private HashSet<Transform> occupiedSpots = new HashSet<Transform>();
    private float spawnTimer = 0f;

    public int ActiveCustomerCount => activeCustomers.Count;
    public bool AutoSpawn { get => autoSpawn; set => autoSpawn = value; }

    /// <summary>
    /// Obtiene la cantidad máxima de clientes simultáneos activa para el día actual.
    /// </summary>
    public int CurrentMaxConcurrentCustomers
    {
        get
        {
            int currentDay = RestaurantShiftManager.Instance != null ? RestaurantShiftManager.Instance.CurrentShiftNumber : 1;
            return GetMaxConcurrentCustomersForDay(currentDay);
        }
    }

    /// <summary>
    /// Calcula la cantidad máxima de clientes simultáneos para un día específico según las reglas configuradas.
    /// </summary>
    public int GetMaxConcurrentCustomersForDay(int dayNumber)
    {
        if (customerLimitsByDay == null || customerLimitsByDay.Count == 0)
        {
            return maxConcurrentCustomers;
        }

        int targetDay = Mathf.Max(1, dayNumber);
        int effectiveLimit = maxConcurrentCustomers;
        int bestMatchingDay = -1;

        foreach (var rule in customerLimitsByDay)
        {
            if (rule.dayNumber <= targetDay && rule.dayNumber > bestMatchingDay)
            {
                bestMatchingDay = rule.dayNumber;
                effectiveLimit = rule.maxConcurrentCustomers;
            }
        }

        if (bestMatchingDay == -1)
        {
            int minDay = int.MaxValue;
            foreach (var rule in customerLimitsByDay)
            {
                if (rule.dayNumber < minDay)
                {
                    minDay = rule.dayNumber;
                    effectiveLimit = rule.maxConcurrentCustomers;
                }
            }
        }

        return effectiveLimit;
    }

    /// <summary>
    /// Activa la generación de clientes.
    /// </summary>
    public void StartSpawning()
    {
        autoSpawn = true;
        spawnTimer = spawnInterval;
    }

    /// <summary>
    /// Añade un nuevo producto a la lista de productos disponibles que los clientes pueden solicitar.
    /// </summary>
    public void AddAvailableProduct(ProductSO product)
    {
        if (product == null) return;

        if (availableProducts == null)
        {
            availableProducts = new List<ProductSO>();
        }

        if (!availableProducts.Contains(product))
        {
            availableProducts.Add(product);
            Debug.Log($"[CustomerSpawner] Producto '{product.ProductName}' añadido a los pedidos de clientes.");
        }
    }

    /// <summary>
    /// Remueve un producto de la lista de pedidos disponibles de clientes.
    /// </summary>
    public void RemoveAvailableProduct(ProductSO product)
    {
        if (product == null || availableProducts == null) return;

        if (availableProducts.Contains(product))
        {
            availableProducts.Remove(product);
            Debug.Log($"[CustomerSpawner] Producto '{product.ProductName}' removido de los pedidos de clientes.");
        }
    }

    /// <summary>
    /// Detiene la generación de clientes.
    /// </summary>
    public void StopSpawning()
    {
        autoSpawn = false;
    }

    /// <summary>
    /// Establece si la generación automática de clientes está activa.
    /// </summary>
    public void SetSpawningEnabled(bool enabled)
    {
        autoSpawn = enabled;
        if (enabled)
        {
            spawnTimer = spawnInterval;
        }
    }

    /// <summary>
    /// Cancela la atención de todos los clientes activos y les ordena retirarse caminando hacia la salida.
    /// </summary>
    public void DismissAllCustomers()
    {
        Customer[] copy = activeCustomers.ToArray();
        foreach (Customer customer in copy)
        {
            if (customer != null)
            {
                customer.ForceLeaveImmediately();
            }
        }
        activeCustomers.Clear();
        occupiedSpots.Clear();
        Debug.Log("[CustomerSpawner] Se ha ordenado la retirada de todos los clientes activos.");
    }

    private void Start()
    {
        spawnTimer = spawnInterval;
    }

    private void Update()
    {
        if (!autoSpawn) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = spawnInterval;
            TrySpawnCustomer();
        }
    }

    /// <summary>
    /// Intenta instanciar un nuevo cliente si hay lugares de espera disponibles y no se ha alcanzado el límite.
    /// </summary>
    public Customer TrySpawnCustomer()
    {
        if (customerPrefab == null)
        {
            Debug.LogWarning("[CustomerSpawner] ¡No se ha asignado el customerPrefab!");
            return null;
        }

        if (activeCustomers.Count >= CurrentMaxConcurrentCustomers)
        {
            return null;
        }

        Transform freeSpot = GetAvailableWaitingSpot();
        if (freeSpot == null)
        {
            // No hay lugares de espera desocupados en el mostrador
            return null;
        }

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        GameObject customerObj = Instantiate(customerPrefab, spawnPos, Quaternion.identity);
        Customer customer = customerObj.GetComponent<Customer>();

        if (customer != null)
        {
            occupiedSpots.Add(freeSpot);
            activeCustomers.Add(customer);

            customer.InitializeCustomer(
                availableProducts,
                freeSpot,
                exitPoint,
                customerPatienceTime,
                OnCustomerLeft
            );

            Debug.Log($"[CustomerSpawner] Cliente creado y enviado al spot '{freeSpot.name}'. Clientes activos: {activeCustomers.Count}");
        }
        else
        {
            Debug.LogError("[CustomerSpawner] El prefab instanciado no contiene el componente Customer.");
            Destroy(customerObj);
        }

        return customer;
    }

    private Transform GetAvailableWaitingSpot()
    {
        if (waitingSpots == null || waitingSpots.Count == 0) return null;

        foreach (Transform spot in waitingSpots)
        {
            if (spot != null && !occupiedSpots.Contains(spot))
            {
                return spot;
            }
        }
        return null;
    }

    private void OnCustomerLeft(Customer customer, Transform assignedSpot)
    {
        if (customer != null)
        {
            activeCustomers.Remove(customer);
        }
        if (assignedSpot != null)
        {
            occupiedSpots.Remove(assignedSpot);
        }

        Debug.Log($"[CustomerSpawner] Cliente retirado. Spot '{assignedSpot?.name}' liberado. Clientes activos restantes: {activeCustomers.Count}");
    }

    private void OnDrawGizmosSelected()
    {
        // Dibujar Spawn Point
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.4f);
        }

        // Dibujar Exit Point
        if (exitPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(exitPoint.position, 0.4f);
        }

        // Dibujar Waiting Spots
        if (waitingSpots != null)
        {
            Gizmos.color = Color.yellow;
            foreach (Transform spot in waitingSpots)
            {
                if (spot != null)
                {
                    Gizmos.DrawWireCube(spot.position, new Vector3(0.5f, 0.5f, 0f));
                }
            }
        }
    }
}
