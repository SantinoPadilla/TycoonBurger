using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Inventario de escena (Caja de Ingredientes / Almacén de Cocina) refactorizado.
/// Habita en un objeto del escenario y opera mediante las interfaces ICarrier e ICarryable.
/// </summary>
public class IngredientContainer : MonoBehaviour, IInteractable
{
    [Header("Configuración del Inventario")]
    [Tooltip("ScriptableObject del ingrediente almacenado.")]
    [SerializeField] private IngredientSO ingredientSO;

    [Tooltip("Prefab del ingrediente a instanciar si no se especificó un IngredientSO.")]
    [SerializeField] private GameObject ingredientPrefab;

    [Tooltip("Cantidad inicial de ingredientes asignados en el Inspector.")]
    [SerializeField] private int initialCount = 10;

    [Tooltip("Si es true, el contenedor nunca se agota de ingredientes.")]
    [SerializeField] private bool infiniteSupply = false;

    [Header("Eventos de Inventario")]
    public UnityEvent<int> onCountChanged;

    private int currentCount = 0;

    public int CurrentCount => currentCount;
    public bool HasIngredients => infiniteSupply || currentCount > 0;
    public GameObject EffectivePrefab => (ingredientSO != null && ingredientSO.Prefab != null) ? ingredientSO.Prefab : ingredientPrefab;

    private void Awake()
    {
        currentCount = Mathf.Max(0, initialCount);
        onCountChanged?.Invoke(currentCount);
    }

    public void AddIngredients(int amount = 1)
    {
        if (amount <= 0) return;
        currentCount += amount;
        onCountChanged?.Invoke(currentCount);
        Debug.Log($"[IngredientContainer] +{amount} ingrediente(s) añadido(s). Total en caja: {currentCount}");
    }

    public bool TryRemoveIngredient()
    {
        if (!HasIngredients)
        {
            Debug.Log("[IngredientContainer] El inventario está vacío.");
            return false;
        }

        if (!infiniteSupply)
        {
            currentCount--;
            onCountChanged?.Invoke(currentCount);
        }

        Debug.Log($"[IngredientContainer] -1 ingrediente retirado. Restantes en caja: {(infiniteSupply ? "Infinito" : currentCount.ToString())}");
        return true;
    }

    public void Interact()
    {
        ICarrier carrier = FindFirstObjectByType<PlayerCarrySystem>();

        if (carrier == null) return;

        ICarryable itemInHand = carrier.GetCarriedItem();

        if (itemInHand != null)
        {
            carrier.TakeCarriedItem();
            Destroy(itemInHand.gameObject);
            AddIngredients(1);
        }
        else
        {
            if (carrier.CanCarryMore())
            {
                if (HasIngredients)
                {
                    GameObject prefabToSpawn = EffectivePrefab;
                    if (prefabToSpawn != null)
                    {
                        if (TryRemoveIngredient())
                        {
                            GameObject spawnedObj = Instantiate(prefabToSpawn, transform.position, Quaternion.identity);
                            ICarryable spawnedCarryable = spawnedObj.GetComponent<ICarryable>();

                            if (spawnedCarryable != null)
                            {
                                carrier.PickUp(spawnedCarryable);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("[IngredientContainer] ¡No has asignado ni IngredientSO ni Prefab en el Inspector!");
                    }
                }
                else
                {
                    Debug.Log("[IngredientContainer] ¡No quedan ingredientes en este inventario!");
                }
            }
            else
            {
                Debug.Log("[IngredientContainer] Las manos del jugador están llenas.");
            }
        }
    }

    public string GetInteractPrompt()
    {
        string countText = infiniteSupply ? "Infinito" : currentCount.ToString();
        string nameText = ingredientSO != null ? ingredientSO.IngredientName : "Ingredientes";
        return $"Caja de {nameText} ({countText})";
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(0.8f, 0.8f, 0f));
    }
}
