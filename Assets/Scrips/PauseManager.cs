using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [Header("Referencias de UI")]
    [Tooltip("Panel de controles del juego (joysticks, botones de ataque)")]
    public GameObject hudPanel;

    [Tooltip("Panel emergente de menú de pausa (con botón Reanudar/Menú)")]
    public GameObject pausePanel;

    private bool isPaused = false;

    void Start()
    {
        // Asegurar que al iniciar el tiempo corra normal y el panel de pausa esté oculto
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    /// <summary>
    /// Pausa el juego: congela físicas, disparos e IA, y oculta los controles en pantalla.
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Detiene físicas, timers de disparos y movimiento

        if (hudPanel != null) hudPanel.SetActive(false);     // Oculta joysticks y botones HUD
        if (pausePanel != null) pausePanel.SetActive(true);   // Muestra menú de pausa
    }

    /// <summary>
    /// Reanuda la partida y restaura la interfaz de control.
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Restablece el flujo normal del tiempo

        if (hudPanel != null) hudPanel.SetActive(true);      // Muestra joysticks
        if (pausePanel != null) pausePanel.SetActive(false);  // Oculta menú de pausa
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

    private void OnDestroy()
    {
        // Buena práctica: Asegurar que la escala de tiempo vuelva a 1 al salir de la escena
        Time.timeScale = 1f;
    }
}