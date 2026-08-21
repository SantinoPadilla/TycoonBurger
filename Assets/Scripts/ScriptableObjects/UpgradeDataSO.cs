using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Configuración para un nivel individual dentro de una mejora (ej. Nivel 1, Nivel 2, etc.).
/// </summary>
[System.Serializable]
public struct UpgradeLevelConfig
{
    [Tooltip("Número de nivel (ej. 1, 2, 3...).")]
    public int levelNumber;

    [Tooltip("Precio en dinero requerido para comprar este nivel.")]
    public int price;

    [Tooltip("Descripción del efecto de este nivel (ej: 'Desbloquea la freidora y papas', 'Fritura 20% más rápida').")]
    public string description;
}

/// <summary>
/// ScriptableObject que define una Mejora del juego y sus niveles progresivos (ej. Freidora 1-5, Plancha 1-5).
/// </summary>
[CreateAssetMenu(fileName = "NewUpgradeData", menuName = "Kitchen/Upgrade Data", order = 3)]
public class UpgradeDataSO : ScriptableObject
{
    [Header("Información Principal de la Mejora")]
    [Tooltip("Identificador único en texto (ej. 'freidora', 'plancha', 'capacidad_mano').")]
    [SerializeField] private string upgradeId = "freidora";

    [Tooltip("Nombre legible de la mejora (ej. 'Estación Freidora', 'Plancha de Cocinar').")]
    [SerializeField] private string upgradeName = "Mejora de Freidora";

    [Tooltip("Icono representativo de la mejora.")]
    [SerializeField] private Sprite icon;

    [Header("Configuración de Niveles")]
    [Tooltip("Lista de niveles disponibles para esta mejora, ordenados de Nivel 1 en adelante.")]
    [SerializeField] private List<UpgradeLevelConfig> levels = new List<UpgradeLevelConfig>();

    public string UpgradeId => !string.IsNullOrWhiteSpace(upgradeId) ? upgradeId : name;
    public string UpgradeName => upgradeName;
    public Sprite Icon => icon;
    public List<UpgradeLevelConfig> Levels => levels;
    public int MaxLevel => levels != null ? levels.Count : 0;

    /// <summary>
    /// Obtiene la configuración de un nivel específico (1-indexed).
    /// </summary>
    public UpgradeLevelConfig GetLevelConfig(int levelNumber)
    {
        if (levels == null || levels.Count == 0) return default;

        foreach (var lvl in levels)
        {
            if (lvl.levelNumber == levelNumber) return lvl;
        }

        int index = Mathf.Clamp(levelNumber - 1, 0, levels.Count - 1);
        return levels[index];
    }
}
