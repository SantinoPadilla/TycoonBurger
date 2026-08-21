using UnityEngine;

/// <summary>
/// Componente para el producto final ensamblado (Pan + Carne Cocinada).
/// Integra ProductSO e implementa ISellable.
/// </summary>
[RequireComponent(typeof(HoldableItem))]
[RequireComponent(typeof(SellableProduct))]
public class HamburguesaCompleta : MonoBehaviour, ISellable
{
    [Header("Datos del Producto (ScriptableObject)")]
    [SerializeField] private ProductSO productData;

    [Header("Configuración por Defecto")]
    [SerializeField] private string burgerName = "Hamburguesa Completa";

    private ICarryable holdableItem;
    private SellableProduct sellableProduct;

    public ProductSO ProductData => productData;
    public int SellPrice => sellableProduct != null ? sellableProduct.SellPrice : (productData != null ? productData.SellPrice : 15);
    public ICarryable HoldableItem => holdableItem ?? (holdableItem = GetComponent<ICarryable>());
    public string BurgerName => productData != null ? productData.ProductName : burgerName;

    private void Awake()
    {
        holdableItem = GetComponent<ICarryable>();
        sellableProduct = GetComponent<SellableProduct>();
    }
}
