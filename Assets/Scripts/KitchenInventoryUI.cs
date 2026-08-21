using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador principal para la barra/panel de inventario del HUD.
/// Trabaja con casillas KitchenInventorySlotUI acomodadas manualmente en la escena,
/// manteniendo sincronizados los contadores de cada ingrediente en tiempo real.
/// </summary>
public class KitchenInventoryUI : MonoBehaviour
{
    [Header("Casillas Pre-colocadas en la Escena")]
    [Tooltip("Lista manual de casillas de inventario. Si se deja vacía, se buscarán automáticamente en los hijos de este GameObject o contenedor.")]
    [SerializeField] private List<KitchenInventorySlotUI> inventorySlots = new List<KitchenInventorySlotUI>();

    [Tooltip("Transform contenedor opcional donde se encuentran los slots.")]
    [SerializeField] private Transform slotsContainer;

    private void Start()
    {
        InitializeAndRefresh();
        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    /// <summary>
    /// Obtiene todas las casillas de inventario registradas en la lista o en los hijos.
    /// </summary>
    public List<KitchenInventorySlotUI> GetInventorySlots()
    {
        if (inventorySlots != null && inventorySlots.Count > 0)
        {
            return inventorySlots;
        }

        KitchenInventorySlotUI[] foundSlots = null;
        if (slotsContainer != null)
        {
            foundSlots = slotsContainer.GetComponentsInChildren<KitchenInventorySlotUI>(true);
        }
        else
        {
            foundSlots = GetComponentsInChildren<KitchenInventorySlotUI>(true);
        }

        if (foundSlots != null)
        {
            inventorySlots = new List<KitchenInventorySlotUI>(foundSlots);
        }

        return inventorySlots ?? new List<KitchenInventorySlotUI>();
    }

    /// <summary>
    /// Inicializa las casillas y actualiza todas las cantidades desde GlobalKitchenInventory.
    /// </summary>
    public void InitializeAndRefresh()
    {
        List<KitchenInventorySlotUI> slots = GetInventorySlots();
        IKitchenInventory inventory = GlobalKitchenInventory.Instance;

        foreach (var slot in slots)
        {
            if (slot == null) continue;

            slot.InitializeSlot();

            if (slot.CurrentIngredient != null)
            {
                int count = (inventory != null) ? inventory.GetIngredientCount(slot.CurrentIngredient) : 0;
                slot.UpdateDisplay(count);
            }
        }
    }

    /// <summary>
    /// Actualiza una casilla específica de ingrediente cuando cambia su stock.
    /// </summary>
    public void RefreshIngredient(IngredientSO ingredient, int newCount)
    {
        if (ingredient == null) return;

        List<KitchenInventorySlotUI> slots = GetInventorySlots();
        foreach (var slot in slots)
        {
            if (slot != null && slot.CurrentIngredient == ingredient)
            {
                slot.UpdateDisplay(newCount);
            }
        }
    }

    private void SubscribeEvents()
    {
        if (GlobalKitchenInventory.Instance != null)
        {
            GlobalKitchenInventory.Instance.onIngredientStockChanged.AddListener(OnStockChanged);
        }
    }

    private void UnsubscribeEvents()
    {
        if (GlobalKitchenInventory.Instance != null)
        {
            GlobalKitchenInventory.Instance.onIngredientStockChanged.RemoveListener(OnStockChanged);
        }
    }

    private void OnStockChanged(IngredientSO ingredient, int newCount)
    {
        RefreshIngredient(ingredient, newCount);
    }
}
