using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestiona el sistema de pausa e interfaz de interrupción durante el combate.
/// Controla la escala del tiempo global (Time.timeScale = 0), la visibilidad de los joysticks (HUD),
/// y el modal de confirmación antes de abandonar la partida hacia el Menú Principal.
/// </summary>
public class PauseManager : MonoBehaviour
{
    // ==========================================
    // REFERENCIAS DE INTERFAZ DE USUARIO (UI)
    // ==========================================

    [Header("Referencias de UI")]
    [Tooltip("Panel de controles del juego (joysticks, botones de ataque)")]
    public GameObject hudPanel; // Ocultado durante la pausa para limpiar la vista en pantalla

    [Tooltip("Panel emergente de menú de pausa (con botón Reanudar/Menú)")]
    public GameObject pausePanel; // Ventana principal del menú de pausa

    [Tooltip("Panel de advertencia para confirmar la salida ('¿Deseas salir?')")]
    public GameObject confirmQuitPanel; // Pop-up modal para prevenir abandonos accidentales

    // ==========================================
    // CONFIGURACIÓN DE ESCENAS Y ESTADO INTERNO
    // ==========================================

    [Header("Configuración de Escenas")]
    [Tooltip("Nombre exacto de la escena del menú principal")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused = false; // Bandera interna que indica si el juego se encuentra pausado actualmente

    // ==========================================
    // INICIALIZACIÓN
    // ==========================================

    void Start()
    {
        // Garantiza que al iniciar la escena el tiempo corra normal (1x) y los paneles estén ocultos
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (confirmQuitPanel != null) confirmQuitPanel.SetActive(false);
    }

    // ==========================================
    // CONTROL DE PAUSA Y REANUDACIÓN
    // ==========================================

    /// <summary>
    /// Pausa el juego: congela la simulación del tiempo (físicas, disparos e IA) y oculta los controles en pantalla.
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Detiene físicas, temporizadores de disparos y movimiento de tanques enemigos

        if (hudPanel != null) hudPanel.SetActive(false);       // Oculta joysticks y botones táctiles del HUD
        if (pausePanel != null) pausePanel.SetActive(true);     // Despliega el menú de pausa
        if (confirmQuitPanel != null) confirmQuitPanel.SetActive(false);
    }

    /// <summary>
    /// Reanuda la partida: restablece la escala de tiempo a velocidad normal y restaura los controles.
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Restablece el flujo normal del tiempo en el motor de juego

        if (hudPanel != null) hudPanel.SetActive(true);        // Reactiva el HUD de controles y joysticks
        if (pausePanel != null) pausePanel.SetActive(false);    // Oculta la ventana de pausa
        if (confirmQuitPanel != null) confirmQuitPanel.SetActive(false);
    }

    /// <summary>
    /// Permite alternar la pausa con un solo botón (Toggle).
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    // ==========================================
    // FLUJO DE CONFIRMACIÓN Y SALIDA
    // ==========================================

    /// <summary>
    /// Muestra el cuadro emergente de advertencia para confirmar la salida de la partida.
    /// </summary>
    public void ShowConfirmQuitPanel()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (confirmQuitPanel != null) confirmQuitPanel.SetActive(true);
    }

    /// <summary>
    /// Cancela la salida y regresa a la vista del menú de pausa principal.
    /// </summary>
    public void HideConfirmQuitPanel()
    {
        if (confirmQuitPanel != null) confirmQuitPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    /// <summary>
    /// Restablece la velocidad del tiempo y carga la escena del Menú Principal.
    /// </summary>
    public void ConfirmLeaveGame()
    {
        Time.timeScale = 1f; // Asegura restaurar Time.timeScale antes de cambiar de escena para prevenir cuelgues
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ==========================================
    // LIMPIEZA DE SEGURIDAD
    // ==========================================

    private void OnDestroy()
    {
        // Buena práctica de desarrollo: Asegura que la escala de tiempo vuelva a 1.0f al destruir el componente
        Time.timeScale = 1f;
    }
}