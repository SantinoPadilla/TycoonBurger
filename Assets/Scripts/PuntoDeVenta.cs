using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Estación Punto de Venta refactorizada (cumple SOLID).
/// Opera a través de ISellable, ICarrier y IMoneyService.
/// </summary>
public class PuntoDeVenta : MonoBehaviour, IInteractable
{
    [Header("Configuración del Punto de Venta")]
    [Tooltip("Precio por defecto si el objeto vendido no implementa ISellable.")]
    [SerializeField] private int defaultItemPrice = 10;

    [Header("Eventos")]
    public UnityEvent<int> onProductSold;

    public void Interact()
    {
        ICarrier carrier = FindFirstObjectByType<PlayerCarrySystem>();

        if (carrier == null || !carrier.HasItems)
        {
            Debug.Log("[PuntoDeVenta] Necesitas llevar un producto en las manos para venderlo.");
            return;
        }

        ICarryable itemInHand = carrier.GetCarriedItem();

        if (itemInHand != null)
        {
            int earnedMoney = defaultItemPrice;

            ISellable sellable = itemInHand.gameObject.GetComponent<ISellable>();
            if (sellable != null)
            {
                earnedMoney = sellable.SellPrice;
            }

            // Aplicar multiplicador de ganancias de la mejora de Profit si aplica
            earnedMoney = ProfitUpgradeItemUI.ApplyProfitMultiplier(earnedMoney);

            carrier.TakeCarriedItem();

            IMoneyService moneyService = FindFirstObjectByType<MoneyManager>();
            if (moneyService != null)
            {
                moneyService.AddMoney(earnedMoney);
            }
            else
            {
                Debug.LogWarning("[PuntoDeVenta] No se encontró un IMoneyService (MoneyManager) en la escena.");
            }

            // Registrar el producto vendido en el gestor de turnos
            string soldName = "";
            SellableProduct sellableProd = itemInHand.gameObject.GetComponent<SellableProduct>();
            HamburguesaCompleta burger = itemInHand.gameObject.GetComponent<HamburguesaCompleta>();
            if (sellableProd != null && sellableProd.ProductData != null) soldName = sellableProd.ProductData.ProductName;
            else if (burger != null && burger.ProductData != null) soldName = burger.ProductData.ProductName;
            else soldName = itemInHand.ItemName;

            RestaurantShiftManager shiftManager = RestaurantShiftManager.Instance != null ? RestaurantShiftManager.Instance : FindFirstObjectByType<RestaurantShiftManager>();
            if (shiftManager != null && !string.IsNullOrEmpty(soldName))
            {
                shiftManager.RegisterProductSold(soldName);
            }

            onProductSold?.Invoke(earnedMoney);
            Debug.Log($"[PuntoDeVenta] ¡Venta exitosa! Objeto '{itemInHand.ItemName}' ({soldName}) vendido por ${earnedMoney}.");

            Destroy(itemInHand.gameObject);
        }
    }

    public string GetInteractPrompt()
    {
        return "Vender Producto";
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
