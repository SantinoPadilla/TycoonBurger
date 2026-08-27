using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gestor del Sistema de Reputación del juego.
/// Administra la cantidad de reputación, sus límites y los valores modificables en el Inspector
/// para clientes felices y clientes furiosos.
/// </summary>
public class ReputationManager : MonoBehaviour
{
    private static ReputationManager instance;
    public static ReputationManager Instance => instance;

    [Header("Configuración de Reputación")]
    [Tooltip("Valor máximo de reputación alcanzable.")]
    [SerializeField] private float maxReputation = 100f;

    [Tooltip("Valor inicial de reputación al comenzar el juego (por defecto en la mitad).")]
    [SerializeField] private float startingReputation = 50f;

    [Header("Puntos Modificables en el Inspector")]
    [Tooltip("Puntos de reputación que suma un cliente feliz al completar su pedido.")]
    [SerializeField] private float happyCustomerPoints = 10f;

    [Tooltip("Puntos de reputación que resta un cliente furioso al retirarse molesto.")]
    [SerializeField] private float angryCustomerPoints = 15f;

    [Header("Eventos")]
    [Tooltip("Evento transmitido cuando cambia la reputación (reputaciónActual, reputaciónMáxima).")]
    public UnityEvent<float, float> onReputationChanged;

    private float currentReputation;

    public float CurrentReputation => currentReputation;
    public float MaxReputation => maxReputation;
    public float NormalizedReputation => maxReputation > 0f ? Mathf.Clamp01(currentReputation / maxReputation) : 0f;

    public float HappyCustomerPoints
    {
        get => happyCustomerPoints;
        set => happyCustomerPoints = value;
    }

    public float AngryCustomerPoints
    {
        get => angryCustomerPoints;
        set => angryCustomerPoints = value;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Inicializar reputación justo en la mitad si no se configuró de forma distinta
        currentReputation = Mathf.Clamp(startingReputation, 0f, maxReputation);
    }

    private void Start()
    {
        NotifyReputationChanged();
    }

    /// <summary>
    /// Suma la cantidad de reputación configurada para clientes felices.
    /// </summary>
    public void AddHappyCustomerReputation()
    {
        AddReputation(happyCustomerPoints);
    }

    /// <summary>
    /// Resta la cantidad de reputación configurada para clientes furiosos.
    /// </summary>
    public void RemoveAngryCustomerReputation()
    {
        RemoveReputation(angryCustomerPoints);
    }

    /// <summary>
    /// Añade puntos a la reputación actual sin superar el máximo.
    /// </summary>
    public void AddReputation(float amount)
    {
        if (amount <= 0f) return;
        currentReputation = Mathf.Min(maxReputation, currentReputation + amount);
        NotifyReputationChanged();
        Debug.Log($"[ReputationManager] ¡Cliente Feliz! +{amount} reputación. Total: {currentReputation}/{maxReputation}");
    }

    /// <summary>
    /// Resta puntos a la reputación actual sin bajar de 0.
    /// </summary>
    public void RemoveReputation(float amount)
    {
        if (amount <= 0f) return;
        currentReputation = Mathf.Max(0f, currentReputation - amount);
        NotifyReputationChanged();
        Debug.Log($"[ReputationManager] ¡Cliente Furioso! -{amount} reputación. Total: {currentReputation}/{maxReputation}");
    }

    private void NotifyReputationChanged()
    {
        onReputationChanged?.Invoke(currentReputation, maxReputation);
    }
}
