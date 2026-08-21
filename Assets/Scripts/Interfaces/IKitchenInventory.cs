/// <summary>
/// Interfaz genérica para almacenes, despensas o inventarios de escena en la cocina.
/// </summary>
public interface IKitchenInventory
{
    bool HasIngredient(IngredientSO ingredient);
    bool TryConsumeIngredient(IngredientSO ingredient);
    void AddIngredient(IngredientSO ingredient, int amount = 1);
    int GetIngredientCount(IngredientSO ingredient);
}
