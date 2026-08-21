using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente para cada casilla de ingrediente acomodada manualmente en la barra de inventario del HUD.
/// Muestra la imagen (sprite) del ingrediente y el número/cantidad disponible en tiempo real.
/// </summary>
public class KitchenInventorySlotUI : MonoBehaviour
{
    [Header("Configuración del Ingrediente en Escena")]
    [Tooltip("ScriptableObject del ingrediente asociado a esta casilla del HUD.")]
    [SerializeField] private IngredientSO ingredientSO;

    [Tooltip("Si es true, oculta el GameObject entero si la cantidad es 0.")]
    [SerializeField] private bool hideIfZero = false;

    [Tooltip("Prefijo para el texto de cantidad (ej: 'x' para 'x10' o dejar vacío para '10').")]
    [SerializeField] private string countPrefix = "x";

    [Header("Referencias Visuales UI")]
    [SerializeField] private Image iconImage;

    [Header("Textos (Soporta TMPro o Text genérico)")]
    [SerializeField] private TextMeshProUGUI tmpCountText;
    [SerializeField] private Text uiCountText;

    public IngredientSO CurrentIngredient => ingredientSO;

    private void Awake()
    {
        InitializeSlot();
    }

    private void OnValidate()
    {
        // En el editor de Unity, actualizar icono automáticamente cuando se cambia el IngredientSO
        if (ingredientSO != null && iconImage != null)
        {
            iconImage.sprite = ingredientSO.Icon;
            iconImage.enabled = (ingredientSO.Icon != null);
        }
    }

    /// <summary>
    /// Configura e inicializa la casilla con sus datos visuales del IngredientSO.
    /// </summary>
    public void InitializeSlot(IngredientSO customIngredient = null)
    {
        if (customIngredient != null)
        {
            ingredientSO = customIngredient;
        }

        if (iconImage != null && ingredientSO != null)
        {
            iconImage.sprite = ingredientSO.Icon;
            iconImage.enabled = (ingredientSO.Icon != null);
        }
    }

    /// <summary>
    /// Actualiza la cantidad en pantalla y la visibilidad de la casilla según corresponda.
    /// </summary>
    public void UpdateDisplay(int count)
    {
        if (hideIfZero)
        {
            gameObject.SetActive(count > 0);
        }

        string textStr = $"{countPrefix}{count}";
        if (tmpCountText != null) tmpCountText.text = textStr;
        if (uiCountText != null) uiCountText.text = textStr;
    }
}
