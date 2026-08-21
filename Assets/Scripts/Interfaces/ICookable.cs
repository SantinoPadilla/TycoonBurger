using UnityEngine;

/// <summary>
/// Interfaz para cualquier ingrediente que pueda ser procesado o cocinado en estaciones como la plancha.
/// </summary>
public interface ICookable
{
    CookingState CurrentState { get; }
    IngredientSO Data { get; }
    ICarryable HoldableItem { get; }
    GameObject gameObject { get; }
    Transform transform { get; }

    bool Cook(float deltaTime);
    float GetTotalProgressNormalized();
    float CookedThresholdNormalized { get; }
}
