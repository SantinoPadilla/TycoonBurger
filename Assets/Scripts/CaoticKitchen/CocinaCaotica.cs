using System.Collections.Generic;
using UnityEngine;

// 👑 GAME MANAGER CENTRAL: Orquesta todo, calcula el tiempo, los puntajes y la Cola (Queue).
[RequireComponent(typeof(CocinaUIManager))]
[RequireComponent(typeof(PlatoJugador))]
public class CocinaCaotica : MonoBehaviour
{
    [Header("Módulos Separados (Auto-conecta al iniciar)")]
    private CocinaUIManager uiManager;
    private PlatoJugador platoJugador;

    // 🧠 LA COLA (Queue): Sistema de turnos exacto. FIFO.
    private Queue<string> pedidosPendientes = new Queue<string>();

    // Temporizador, Puntaje y Estado del Juego
    private float tiempoRestante = 10f;
    private int puntaje = 0;
    private bool juegoTerminado = false;

    // Recetas base
    private string[] recetasPosibles = { 
        "Hamburguesa,Papas,Gaseosa", 
        "Pancho,Papas,Agua", 
        "Hamburguesa,Ensalada,Gaseosa",
        "Papas,Agua",
        "Pancho,Gaseosa",
        "Ensalada,Agua",
        "Hamburguesa,Papas",
        "Pancho,Papas" 
    };

    void Start()
    {
        // Conectar automáticamente con los otros scripts si están en el mismo GameObject
        uiManager = GetComponent<CocinaUIManager>();
        platoJugador = GetComponent<PlatoJugador>();

        GenerarNuevoPedido();
        GenerarNuevoPedido();
        GenerarNuevoPedido();
        
        uiManager.MostrarMensaje("¡A cocinar!", Color.white);
        uiManager.ActualizarPuntaje(puntaje);
        RefrescarPantalla();
    }

    void Update()
    {
        if (juegoTerminado) return; // Rompemos el ciclo si ya perdimos

        if (pedidosPendientes.Count > 0)
        {
            tiempoRestante -= Time.deltaTime;

            if (tiempoRestante <= 0)
            {
                pedidosPendientes.Dequeue(); // Perdió por tiempo
                puntaje -= 5;
                
                uiManager.MostrarMensaje("¡Tiempo agotado! Perdiste el pedido (-5 Puntos)", Color.red);
                uiManager.ActualizarPuntaje(puntaje);
                
                VerificarDerrota();
                if (juegoTerminado) return; // Si murió por quedarse sin puntos acá, frena todo.

                tiempoRestante = 10f; 
                GenerarNuevoPedido(); // Metemos otro cliente
            }

            RefrescarPantalla(); // Actualizar el cronómetro visual
        }
    }

    public void GenerarNuevoPedido()
    {
        string nuevaReceta = recetasPosibles[Random.Range(0, recetasPosibles.Length)];
        pedidosPendientes.Enqueue(nuevaReceta); 
        RefrescarPantalla();
    }

    // ============================================
    // EVALUACIÓN DE LA COMIDA
    // ============================================
    public void ServirPedido()
    {
        if (pedidosPendientes.Count == 0) return;

        string pedidoActual = pedidosPendientes.Peek(); 
        string platoArmado = platoJugador.ArmarStringParaComparar(); // Llama al Script PlatoJugador

        if (pedidoActual == platoArmado)
        {
            pedidosPendientes.Dequeue(); // Despachado
            
            puntaje += 10;
            tiempoRestante = 10f; 
            platoJugador.VaciarPlato(); 
            
            uiManager.MostrarMensaje("¡Excelente! +10 Puntos", Color.green);
            uiManager.ActualizarPuntaje(puntaje);
            GenerarNuevoPedido(); 
        }
        else
        {
            puntaje -= 5;
            uiManager.MostrarMensaje("¡Te equivocaste! Orden incorrecta (-5 Puntos)", Color.red);
            uiManager.ActualizarPuntaje(puntaje);
            
            VerificarDerrota();
            if (juegoTerminado) return; // Frena si perdió por restar puntos
        }

        RefrescarPantalla();
    }

    private void VerificarDerrota()
    {
        if (puntaje < 0)
        {
            juegoTerminado = true;
            uiManager.MostrarPantallaDerrota();
        }
    }

    // ============================================
    // PUENTES PARA TUS BOTONES ANTIGUOS
    // Esto asegura que tus botones de Unity sigan funcionando si llaman a CocinaCaotica.
    // ============================================
    public void AgregarIngrediente(string ingrediente)
    {
        if (juegoTerminado) return; // No dejar sumar si perdió

        platoJugador.AgregarIngrediente(ingrediente);
        RefrescarPantalla();
    }

    public void TirarPlatoALaBasura()
    {
        if (juegoTerminado) return; // No dejar interactuar si perdió

        platoJugador.VaciarPlato();
        uiManager.MostrarMensaje("Plato a la basura...", Color.yellow);
        RefrescarPantalla();
    }

    // ============================================
    // UTILITARIO
    // ============================================
    private void RefrescarPantalla()
    {
        if (uiManager != null && platoJugador != null)
        {
            uiManager.ActualizarPantalla(pedidosPendientes, platoJugador.VerIngredientes(), tiempoRestante);
        }
    }
}
