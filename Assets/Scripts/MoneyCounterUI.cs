using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente para mostrar el saldo de dinero acumulado en la esquina de la pantalla.
/// Compatible tanto con TextMeshProUGUI como con el componente Text tradicional de Unity UI.
/// </summary>
public class MoneyCounterUI : MonoBehaviour
{
    [Header("Referencias UI (Asignar una de las dos)")]
    [SerializeField] private TextMeshProUGUI tmpMoneyText;
    [SerializeField] private Text uiMoneyText;

    [Header("Formato de Texto")]
    [SerializeField] private string prefix = "$ ";
    [SerializeField] private string suffix = "";

    private void Start()
    {
        // Suscribirse a los cambios de dinero
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.onMoneyChanged.AddListener(UpdateMoneyDisplay);
            UpdateMoneyDisplay(MoneyManager.Instance.CurrentMoney);
        }
        else
        {
            // Reintento en el primer frame por si MoneyManager se inicializa después
            Invoke(nameof(SubscribeToMoneyManager), 0.1f);
        }
    }

    private void SubscribeToMoneyManager()
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.onMoneyChanged.AddListener(UpdateMoneyDisplay);
            UpdateMoneyDisplay(MoneyManager.Instance.CurrentMoney);
        }
    }

    private void OnDestroy()
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.onMoneyChanged.RemoveListener(UpdateMoneyDisplay);
        }
    }

    /// <summary>
    /// Actualiza el texto visual en pantalla con la cantidad de dinero actual.
    /// </summary>
    public void UpdateMoneyDisplay(int currentMoney)
    {
        string displayText = $"{prefix}{currentMoney}{suffix}";

        if (tmpMoneyText != null)
        {
            tmpMoneyText.text = displayText;
        }
        else if (uiMoneyText != null)
        {
            uiMoneyText.text = displayText;
        }
    }
}
