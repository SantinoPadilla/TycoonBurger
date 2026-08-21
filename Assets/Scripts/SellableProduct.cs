using UnityEngine;

/// <summary>
/// Componente para definir el valor de venta de cualquier producto u objeto transportable.
/// Implementa ISellable e integra ProductSO.
/// </summary>
public class SellableProduct : MonoBehaviour, ISellable
{
    [Header("ScriptableObject de Producto (Opcional)")]
    [SerializeField] private ProductSO productData;

    [Header("Valor Comercial por Defecto")]
    [Tooltip("Precio en dinero que otorga este producto al venderlo si no se asignó un ProductSO.")]
    [SerializeField] private int sellPrice = 15;

    private ICarryable holdableItem;

    public ProductSO ProductData => productData;
    public int SellPrice => productData != null ? productData.SellPrice : sellPrice;
    public ICarryable HoldableItem => holdableItem ?? (holdableItem = GetComponent<ICarryable>());

    private void Awake()
    {
        holdableItem = GetComponent<ICarryable>();
    }
}
