using UnityEngine;

/// <summary>
/// Interfaz para cualquier producto o bien comerciable que otorgue dinero al venderse.
/// </summary>
public interface ISellable
{
    int SellPrice { get; }
    ProductSO ProductData { get; }
    ICarryable HoldableItem { get; }
    GameObject gameObject { get; }
}
