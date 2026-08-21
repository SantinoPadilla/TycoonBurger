using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gestor principal del sistema de mejoras en el juego (Singleton).
/// Administra el nivel alcanzado en cada mejora y procesa las compras descontando dinero a través de MoneyManager.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    private static UpgradeManager instance;
    public static UpgradeManager Instance => instance;

    [Header("Eventos")]
    public UnityEvent<string, int> onUpgradeLevelChanged; // (upgradeId, newLevel)

    // Diccionario de niveles alcanzados: ID Mejora -> Nivel Actual (0 = No comprada)
    private Dictionary<string, int> purchasedLevels = new Dictionary<string, int>();

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
    }

    /// <summary>
    /// Obtiene el nivel actual alcanzado para una mejora (0 = No comprada/Bloqueada).
    /// </summary>
    public int GetUpgradeLevel(string upgradeId)
    {
        if (string.IsNullOrWhiteSpace(upgradeId)) return 0;
        return purchasedLevels.TryGetValue(upgradeId.Trim().ToLower(), out int lvl) ? lvl : 0;
    }

    /// <summary>
    /// Verifica si la mejora se encuentra en su nivel máximo.
    /// </summary>
    public bool IsMaxLevel(UpgradeDataSO upgradeData)
    {
        if (upgradeData == null) return true;
        int currentLvl = GetUpgradeLevel(upgradeData.UpgradeId);
        return currentLvl >= upgradeData.MaxLevel;
    }

    /// <summary>
    /// Verifica si el jugador puede costear el siguiente nivel de la mejora.
    /// </summary>
    public bool CanAffordNextLevel(UpgradeDataSO upgradeData)
    {
        if (upgradeData == null || IsMaxLevel(upgradeData)) return false;

        int nextLevel = GetUpgradeLevel(upgradeData.UpgradeId) + 1;
        UpgradeLevelConfig nextConfig = upgradeData.GetLevelConfig(nextLevel);

        IMoneyService moneyService = FindFirstObjectByType<MoneyManager>();
        return moneyService != null && moneyService.CurrentMoney >= nextConfig.price;
    }

    /// <summary>
    /// Intenta comprar el siguiente nivel disponible para la mejora indicada.
    /// </summary>
    public bool TryBuyNextLevel(UpgradeDataSO upgradeData)
    {
        if (upgradeData == null) return false;
        if (IsMaxLevel(upgradeData))
        {
            Debug.Log($"[UpgradeManager] La mejora '{upgradeData.UpgradeName}' ya alcanzó su nivel máximo ({upgradeData.MaxLevel}).");
            return false;
        }

        int currentLvl = GetUpgradeLevel(upgradeData.UpgradeId);
        int targetLvl = currentLvl + 1;
        UpgradeLevelConfig targetConfig = upgradeData.GetLevelConfig(targetLvl);

        IMoneyService moneyService = FindFirstObjectByType<MoneyManager>();
        if (moneyService == null)
        {
            Debug.LogWarning("[UpgradeManager] No se encontró MoneyManager en la escena para procesar la compra.");
            return false;
        }

        if (moneyService.TrySpendMoney(targetConfig.price))
        {
            string cleanId = upgradeData.UpgradeId.Trim().ToLower();
            purchasedLevels[cleanId] = targetLvl;

            Debug.Log($"[UpgradeManager] ¡Mejora comprada! '{upgradeData.UpgradeName}' subió a Nivel {targetLvl} por ${targetConfig.price}.");
            onUpgradeLevelChanged?.Invoke(cleanId, targetLvl);
            return true;
        }
        else
        {
            Debug.Log($"[UpgradeManager] Saldo insuficiente para comprar Nivel {targetLvl} de '{upgradeData.UpgradeName}' (${targetConfig.price}).");
            return false;
        }
    }

    /// <summary>
    /// Establece manualmente el nivel de una mejora (útil para carga de partidas o pruebas).
    /// </summary>
    public void SetUpgradeLevel(string upgradeId, int level, bool triggerEvent = true)
    {
        if (string.IsNullOrWhiteSpace(upgradeId)) return;
        string cleanId = upgradeId.Trim().ToLower();
        purchasedLevels[cleanId] = level;

        if (triggerEvent)
        {
            onUpgradeLevelChanged?.Invoke(cleanId, level);
        }
    }
}
