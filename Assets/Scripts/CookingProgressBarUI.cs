using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Componente de interfaz de usuario para mostrar la barra de progreso de cocción sobre la plancha.
/// </summary>
public class CookingProgressBarUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Image fillImage;
    [SerializeField] private GameObject barCanvasGroup;

    [Header("Colores de Progreso")]
    [SerializeField] private Color rawCookingColor = new Color(1f, 0.7f, 0.2f); // Amarillo / Naranja (Cocinando)
    [SerializeField] private Color cookedColor = new Color(0.2f, 0.8f, 0.2f);   // Verde (¡Listo/Hecho!)
    [SerializeField] private Color burntColor = new Color(0.9f, 0.2f, 0.2f);    // Rojo/Oscuro (Quemándose)

    private void Awake()
    {
        Hide();
    }

    /// <summary>
    /// Muestra la barra y actualiza el progreso en un único ciclo continuo (0 a 1) con transición suave de color.
    /// </summary>
    public void UpdateProgress(float totalFillNormalized, float cookedThreshold = 0.5f)
    {
        Show();

        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(totalFillNormalized);

            // Transición continua de color durante el único ciclo de la barra:
            if (totalFillNormalized < cookedThreshold)
            {
                // De 0% hasta el umbral (Crudo -> Hecho): Transición de Amarillo/Naranja a Verde
                float t = cookedThreshold > 0 ? (totalFillNormalized / cookedThreshold) : 1f;
                fillImage.color = Color.Lerp(rawCookingColor, cookedColor, t);
            }
            else
            {
                // Del umbral hasta 100% (Hecho -> Quemado): Transición de Verde a Rojo/Negro
                float t = (1f - cookedThreshold) > 0 ? ((totalFillNormalized - cookedThreshold) / (1f - cookedThreshold)) : 1f;
                fillImage.color = Color.Lerp(cookedColor, burntColor, t);
            }
        }
    }

    public void Show()
    {
        if (barCanvasGroup != null) barCanvasGroup.SetActive(true);
        else gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (barCanvasGroup != null) barCanvasGroup.SetActive(false);
        else gameObject.SetActive(false);
    }
}
