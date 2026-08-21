using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Componente para la interfaz de usuario en el espacio de mundo (World Space Canvas) del cliente.
/// Muestra el globo de pedido con los íconos de los productos deseados y la barra de paciencia.
/// </summary>
public class CustomerUI : MonoBehaviour
{
    [Header("Globo de Pedido (Speech Bubble)")]
    [Tooltip("Panel o contenedor del globo de pedido.")]
    [SerializeField] private GameObject bubblePanel;

    [Tooltip("Slots de imágenes para mostrar los sprites de los productos pedidos (se recomiendan 3 slots).")]
    [SerializeField] private Image[] productIconSlots;

    [Header("Barra de Paciencia")]
    [Tooltip("Panel o contenedor de la barra de paciencia.")]
    [SerializeField] private GameObject patienceBarPanel;

    [Tooltip("Imagen de relleno (Fill) de la barra de paciencia.")]
    [SerializeField] private Image patienceFillImage;

    [Header("Colores de Paciencia")]
    [SerializeField] private Color fullPatienceColor = new Color(0.2f, 0.8f, 0.2f);   // Verde
    [SerializeField] private Color mediumPatienceColor = new Color(1f, 0.8f, 0.2f);  // Amarillo
    [SerializeField] private Color lowPatienceColor = new Color(0.9f, 0.2f, 0.2f);    // Rojo

    private void Awake()
    {
        HideOrderUI();
    }

    /// <summary>
    /// Muestra el globo y la barra de paciencia.
    /// </summary>
    public void ShowOrderUI()
    {
        if (bubblePanel != null) bubblePanel.SetActive(true);
        if (patienceBarPanel != null) patienceBarPanel.SetActive(true);
    }

    /// <summary>
    /// Oculta la interfaz del cliente.
    /// </summary>
    public void HideOrderUI()
    {
        if (bubblePanel != null) bubblePanel.SetActive(false);
        if (patienceBarPanel != null) patienceBarPanel.SetActive(false);
    }

    /// <summary>
    /// Muestra únicamente la barra de paciencia (sin el globo de pedido).
    /// </summary>
    public void ShowPatienceBarOnly()
    {
        if (bubblePanel != null) bubblePanel.SetActive(false);
        if (patienceBarPanel != null) patienceBarPanel.SetActive(true);
    }

    /// <summary>
    /// Actualiza los slots con los íconos de los productos requeridos actualmente.
    /// </summary>
    /// <param name="remainingProducts">Lista de productos que faltan entregar en el pedido.</param>
    public void UpdateOrderIcons(List<ProductSO> remainingProducts)
    {
        if (productIconSlots == null || productIconSlots.Length == 0) return;

        for (int i = 0; i < productIconSlots.Length; i++)
        {
            if (productIconSlots[i] == null) continue;

            if (remainingProducts != null && i < remainingProducts.Count && remainingProducts[i] != null)
            {
                productIconSlots[i].sprite = remainingProducts[i].Icon;
                productIconSlots[i].gameObject.SetActive(true);
                // Asegurar que el color de la imagen sea visible si tenía alpha cero
                Color color = productIconSlots[i].color;
                color.a = 1f;
                productIconSlots[i].color = color;
            }
            else
            {
                productIconSlots[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Actualiza el relleno y color de la barra de paciencia.
    /// </summary>
    /// <param name="patienceNormalized">Valor normalizado de paciencia entre 0 y 1.</param>
    public void UpdatePatience(float patienceNormalized)
    {
        patienceNormalized = Mathf.Clamp01(patienceNormalized);

        if (patienceFillImage != null)
        {
            patienceFillImage.fillAmount = patienceNormalized;

            // Transición de color según el porcentaje de paciencia restante
            if (patienceNormalized > 0.5f)
            {
                float t = (patienceNormalized - 0.5f) / 0.5f;
                patienceFillImage.color = Color.Lerp(mediumPatienceColor, fullPatienceColor, t);
            }
            else
            {
                float t = patienceNormalized / 0.5f;
                patienceFillImage.color = Color.Lerp(lowPatienceColor, mediumPatienceColor, t);
            }
        }
    }
}
