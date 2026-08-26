using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente de UI para el botón que permite al jugador cerrar el restaurante manualmente.
/// Al hacer clic, reduce el tiempo del turno a 0, expulsa los clientes, limpia los objetos de la cocina
/// y abre la tienda / panel de cerrado.
/// </summary>
public class CloseRestaurantButtonUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("Botón de la interfaz para cerrar el restaurante. Si no se asigna, se buscará en este GameObject.")]
    [SerializeField] private Button closeRestaurantButton;

    [Tooltip("Texto opcional del botón para actualizar su contenido si se desea.")]
    [SerializeField] private TextMeshProUGUI tmpButtonText;
    [SerializeField] private Text uiButtonText;

    [Header("Configuración de Visibilidad")]
    [Tooltip("Si es true, oculta el GameObject del botón cuando el restaurante está CERRADO y lo muestra cuando está ABIERTO.")]
    [SerializeField] private bool hideButtonWhenClosed = true;

    private void Awake()
    {
        if (closeRestaurantButton == null)
        {
            closeRestaurantButton = GetComponent<Button>();
        }

        if (closeRestaurantButton != null)
        {
            closeRestaurantButton.onClick.RemoveAllListeners();
            closeRestaurantButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }

    private void Start()
    {
        SubscribeToShiftManager();
        UpdateVisibility();
    }

    private void SubscribeToShiftManager()
    {
        if (RestaurantShiftManager.Instance != null)
        {
            RestaurantShiftManager.Instance.onShiftStarted.AddListener(UpdateVisibility);
            RestaurantShiftManager.Instance.onShiftEnded.AddListener(UpdateVisibility);
            RestaurantShiftManager.Instance.onStateChanged.AddListener(OnStateChanged);
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
            RestaurantShiftManager.Instance.onShiftStarted.RemoveListener(UpdateVisibility);
            RestaurantShiftManager.Instance.onShiftEnded.RemoveListener(UpdateVisibility);
            RestaurantShiftManager.Instance.onStateChanged.RemoveListener(OnStateChanged);
        }
    }

    private void OnStateChanged(RestaurantShiftManager.RestaurantState state)
    {
        UpdateVisibility();
    }

    /// <summary>
    /// Acción ejecutada al hacer clic en el botón de cerrar restaurante.
    /// </summary>
    public void OnCloseButtonClicked()
    {
        if (RestaurantShiftManager.Instance != null && RestaurantShiftManager.Instance.IsOpen)
        {
            Debug.Log("[CloseRestaurantButtonUI] Clic en botón 'Cerrar Restaurante'. Finalizando servicio manualmente...");
            RestaurantShiftManager.Instance.CloseRestaurant(triggerShop: true);
        }
    }

    /// <summary>
    /// Actualiza la visibilidad e interactividad del botón según si el restaurante está abierto o cerrado.
    /// </summary>
    public void UpdateVisibility()
    {
        if (RestaurantShiftManager.Instance == null) return;

        bool isOpen = RestaurantShiftManager.Instance.IsOpen;

        if (hideButtonWhenClosed)
        {
            gameObject.SetActive(isOpen);
        }
        else if (closeRestaurantButton != null)
        {
            closeRestaurantButton.interactable = isOpen;
        }
    }
}
