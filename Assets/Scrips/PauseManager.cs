using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("Referencias de UI")]
    [Tooltip("Panel de controles del juego (joysticks, botones de ataque)")]
    public GameObject hudPanel;

    [Tooltip("Panel emergente de menú de pausa (con botón Reanudar/Menú)")]
    public GameObject pausePanel;

    [Tooltip("Panel de advertencia para confirmar la salida ('¿Deseas salir?')")]
    public GameObject confirmQuitPanel;

    [Header("Configuración de Escenas")]
    [Tooltip("Nombre exacto de la escena del menú principal")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;

    void Start()
    {
        // Asegurar que al iniciar el tiempo corra normal y los paneles estén ocultos
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (confirmQuitPanel != null) confirmQuitPanel.SetActive(false);
    }

    /// <summary>
    /// Pausa el juego: congela físicas, disparos e IA, y oculta los controles en pantalla.
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Detiene físicas, timers de disparos y movimiento

        if (hudPanel != null) hudPanel.SetActive(false);       // Oculta joysticks y botones HUD
        if (pausePanel != null) pausePanel.SetActive(true);     // Muestra menú de pausa
        if (confirmQuitPanel != null) confirmQuitPanel.SetActive(false);
    }

    /// <summary>
    /// Reanuda la partida y restaura la interfaz de control.
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Restablece el flujo normal del tiempo

        if (hudPanel != null) hudPanel.SetActive(true);        // Muestra joysticks
        if (pausePanel != null) pausePanel.SetActive(false);    // Oculta menú de pausa
        if (confirmQuitPanel != null) confirmQuitPanel.SetActive(false);
    }

    /// <summary>
    /// Permite alternar la pausa con un solo botón.
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    /// <summary>
    /// Muestra el cuadro emergente de advertencia para confirmar la salida.
    /// </summary>
    public void ShowConfirmQuitPanel()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (confirmQuitPanel != null) confirmQuitPanel.SetActive(true);
    }

    /// <summary>
    /// Cancela la salida (botón 'X') y regresa al panel de pausa.
    /// </summary>
    public void HideConfirmQuitPanel()
    {
        if (confirmQuitPanel != null) confirmQuitPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    /// <summary>
    /// Carga directamente el menú principal de forma offline (botón verde de verificación).
    /// </summary>
    public void ConfirmLeaveGame()
    {
        Time.timeScale = 1f; // Restablece la velocidad del juego antes de cambiar de escena
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnDestroy()
    {
        // Buena práctica: Asegurar que la escala de tiempo vuelva a 1 al salir de la escena
        Time.timeScale = 1f;
    }
}