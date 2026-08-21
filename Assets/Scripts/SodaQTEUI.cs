using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Componente de interfaz de usuario para el minijuego Quick Time Event (QTE) de la SodaStacion.
/// Maneja un indicador que rebota de lado a lado en una barra y se desacelera progresivamente.
/// El jugador debe interactuar cuando el indicador esté en el centro verde.
/// </summary>
public class SodaQTEUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("Objeto contenedor o CanvasGroup que engloba la barra QTE para mostrar/ocultar.")]
    [SerializeField] private GameObject qteContainer;

    [Tooltip("RectTransform del contenedor principal de la barra (fondo).")]
    [SerializeField] private RectTransform barBackground;

    [Tooltip("RectTransform de la zona objetivo central (verde).")]
    [SerializeField] private RectTransform greenCenterZone;

    [Tooltip("RectTransform del indicador o línea horizontal que rebota.")]
    [SerializeField] private RectTransform indicatorLine;

    [Header("Parámetros de Velocidad y Desaceleración")]
    [Tooltip("Velocidad inicial del indicador (en porcentaje del ancho de barra por segundo, ej. 2.0 = recorre la barra 2 veces por segundo).")]
    [SerializeField] private float initialSpeed = 2.5f;

    [Tooltip("Tasa de desaceleración por segundo (cuánto se frena por segundo).")]
    [SerializeField] private float decelerationRate = 0.8f;

    [Tooltip("Velocidad mínima a la que se puede ralentizar el indicador.")]
    [SerializeField] private float minSpeed = 0.4f;

    [Header("Configuración de Zona Verde")]
    [Tooltip("Proporción del ancho de la barra ocupado por el centro verde (ej. 0.2 = 20% del ancho).")]
    [Range(0.05f, 0.6f)]
    [SerializeField] private float greenCenterRatio = 0.2f;

    private float currentSpeed;
    private float normalizedPos = 0f; // 0 = extremo izquierdo, 1 = extremo derecho
    private bool movingRight = true;
    private bool isQTEActive = false;

    public bool IsQTEActive => isQTEActive;

    private void Awake()
    {
        Hide();
    }

    private void Update()
    {
        if (!isQTEActive) return;

        // 1. Proceso de ralentización progresiva
        currentSpeed = Mathf.Max(minSpeed, currentSpeed - decelerationRate * Time.deltaTime);

        // 2. Movimiento del indicador
        if (movingRight)
        {
            normalizedPos += currentSpeed * Time.deltaTime;
            if (normalizedPos >= 1f)
            {
                normalizedPos = 1f;
                movingRight = false;
            }
        }
        else
        {
            normalizedPos -= currentSpeed * Time.deltaTime;
            if (normalizedPos <= 0f)
            {
                normalizedPos = 0f;
                movingRight = true;
            }
        }

        // 3. Actualizar la posición visual del indicador
        UpdateIndicatorPosition();
    }

    /// <summary>
    /// Inicia el minijuego QTE reseteando velocidad, posición y mostrando la interfaz.
    /// </summary>
    public void StartQTE()
    {
        currentSpeed = initialSpeed;
        normalizedPos = 0f; // Comienza en el extremo izquierdo
        movingRight = true;
        isQTEActive = true;

        SetupGreenZoneSize();
        Show();
        UpdateIndicatorPosition();
    }

    /// <summary>
    /// Inicia el QTE permitiendo personalizar parámetros al vuelo desde la estación.
    /// </summary>
    public void StartQTE(float customInitialSpeed, float customDeceleration, float customMinSpeed)
    {
        initialSpeed = customInitialSpeed;
        decelerationRate = customDeceleration;
        minSpeed = customMinSpeed;
        StartQTE();
    }

    /// <summary>
    /// Detiene y oculta el minijuego QTE.
    /// </summary>
    public void StopQTE()
    {
        isQTEActive = false;
        Hide();
    }

    /// <summary>
    /// Comprueba si el indicador horizontal se encuentra actualmente dentro del centro verde.
    /// </summary>
    /// <returns>True si la interacción ocurrió en el centro verde; False en caso contrario.</returns>
    public bool IsIndicatorInGreenZone()
    {
        if (greenCenterZone != null && indicatorLine != null && barBackground != null)
        {
            // Verificación basada en la posición local dentro de la barra
            float barWidth = barBackground.rect.width;
            if (barWidth > 0f)
            {
                float greenZoneWidth = barWidth * greenCenterRatio;
                float halfGreenWidth = greenZoneWidth * 0.5f;

                // Centro de la barra en coordenadas normalizadas es 0.5
                float minValidPos = 0.5f - (greenCenterRatio * 0.5f);
                float maxValidPos = 0.5f + (greenCenterRatio * 0.5f);

                return normalizedPos >= minValidPos && normalizedPos <= maxValidPos;
            }
        }

        // Fallback por defecto si no están asignadas las referencias UI
        float fallbackHalf = greenCenterRatio * 0.5f;
        return (normalizedPos >= 0.5f - fallbackHalf) && (normalizedPos <= 0.5f + fallbackHalf);
    }

    private void SetupGreenZoneSize()
    {
        if (barBackground != null && greenCenterZone != null)
        {
            float barWidth = barBackground.rect.width;
            if (barWidth > 0f)
            {
                greenCenterZone.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barWidth * greenCenterRatio);
            }
        }
    }

    private void UpdateIndicatorPosition()
    {
        if (indicatorLine == null) return;

        if (barBackground != null)
        {
            float barWidth = barBackground.rect.width;
            // Si el pivot del barBackground está en el centro (0.5, 0.5):
            float minX = -barWidth * 0.5f;
            float maxX = barWidth * 0.5f;
            float targetX = Mathf.Lerp(minX, maxX, normalizedPos);

            Vector2 anchoredPos = indicatorLine.anchoredPosition;
            anchoredPos.x = targetX;
            indicatorLine.anchoredPosition = anchoredPos;
        }
        else
        {
            // Fallback usando anclajes
            indicatorLine.anchorMin = new Vector2(normalizedPos, indicatorLine.anchorMin.y);
            indicatorLine.anchorMax = new Vector2(normalizedPos, indicatorLine.anchorMax.y);
        }
    }

    public void Show()
    {
        if (qteContainer != null) qteContainer.SetActive(true);
        else gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (qteContainer != null) qteContainer.SetActive(false);
        else gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        if (barBackground != null && greenCenterZone != null)
        {
            SetupGreenZoneSize();
        }
    }
}
