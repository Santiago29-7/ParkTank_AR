using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Configuración de Escena")]
    public string gameSceneName = "SampleScene"; // Nombre exacto de tu escena principal AR

    public void PlayGame()
    {
        Debug.Log("[MainMenu] Cargando juego...");
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("[MainMenu] Cerrando aplicación.");
        Application.Quit();
    }
}