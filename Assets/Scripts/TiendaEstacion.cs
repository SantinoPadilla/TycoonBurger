using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estructura de configuración para cada artículo en venta dentro de la tienda.
/// Permite especificar un IngredientSO y opcionalmente sobreescribir su precio unitario.
/// </summary>
[System.Serializable]
public struct ShopItemConfig
{
    [Tooltip("ScriptableObject del ingrediente a vender.")]
    public IngredientSO ingredient;

    [Tooltip("Precio personalizado para esta estación (Si es <= 0, se usará el BuyPrice del IngredientSO).")]
    public int customPrice;

    public int EffectivePrice => (customPrice > 0) ? customPrice : (ingredient != null ? ingredient.BuyPrice : 5);
}

/// <summary>
/// Estación interactuable de Tienda de Ingredientes.
/// Al interactuar con el jugador, abre la interfaz de usuario de la tienda (ShopUI).
/// </summary>
public class TiendaEstacion : MonoBehaviour, IInteractable
{
    [Header("Configuración de la Estación")]
    [SerializeField] private string stationName = "Tienda de Ingredientes";
    
    [Tooltip("Lista de ingredientes a la venta en esta tienda.")]
    [SerializeField] private List<ShopItemConfig> itemsForSale = new List<ShopItemConfig>();

    [Header("Referencias UI")]
    [Tooltip("Referencia opcional a ShopUI. Si se deja vacía, se buscará automáticamente en la escena (incluyendo objetos inactivos).")]
    [SerializeField] private ShopUI shopUI;

    public string StationName => stationName;
    public List<ShopItemConfig> ItemsForSale => itemsForSale;

    private void Awake()
    {
        // Buscar ShopUI incluyendo objetos inactivos por si el Canvas/Panel empieza apagado
        if (shopUI == null)
        {
            shopUI = FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
        }

        // Verificación y advertencia útil en consola si falta el Collider2D
        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogWarning($"[{gameObject.name}] ¡ATENCIÓN! No se encontró un Collider2D en este GameObject. Añade un BoxCollider2D (con Is Trigger activado) para que el jugador pueda interactuar.");
        }
    }

    public void Interact()
    {
        if (shopUI == null)
        {
            shopUI = FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
        }

        if (shopUI != null)
        {
            Debug.Log($"[{gameObject.name}] ¡Interacción detectada! Abriendo la tienda '{stationName}'...");
            shopUI.ToggleShop(itemsForSale, this);
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] No se encontró ninguna instancia de ShopUI en la escena. Asegúrate de agregar el script ShopUI en tu Canvas o Panel de Tienda.");
        }
    }

    public string GetInteractPrompt()
    {
        return $"Abrir {stationName}";
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 1f, 0f));
    }
}
