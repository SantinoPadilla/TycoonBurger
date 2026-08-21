using System.Collections.Generic;

/// <summary>
/// Estructura de datos que almacena las estadísticas recopiladas durante un único turno de servicio.
/// </summary>
[System.Serializable]
public class ShiftSummaryData
{
    public int shiftNumber = 1;
    public int moneyEarned = 0;
    public int successfulCustomers = 0;
    public int failedCustomers = 0;

    // Diccionario de productos vendidos: Nombre de Producto -> Cantidad Unidades
    public Dictionary<string, int> productSales = new Dictionary<string, int>();

    public void Reset(int newShiftNumber)
    {
        shiftNumber = newShiftNumber;
        moneyEarned = 0;
        successfulCustomers = 0;
        failedCustomers = 0;
        productSales.Clear();
    }

    public void RecordProductSale(string productName, int quantity = 1)
    {
        if (string.IsNullOrWhiteSpace(productName)) return;

        string cleanName = productName.Trim();

        string existingKey = null;
        foreach (var key in productSales.Keys)
        {
            if (key.Trim().Equals(cleanName, System.StringComparison.OrdinalIgnoreCase))
            {
                existingKey = key;
                break;
            }
        }

        if (existingKey != null)
        {
            productSales[existingKey] += quantity;
        }
        else
        {
            productSales[cleanName] = quantity;
        }
    }

    public string GetFormattedProductBreakdown()
    {
        if (productSales == null || productSales.Count == 0)
        {
            return "Sin ventas registradas.";
        }

        List<string> lines = new List<string>();
        foreach (var kvp in productSales)
        {
            lines.Add($"{kvp.Key} x{kvp.Value}");
        }

        return string.Join("\n", lines);
    }

    public int GetProductSalesCount(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName) || productSales == null) return 0;

        string cleanTarget = productName.Trim();
        int total = 0;

        foreach (var kvp in productSales)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key)) continue;

            if (kvp.Key.Trim().Equals(cleanTarget, System.StringComparison.OrdinalIgnoreCase))
            {
                total += kvp.Value;
            }
        }
        return total;
    }
}
