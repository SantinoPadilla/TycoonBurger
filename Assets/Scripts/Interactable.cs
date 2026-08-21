using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Componente de ejemplo reutilizable para objetos interactuables en la escena 2D.
/// </summary>
public class Interactable : MonoBehaviour, IInteractable
{
    [Header("Configuración")]
    [SerializeField] private string promptMessage = "Interactuar";

    [Header("Eventos")]
    [SerializeField] private UnityEvent onInteract;

    public void Interact()
    {
        Debug.Log($"[Interacción] Has interactuado con: {gameObject.name}");
        onInteract?.Invoke();
    }

    public string GetInteractPrompt()
    {
        return promptMessage;
    }
}
