/// <summary>
/// Interfaz para el servicio de economía, saldo y transacciones de dinero.
/// </summary>
public interface IMoneyService
{
    int CurrentMoney { get; }
    void AddMoney(int amount);
    bool TrySpendMoney(int amount);
}
