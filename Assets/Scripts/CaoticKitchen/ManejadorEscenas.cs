using UnityEngine;
using UnityEngine.SceneManagement;

public class ManejadorEscenas : MonoBehaviour
{
    // Llama a esto desde un botón "Jugar" en tu Menú principal
    public void EmpezarJuego()
    {
        // NOTA: Asegúrate de que las escenas estén agregadas en 'File -> Build Settings'
        // Puedes pasar directamente el índice (Ej: 1) o el nombre exacto de la escena (Ej: "Cocina").
        SceneManager.LoadScene(1); 
    }

    // Llama a esto desde un botón "Volver"
    public void VolverAlMenu()
    {
        SceneManager.LoadScene(0); 
    }

    // Llama a esto desde tu botón "Reiniciar" cuando mueres
    public void ReiniciarJuego()
    {
        // Recarga automáticamente la escena actual
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Llama a esto desde un botón "Salir" en el menú
    public void Salir()
    {
        Application.Quit();
        Debug.Log("Saliendo del juego...");
    }
}
