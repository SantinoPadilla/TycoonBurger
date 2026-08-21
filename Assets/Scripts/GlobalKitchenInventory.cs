using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public struct IngredientStockItem
{
    public IngredientSO ingredient;
    public int initialCount;
}

/// <summary>
/// Inventario Global de Escena para la Cocina (Despensa / Almacén Central).
/// Implementa IKitchenInventory y administra stock basado en IngredientSO.
/// </summary>
public class GlobalKitchenInventory : MonoBehaviour, IKitchenInventory
{
    private static GlobalKitchenInventory instance;
    public static GlobalKitchenInventory Instance => instance;

    [Header("Configuración por ScriptableObject")]
    [Tooltip("Lista de stock de ingredientes iniciales en la despensa.")]
    [SerializeField] private List<IngredientStockItem> initialStock = new List<IngredientStockItem>();

    [Header("Fallbacks por Defecto (Hamburguesa, Pan, Papa, Soda)")]
    [SerializeField] private IngredientSO defaultBurgerSO;
    [SerializeField] private IngredientSO defaultBunSO;
    [SerializeField] private IngredientSO defaultPotatoSO;
    [SerializeField] private IngredientSO defaultSodaSO;
    [SerializeField] private int initialBurgerCount = 10;
    [SerializeField] private int initialBunCount = 10;
    [SerializeField] private int initialPotatoCount = 10;
    [SerializeField] private int initialSodaCount = 10;

    [Tooltip("Si es verdadero, los ingredientes son infinitos.")]
    [SerializeField] private bool infiniteSupply = false;

    [Header("Eventos")]
    public UnityEvent<IngredientSO, int> onIngredientStockChanged;
    public UnityEvent<int> onBurgerCountChanged;
    public UnityEvent<int> onBunCountChanged;
    public UnityEvent<int> onPotatoCountChanged;
    public UnityEvent<int> onSodaCountChanged;

    private Dictionary<IngredientSO, int> stockDictionary = new Dictionary<IngredientSO, int>();
    private int currentBurgerCount = 0;
    private int currentBunCount = 0;
    private int currentPotatoCount = 0;
    private int currentSodaCount = 0;

    public int CurrentBurgerCount => GetIngredientCount(defaultBurgerSO);
    public int CurrentBunCount => GetIngredientCount(defaultBunSO);
    public int CurrentPotatoCount => GetIngredientCount(defaultPotatoSO);
    public int CurrentSodaCount => GetIngredientCount(defaultSodaSO);
    public bool HasBurgers => HasIngredient(defaultBurgerSO);
    public bool HasBuns => HasIngredient(defaultBunSO);
    public bool HasPotatoes => HasIngredient(defaultPotatoSO);
    public bool HasSoda => HasIngredient(defaultSodaSO);

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

        // Cargar stock desde la lista de ScriptableObjects
        foreach (var item in initialStock)
        {
            if (item.ingredient != null)
            {
                stockDictionary[item.ingredient] = Mathf.Max(0, item.initialCount);
            }
        }

        // Cargar fallbacks por defecto si no estaban en el listado
        currentBurgerCount = Mathf.Max(0, initialBurgerCount);
        currentBunCount = Mathf.Max(0, initialBunCount);
        currentPotatoCount = Mathf.Max(0, initialPotatoCount);
        currentSodaCount = Mathf.Max(0, initialSodaCount);

        if (defaultBurgerSO != null && !stockDictionary.ContainsKey(defaultBurgerSO))
        {
            stockDictionary[defaultBurgerSO] = currentBurgerCount;
        }
        if (defaultBunSO != null && !stockDictionary.ContainsKey(defaultBunSO))
        {
            stockDictionary[defaultBunSO] = currentBunCount;
        }
        if (defaultPotatoSO != null && !stockDictionary.ContainsKey(defaultPotatoSO))
        {
            stockDictionary[defaultPotatoSO] = currentPotatoCount;
        }
        if (defaultSodaSO != null && !stockDictionary.ContainsKey(defaultSodaSO))
        {
            stockDictionary[defaultSodaSO] = currentSodaCount;
        }

        onBurgerCountChanged?.Invoke(CurrentBurgerCount);
        onBunCountChanged?.Invoke(CurrentBunCount);
        onPotatoCountChanged?.Invoke(CurrentPotatoCount);
        onSodaCountChanged?.Invoke(CurrentSodaCount);
    }

    public bool HasIngredient(IngredientSO ingredient)
    {
        if (infiniteSupply) return true;
        if (ingredient == null) return HasAnyDefaultStock();
        return stockDictionary.TryGetValue(ingredient, out int count) && count > 0;
    }

    public bool TryConsumeIngredient(IngredientSO ingredient)
    {
        if (!HasIngredient(ingredient))
        {
            Debug.Log($"[GlobalKitchenInventory] ¡No quedan ingredientes de tipo {(ingredient != null ? ingredient.IngredientName : "genérico")}!");
            return false;
        }

        if (!infiniteSupply && ingredient != null && stockDictionary.ContainsKey(ingredient))
        {
            stockDictionary[ingredient]--;
            NotifyChanges(ingredient);
        }

        return true;
    }

    public void AddIngredient(IngredientSO ingredient, int amount = 1)
    {
        if (amount <= 0 || ingredient == null) return;

        if (!stockDictionary.ContainsKey(ingredient))
        {
            stockDictionary[ingredient] = 0;
        }

        stockDictionary[ingredient] += amount;
        NotifyChanges(ingredient);
        Debug.Log($"[GlobalKitchenInventory] +{amount} '{ingredient.IngredientName}' añadido. Total: {stockDictionary[ingredient]}");
    }

    public int GetIngredientCount(IngredientSO ingredient)
    {
        if (infiniteSupply) return 999;
        if (ingredient != null && stockDictionary.TryGetValue(ingredient, out int count))
        {
            return count;
        }
        return 0;
    }

    /// <summary>
    /// Devuelve un diccionario copia de todos los ingredientes registrados y sus cantidades.
    /// </summary>
    public Dictionary<IngredientSO, int> GetAllStock()
    {
        return new Dictionary<IngredientSO, int>(stockDictionary);
    }


    // Métodos de compatibilidad directa para la plancha, freidora, mesa de armado y soda estacion
    public bool TryConsumeBurger() => TryConsumeIngredient(defaultBurgerSO);
    public bool TryConsumeBun() => TryConsumeIngredient(defaultBunSO);
    public bool TryConsumePotato() => TryConsumeIngredient(defaultPotatoSO);
    public bool TryConsumeSoda() => TryConsumeIngredient(defaultSodaSO);
    public void AddBurgers(int amount = 1) => AddIngredient(defaultBurgerSO, amount);
    public void AddBuns(int amount = 1) => AddIngredient(defaultBunSO, amount);
    public void AddPotatoes(int amount = 1) => AddIngredient(defaultPotatoSO, amount);
    public void AddSoda(int amount = 1) => AddIngredient(defaultSodaSO, amount);

    private bool HasAnyDefaultStock() => infiniteSupply || currentBurgerCount > 0 || currentBunCount > 0 || currentPotatoCount > 0 || currentSodaCount > 0;

    private void NotifyChanges(IngredientSO ingredient)
    {
        if (ingredient == null) return;

        int newCount = GetIngredientCount(ingredient);
        onIngredientStockChanged?.Invoke(ingredient, newCount);

        if (ingredient == defaultBurgerSO)
        {
            onBurgerCountChanged?.Invoke(newCount);
        }
        else if (ingredient == defaultBunSO)
        {
            onBunCountChanged?.Invoke(newCount);
        }
        else if (ingredient == defaultPotatoSO)
        {
            onPotatoCountChanged?.Invoke(newCount);
        }
        else if (ingredient == defaultSodaSO)
        {
            onSodaCountChanged?.Invoke(newCount);
        }
    }
}
