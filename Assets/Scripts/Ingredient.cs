using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Componente genérico para cualquier ingrediente en el juego (Hamburguesa, Papa, Soda, Pan, etc.).
/// Obtiene todos sus datos (tiempos de cocción, colores por estado, icono) directamente del ScriptableObject IngredientSO.
/// </summary>
[RequireComponent(typeof(HoldableItem))]
public class Ingredient : MonoBehaviour, ICookable
{
    [Header("Datos del Ingrediente (ScriptableObject)")]
    [SerializeField] private IngredientSO data;

    [Header("Eventos")]
    public UnityEvent<CookingState> onStateChanged;

    private CookingState currentState = CookingState.Raw;
    private float currentCookTimer = 0f;
    private SpriteRenderer spriteRenderer;
    private ICarryable holdableItem;

    public CookingState CurrentState => currentState;
    public IngredientSO Data => data;
    public ICarryable HoldableItem => holdableItem ?? (holdableItem = GetComponent<HoldableItem>());

    public float EffectiveTimeToCook => data != null ? data.TimeToCook : 4f;
    public float EffectiveTimeToBurn => data != null ? data.TimeToBurn : 4f;
    public Color EffectiveRawColor => data != null ? data.RawColor : Color.white;
    public Color EffectiveCookedColor => data != null ? data.CookedColor : Color.white;
    public Color EffectiveBurntColor => data != null ? data.BurntColor : Color.black;

    public float TotalCookingTime => EffectiveTimeToCook + EffectiveTimeToBurn;
    public float CookedThresholdNormalized => TotalCookingTime > 0 ? (EffectiveTimeToCook / TotalCookingTime) : 0.5f;

    private void Awake()
    {
        holdableItem = GetComponent<HoldableItem>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisuals();
    }

    public bool Cook(float deltaTime)
    {
        // Si el ingrediente NO es cocinable (ej. Soda, Pan), no avanza el tiempo de cocción
        if (data != null && !data.IsCookable) return false;
        if (currentState == CookingState.Burnt) return false;

        currentCookTimer += deltaTime;

        float totalTime = TotalCookingTime;

        if (currentCookTimer >= totalTime)
        {
            currentCookTimer = totalTime;
            SetState(CookingState.Burnt);
        }
        else if (currentCookTimer >= EffectiveTimeToCook)
        {
            SetState(CookingState.Cooked);
        }
        else
        {
            SetState(CookingState.Raw);
        }

        return currentState != CookingState.Burnt;
    }

    public float GetTotalProgressNormalized()
    {
        if (data != null && !data.IsCookable) return 1f;
        float totalTime = TotalCookingTime;
        if (totalTime <= 0) return 0f;
        return Mathf.Clamp01(currentCookTimer / totalTime);
    }

    private void SetState(CookingState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        UpdateVisuals();
        onStateChanged?.Invoke(currentState);
        Debug.Log($"[{gameObject.name}] Ingrediente cambió de estado a: {currentState}");
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            switch (currentState)
            {
                case CookingState.Raw:
                    spriteRenderer.color = EffectiveRawColor;
                    break;
                case CookingState.Cooked:
                    spriteRenderer.color = EffectiveCookedColor;
                    break;
                case CookingState.Burnt:
                    spriteRenderer.color = EffectiveBurntColor;
                    break;
            }
        }
    }
}
