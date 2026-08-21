using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estructura que define los requisitos de un ingrediente dentro de una receta.
/// </summary>
[System.Serializable]
public struct IngredientRequirement
{
    [Tooltip("Ingrediente requerido (ej. Hamburguesa, Papa, Soda, Pan).")]
    public IngredientSO ingredient;

    [Tooltip("Estado de cocción necesario (ej. Cooked para carne/papa, Raw para pan/soda).")]
    public CookingState requiredState;

    [Tooltip("Cantidad requerida de este ingrediente.")]
    public int count;
}

/// <summary>
/// ScriptableObject genérico que define cualquier producto final o receta (Hamburguesa Completa, Potato Fries, Soda, etc.).
/// </summary>
[CreateAssetMenu(fileName = "NewProduct", menuName = "Kitchen/Product Recipe", order = 2)]
public class ProductSO : ScriptableObject
{
    [Header("Información del Producto")]
    [SerializeField] private string productName = "Producto Comercial";
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject resultPrefab;
    [SerializeField] private int sellPrice = 15;

    [Header("Configuración de Armado")]
    [Tooltip("Si requiere proceso de ensamblado en la Mesa de Armado.")]
    [SerializeField] private bool requiresAssembly = true;
    [SerializeField] private float assemblyTime = 3f;

    [Header("Receta / Lista Genérica de Ingredientes Requeridos")]
    [SerializeField] private List<IngredientRequirement> requiredIngredients = new List<IngredientRequirement>();

    public string ProductName => (!string.IsNullOrWhiteSpace(productName) && !productName.Equals("Producto Comercial", System.StringComparison.OrdinalIgnoreCase)) ? productName : name;
    public Sprite Icon => icon;
    public GameObject ResultPrefab => resultPrefab;
    public int SellPrice => sellPrice;
    public bool RequiresAssembly => requiresAssembly;
    public float AssemblyTime => assemblyTime;
    public List<IngredientRequirement> RequiredIngredients => requiredIngredients;

    // Propiedades de ayuda y compatibilidad
    public IngredientSO RequiredPattyIngredient => (requiredIngredients != null && requiredIngredients.Count > 0) ? requiredIngredients[0].ingredient : null;
    public CookingState RequiredPattyState => (requiredIngredients != null && requiredIngredients.Count > 0) ? requiredIngredients[0].requiredState : CookingState.Cooked;
    public IngredientSO RequiredBunIngredient => (requiredIngredients != null && requiredIngredients.Count > 1) ? requiredIngredients[1].ingredient : null;
}
