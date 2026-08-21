using UnityEngine;

/// <summary>
/// ScriptableObject genérico que define los datos de cualquier ingrediente (Hamburguesa, Papa, Soda, Pan, Queso, etc.).
/// Soporta ingredientes cocinables (papas, carne) e ingredientes no cocinables (soda, pan, aderezos).
/// </summary>
[CreateAssetMenu(fileName = "NewIngredient", menuName = "Kitchen/Ingredient", order = 1)]
public class IngredientSO : ScriptableObject
{
    [Header("Información del Ingrediente")]
    [SerializeField] private string ingredientName = "Ingrediente";
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject prefab;

    [Header("Propiedades de Cocción")]
    [Tooltip("Indica si este ingrediente requiere ser cocinado (ej. carne, papas) o si se usa directo (ej. soda, pan).")]
    [SerializeField] private bool isCookable = true;

    [Header("Tiempos de Cocción (Sólo si es cocinable)")]
    [SerializeField] private float timeToCook = 4f;
    [SerializeField] private float timeToBurn = 4f;

    [Header("Colores Visuales por Estado")]
    [SerializeField] private Color rawColor = new Color(0.9f, 0.4f, 0.4f);
    [SerializeField] private Color cookedColor = new Color(0.6f, 0.3f, 0.1f);
    [SerializeField] private Color burntColor = new Color(0.15f, 0.15f, 0.15f);

    [Header("Precio en Tienda")]
    [Tooltip("Precio por unidad de este ingrediente cuando se compra en la tienda.")]
    [SerializeField] private int buyPrice = 5;

    public string IngredientName => ingredientName;
    public Sprite Icon => icon;
    public GameObject Prefab => prefab;
    public int BuyPrice => buyPrice;
    public bool IsCookable => isCookable;
    public float TimeToCook => isCookable ? timeToCook : 0f;
    public float TimeToBurn => isCookable ? timeToBurn : 0f;
    public Color RawColor => rawColor;
    public Color CookedColor => isCookable ? cookedColor : rawColor;
    public Color BurntColor => isCookable ? burntColor : rawColor;
}
