using UnityEngine;

/// <summary>
/// Interfaz para entidades o personajes capaces de cargar y transportar objetos ICarryable.
/// </summary>
public interface ICarrier
{
    int MaxCapacity { get; }
    int CurrentCarriedCount { get; }
    bool IsFull { get; }
    bool HasItems { get; }
    Transform transform { get; }

    bool CanCarryMore();
    bool PickUp(ICarryable item);
    ICarryable GetCarriedItem();
    ICarryable TakeCarriedItem();
    ICarryable DropItem();
    void DropAllItems();
}
