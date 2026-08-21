using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente de UI para mostrar el tiempo restante del turno de servicio,
/// el estado actual (ABIERTO / CERRADO) y el número de servicio/día.
/// Soporta componentes TextMeshProUGUI y Text tradicionales de Unity.
/// </summary>
public class ShiftTimerUI : MonoBehaviour
{
    [Header("Referencias UI de Temporizador")]
    [SerializeField] private TextMeshProUGUI tmpTimerText;
    [SerializeField] private Text uiTimerText;
    [SerializeField] private Image timerFillImage;

    [Header("Referencias UI de Estado (Abierto / Cerrado)")]
    [SerializeField] private TextMeshProUGUI tmpStatusText;
    [SerializeField] private Text uiStatusText;

    [Header("Referencias UI de Turno / Día")]
    [SerializeField] private TextMeshProUGUI tmpShiftText;
    [SerializeField] private Text uiShiftText;

    [Header("Configuración de Colores y Formatos")]
    [SerializeField] private Color openColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Verde
    [SerializeField] private Color closedColor = new Color(0.9f, 0.2f, 0.2f, 1f); // Rojo
    [SerializeField] private string openStatusMessage = "ABIERTO";
    [SerializeField] private string closedStatusMessage = "CERRADO";
    [SerializeField] private string shiftPrefixMessage = "Servicio #";

    private void Start()
    {
        SubscribeToShiftManager();
        RefreshDisplay();
    }

    private void SubscribeToShiftManager()
    {
        if (RestaurantShiftManager.Instance != null)
        {
            RestaurantShiftManager.Instance.onShiftTimerUpdated.AddListener(UpdateTimerDisplay);
            RestaurantShiftManager.Instance.onStateChanged.AddListener(OnStateChanged);
            RestaurantShiftManager.Instance.onShiftStarted.AddListener(RefreshDisplay);
            RestaurantShiftManager.Instance.onShiftEnded.AddListener(RefreshDisplay);
        }
        else
        {
            Invoke(nameof(SubscribeToShiftManager), 0.1f);
        }
    }

    private void OnDestroy()
    {
        if (RestaurantShiftManager.Instance != null)
        {
            RestaurantShiftManager.Instance.onShiftTimerUpdated.RemoveListener(UpdateTimerDisplay);
            RestaurantShiftManager.Instance.onStateChanged.RemoveListener(OnStateChanged);
            RestaurantShiftManager.Instance.onShiftStarted.RemoveListener(RefreshDisplay);
            RestaurantShiftManager.Instance.onShiftEnded.RemoveListener(RefreshDisplay);
        }
    }

    private void UpdateTimerDisplay(float remainingSeconds, float totalDuration)
    {
        int minutes = Mathf.FloorToInt(remainingSeconds / 60f);
        int seconds = Mathf.FloorToInt(remainingSeconds % 60f);
        string formattedTime = $"{minutes:D2}:{seconds:D2}";

        SetText(tmpTimerText, uiTimerText, formattedTime);

        if (timerFillImage != null && totalDuration > 0f)
        {
            timerFillImage.fillAmount = Mathf.Clamp01(remainingSeconds / totalDuration);
        }
    }

    private void OnStateChanged(RestaurantShiftManager.RestaurantState newState)
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (RestaurantShiftManager.Instance == null) return;

        bool isOpen = RestaurantShiftManager.Instance.IsOpen;
        string statusText = isOpen ? openStatusMessage : closedStatusMessage;
        Color statusColor = isOpen ? openColor : closedColor;

        SetText(tmpStatusText, uiStatusText, statusText);
        SetTextColor(tmpStatusText, uiStatusText, statusColor);

        int shiftNumber = RestaurantShiftManager.Instance.CurrentShiftNumber;
        SetText(tmpShiftText, uiShiftText, $"{shiftPrefixMessage}{shiftNumber}");

        UpdateTimerDisplay(RestaurantShiftManager.Instance.RemainingTime, RestaurantShiftManager.Instance.ShiftDuration);
    }

    private void SetText(TextMeshProUGUI tmpText, Text uiText, string content)
    {
        if (tmpText != null) tmpText.text = content;
        if (uiText != null) uiText.text = content;
    }

    private void SetTextColor(TextMeshProUGUI tmpText, Text uiText, Color color)
    {
        if (tmpText != null) tmpText.color = color;
        if (uiText != null) uiText.color = color;
    }
}
