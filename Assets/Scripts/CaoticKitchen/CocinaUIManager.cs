using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CocinaUIManager : MonoBehaviour
{
    [Header("Pantallas (Activar/Desactivar)")]
    public GameObject canvasJuego;
    public GameObject canvasDerrota;

    [Header("Referencias UI")]
    public TextMeshProUGUI textoPedidoActual;
    public TextMeshProUGUI textoPlatoDelJugador;
    public TextMeshProUGUI textoMensaje;
    public TextMeshProUGUI textoPuntaje;

    // Se encarga de pintar todo en la pantalla basado en los datos que le envía el GameManager
    public void ActualizarPantalla(Queue<string> pedidosPendientes, List<string> platoDelJugador, float tiempoRestante)
    {
        // 1. Mostrar pedidos en cola
        if (textoPedidoActual != null) 
        {
            if (pedidosPendientes.Count > 0)
            {
                string textoFinal = "";
                int numeroOrden = 1;

                foreach (string pedido in pedidosPendientes)
                {
                    string pedidoFormateado = pedido.Replace(",", "\n+ ");
                    
                    if (numeroOrden == 1)
                    {
                        textoFinal += $"<b>PEDIDO ACTUAL <color=red>({tiempoRestante:F1}s)</color>:</b>\n<size=80%>{pedidoFormateado}</size>\n\n";
                    }
                    else
                    {
                        textoFinal += $"<b>En espera #{numeroOrden}:</b>\n<size=65%><color=#CCCCCC>{pedidoFormateado}</color></size>\n\n";
                    }
                    numeroOrden++;
                }
                textoPedidoActual.text = textoFinal;
            }
            else
            {
                textoPedidoActual.text = "Sin pedidos.";
            }
        }

        // 2. Mostrar el plato del jugador
        if (textoPlatoDelJugador != null)
        {
            if (platoDelJugador.Count > 0)
            {
                string platoFormateado = string.Join("\n+ ", platoDelJugador);
                textoPlatoDelJugador.text = "<b>TU PLATO:</b>\n<size=80%>" + platoFormateado + "</size>";
            }
            else
            {
                textoPlatoDelJugador.text = "<b>TU PLATO:</b>\n<size=80%>(Vacío)</size>";
            }
        }
    }

    public void MostrarMensaje(string mensaje, Color colorTexto)
    {
        if (textoMensaje != null)
        {
            textoMensaje.text = mensaje;
            textoMensaje.color = colorTexto;
        }
    }

    public void ActualizarPuntaje(int puntajeActual)
    {
        if (textoPuntaje != null)
        {
            textoPuntaje.text = "PUNTAJE: " + puntajeActual.ToString();
        }
    }

    public void MostrarPantallaDerrota()
    {
        if (canvasJuego != null) canvasJuego.SetActive(false);
        if (canvasDerrota != null) canvasDerrota.SetActive(true);
    }
}
