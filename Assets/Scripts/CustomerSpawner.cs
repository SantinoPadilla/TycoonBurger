using System.Collections.Generic;
using UnityEngine;

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

    [Tooltip("Máximo número de clientes simultáneos permitidos en pantalla.")]
    [SerializeField] private int maxConcurrentCustomers = 3;

    [Tooltip("Si es true, los clientes aparecerán automáticamente al iniciar la escena.")]
    [SerializeField] private bool autoSpawn = true;

    private List<Customer> activeCustomers = new List<Customer>();
    private HashSet<Transform> occupiedSpots = new HashSet<Transform>();
    private float spawnTimer = 0f;

    private int? overrideMaxConcurrentCustomers = null;
    private float? overrideSpawnInterval = null;

    public int ActiveCustomerCount => activeCustomers.Count;
    public bool AutoSpawn { get => autoSpawn; set => autoSpawn = value; }

    /// <summary>
    /// Tiempo actual en segundos entre la aparición de cada cliente (considera mejoras aplicadas).
    /// </summary>
    public float CurrentSpawnInterval => overrideSpawnInterval.HasValue ? overrideSpawnInterval.Value : spawnInterval;

    /// <summary>
    /// Obtiene la cantidad máxima de clientes simultáneos activa o según mejoras aplicadas.
    /// </summary>
    public int CurrentMaxConcurrentCustomers => overrideMaxConcurrentCustomers.HasValue ? overrideMaxConcurrentCustomers.Value : maxConcurrentCustomers;

    /// <summary>
    /// Establece los valores modificados de la mejora de clientes (máximo número simultáneo e intervalo de generación).
    /// </summary>
    public void SetUpgradeCustomerLimits(int maxConcurrent, float interval)
    {
        overrideMaxConcurrentCustomers = maxConcurrent;
        overrideSpawnInterval = Mathf.Max(0.1f, interval);
    }

    /// <summary>
    /// Restablece los valores del spawner a su configuración base de la escena.
    /// </summary>
    public void ClearUpgradeCustomerLimits()
    {
        overrideMaxConcurrentCustomers = null;
        overrideSpawnInterval = null;
    }

    /// <summary>
    /// Activa la generación de clientes.
    /// </summary>
    public void StartSpawning()
    {
        autoSpawn = true;
        spawnTimer = CurrentSpawnInterval;
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
            spawnTimer = CurrentSpawnInterval;
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

        // Buscar y retirar cualquier otro cliente existente en la escena
        Customer[] allCustomers = FindObjectsByType<Customer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Customer customer in allCustomers)
        {
            if (customer != null)
            {
                customer.ForceLeaveImmediately();
            }
        }

        Debug.Log("[CustomerSpawner] Se ha ordenado la retirada de todos los clientes activos.");
    }

    private void Start()
    {
        spawnTimer = CurrentSpawnInterval;
    }

    private void Update()
    {
        if (!autoSpawn) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = CurrentSpawnInterval;
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
