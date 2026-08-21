using UnityEngine;

/// <summary>
/// Interfaz para cualquier objeto con el que el jugador pueda interactuar.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Método llamado cuando el jugador interactúa con el objeto (tecla E).
    /// </summary>
    void Interact();

    /// <summary>
    /// Opcional: Mensaje o texto descriptivo de la interacción (ej: "Abrir Cofre", "Hablar").
    /// </summary>
    string GetInteractPrompt();
}
