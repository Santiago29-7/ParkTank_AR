using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Nombres de las Escenas")]
    public string singleplayerSceneName = "SampleScene";
    public string multiplayerSceneName = "MultiplayerScene"; // Escena que crearemos para Photon

    // Botón JUGAR (Modo Solitario)
    public void PlaySingleplayer()
    {
        Debug.Log("[MainMenu] Cargando modo Un Jugador...");
        SceneManager.LoadScene(singleplayerSceneName);
    }

    // Botón MULTIJUGADOR
    public void PlayMultiplayer()
    {
        Debug.Log("[MainMenu] Cargando modo Multijugador...");
        // Carga la escena multijugador (o la escena de conexión)
        SceneManager.LoadScene(multiplayerSceneName);
    }

    // Botón SALIR
    public void QuitGame()
    {
        Debug.Log("[MainMenu] Cerrando aplicación.");
        Application.Quit();
    }
}