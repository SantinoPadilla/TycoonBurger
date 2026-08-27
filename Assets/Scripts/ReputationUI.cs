using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente de UI para la barra de reputación.
/// Calcula por código puro la transición de color de Rojo (0%) a Amarillo (50%) y Verde (100%)
/// según el porcentaje de reputación actual sin depender del Inspector.
/// </summary>
public class ReputationUI : MonoBehaviour
{
    [Header("Referencias de UI")]
    [Tooltip("Slider opcional para la barra de reputación.")]
    [SerializeField] private Slider reputationSlider;

    [Tooltip("Imagen del relleno de la barra (Fill) a la que se aplicará el color.")]
    [SerializeField] private Image fillImage;

    [Tooltip("Texto opcional TMP para mostrar el porcentaje o valor de reputación.")]
    [SerializeField] private TextMeshProUGUI tmpReputationText;

    [Tooltip("Texto opcional Legacy de Unity UI.")]
    [SerializeField] private Text uiReputationText;

    [Header("Configuración de Texto")]
    [Tooltip("Prefijo del texto que se muestra en la barra de reputación (ej. 'Reputación: ', 'Rep: ', etc.).")]
    [SerializeField] private string textPrefix = "Reputación: ";

    [Tooltip("Sufijo opcional del texto (por defecto '%').")]
    [SerializeField] private string textSuffix = "%";

    [Tooltip("Si es verdadero, muestra la reputación como 'actual/máximo' en lugar de porcentaje.")]
    [SerializeField] private bool showRawValues = false;

    // Colores definidos estrictamente por código (no modificables en el Inspector)
    private static readonly Color RedColor = new Color(0.95f, 0.25f, 0.25f, 1f);    // Rojo (0% - Malo)
    private static readonly Color YellowColor = new Color(1.0f, 0.85f, 0.1f, 1f);   // Amarillo (50% - Medio)
    private static readonly Color GreenColor = new Color(0.25f, 0.85f, 0.35f, 1f);  // Verde (100% - Bueno)

    private void Awake()
    {
        // Auto-detectar Slider o FillImage si no están asignados en el Inspector
        if (reputationSlider == null)
        {
            reputationSlider = GetComponent<Slider>();
        }

        if (fillImage == null && reputationSlider != null && reputationSlider.fillRect != null)
        {
            fillImage = reputationSlider.fillRect.GetComponent<Image>();
        }
    }

    private void Start()
    {
        SubscribeToReputationManager();
        RefreshUI();
    }

    private void SubscribeToReputationManager()
    {
        if (ReputationManager.Instance != null)
        {
            ReputationManager.Instance.onReputationChanged.AddListener(UpdateReputationUI);
            UpdateReputationUI(ReputationManager.Instance.CurrentReputation, ReputationManager.Instance.MaxReputation);
        }
        else
        {
            Invoke(nameof(SubscribeToReputationManager), 0.1f);
        }
    }

    private void OnDestroy()
    {
        if (ReputationManager.Instance != null)
        {
            ReputationManager.Instance.onReputationChanged.RemoveListener(UpdateReputationUI);
        }
    }

    /// <summary>
    /// Actualiza la representación visual de la barra de reputación y sus colores.
    /// </summary>
    public void UpdateReputationUI(float current, float max)
    {
        float normalized = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        if (reputationSlider != null)
        {
            reputationSlider.minValue = 0f;
            reputationSlider.maxValue = 1f;
            reputationSlider.value = normalized;
        }

        // Si la imagen de relleno no fue asignada, intentar auto-detectarla desde el FillRect del Slider
        if (fillImage == null && reputationSlider != null && reputationSlider.fillRect != null)
        {
            fillImage = reputationSlider.fillRect.GetComponent<Image>();
        }

        Color targetColor = GetReputationColor(normalized);

        if (fillImage != null)
        {
            fillImage.fillAmount = normalized;
            fillImage.color = targetColor;
        }

        string valueText = showRawValues 
            ? $"{Mathf.RoundToInt(current)}/{Mathf.RoundToInt(max)}" 
            : $"{Mathf.RoundToInt(normalized * 100f)}{textSuffix}";
            
        string fullText = $"{textPrefix}{valueText}";

        if (tmpReputationText != null) tmpReputationText.text = fullText;
        if (uiReputationText != null) uiReputationText.text = fullText;
    }

    private void RefreshUI()
    {
        if (ReputationManager.Instance != null)
        {
            UpdateReputationUI(ReputationManager.Instance.CurrentReputation, ReputationManager.Instance.MaxReputation);
        }
    }

    /// <summary>
    /// Calcula por código el color correspondiente al porcentaje dado:
    /// - 0% a 50%: Transición de Rojo a Amarillo.
    /// - 50% a 100%: Transición de Amarillo a Verde.
    /// </summary>
    private Color GetReputationColor(float normalized)
    {
        normalized = Mathf.Clamp01(normalized);

        if (normalized <= 0.5f)
        {
            // Mapear [0.0, 0.5] a [0.0, 1.0] para interpolar entre Rojo y Amarillo
            float t = normalized / 0.5f;
            return Color.Lerp(RedColor, YellowColor, t);
        }
        else
        {
            // Mapear [0.5, 1.0] a [0.0, 1.0] para interpolar entre Amarillo y Verde
            float t = (normalized - 0.5f) / 0.5f;
            return Color.Lerp(YellowColor, GreenColor, t);
        }
    }
}
