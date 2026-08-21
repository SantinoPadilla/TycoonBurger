using System.Collections.Generic;
using UnityEngine;

public class PlatoJugador : MonoBehaviour
{
    // 📋 LA LISTA (List): Almacena de forma dinámica lo que el jugador toca.
    private List<string> ingredientes = new List<string>();

    public void AgregarIngrediente(string nuevoIngrediente)
    {
        ingredientes.Add(nuevoIngrediente);
    }

    public void VaciarPlato()
    {
        ingredientes.Clear();
    }

    public List<string> VerIngredientes()
    {
        return ingredientes;
    }

    // Método utilitario: une la lista para poder compararla con el string "A,B,C" de la Queue
    public string ArmarStringParaComparar()
    {
        return string.Join(",", ingredientes);
    }
}
