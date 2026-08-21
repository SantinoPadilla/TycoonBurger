using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gestor del Sistema de Dinero acumulado en el juego.
/// Implementa IMoneyService.
/// </summary>
public class MoneyManager : MonoBehaviour, IMoneyService
{
    private static MoneyManager instance;
    public static MoneyManager Instance => instance;

    [Header("Configuración Inicial")]
    [SerializeField] private int startingMoney = 0;

    [Header("Eventos")]
    public UnityEvent<int> onMoneyChanged;

    private int currentMoney = 0;

    public int CurrentMoney => currentMoney;

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

        currentMoney = Mathf.Max(0, startingMoney);
    }

    private void Start()
    {
        onMoneyChanged?.Invoke(currentMoney);
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        currentMoney += amount;
        onMoneyChanged?.Invoke(currentMoney);
        Debug.Log($"[MoneyManager] ¡+${amount} ganados! Saldo total: ${currentMoney}");
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount <= 0) return true;

        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            onMoneyChanged?.Invoke(currentMoney);
            Debug.Log($"[MoneyManager] -${amount} gastados. Saldo restante: ${currentMoney}");
            return true;
        }

        Debug.Log($"[MoneyManager] Saldo insuficiente. Requerido: ${amount}, Actual: ${currentMoney}");
        return false;
    }
}
