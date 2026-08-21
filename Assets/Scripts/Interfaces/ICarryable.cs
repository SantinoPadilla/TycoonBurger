using UnityEngine;

/// <summary>
/// Interfaz para objetos que pueden ser cargados, transportados y colocados por el jugador o estaciones.
/// </summary>
public interface ICarryable
{
    bool IsBeingCarried { get; }
    Vector3 OriginalScale { get; }
    string ItemName { get; }
    GameObject gameObject { get; }
    Transform transform { get; }

    void OnPickedUp(Transform holdPoint);
    void OnDropped(Vector3 dropPosition);
    void PlaceAtPoint(Transform targetPoint);
}
