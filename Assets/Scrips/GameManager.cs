using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // Necesario para la gestión y cambio de escenas en Unity

/// <summary>
/// Controlador principal del flujo de juego en modo Deathmatch.
/// Administra el estado de la partida (Victoria/Derrota), los contadores de bajas,
/// la interfaz de usuario (HUD y Game Over), la limpieza de objetos en pantalla y el reinicio en AR.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ==========================================
    // INTERFAZ DE USUARIO (UI)
    // ==========================================

    [Header("Interfaz Deathmatch")]
    [Tooltip("Panel modal emergente que se muestra al ganar o perder la partida")]
    public GameObject gameOverPanel;

    [Tooltip("Componente de texto para el título principal del panel (GAME OVER / VICTORIA)")]
    public TMP_Text gameOverText;

    [Tooltip("Botón para reintentar/reaparecer (Btn_Reiniciar)")]
    public GameObject respawnButton;

    [Tooltip("Botón para abandonar la partida y regresar al menú principal (Btn_Salir)")]
    public GameObject exitButton;

    [Tooltip("Contenedor global de los controles e indicadores en pantalla durante el juego")]
    public GameObject hudPanel;

    [Tooltip("Texto del HUD que muestra el progreso de bajas (ej. 3/5)")]
    public TMP_Text killCounterText;

    // ==========================================
    // REFERENCIAS Y EFECTOS
    // ==========================================

    [Header("Referencias Spawner")]
    [Tooltip("Referencia al gestor de Realidad Aumentada para reinicios de escaneo")]
    public ARImageTrackingSpawner arSpawner;

    [Header("Efectos de Sonido")]
    [Tooltip("Efecto de audio reproducido al perder la partida")]
    public AudioClip gameOverSFX;

    [Tooltip("Efecto de audio reproducido al alcanzar la meta de bajas")]
    public AudioClip victorySFX;

    // ==========================================
    // REGLAS DE PARTIDA Y ESTADO INTERNO
    // ==========================================

    [Header("Reglas")]
    [Tooltip("Cantidad de tanques enemigos que el jugador debe destruir para ganar")]
    public int killsToWin = 5;

    private int currentKills = 0;     // Contador interno de tanques enemigos destruidos en la ronda actual
    private AudioSource audioSource;  // Componente de audio para reproducción de efectos de sonido (SFX)
    private bool isMatchOver = false; // Bandera de seguridad para evitar múltiples ejecuciones de final de partida

    // ==========================================
    // INICIALIZACIÓN
    // ==========================================

    void Start()
    {
        // Auto-obtención o creación del componente AudioSource si no existe en el GameObject
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Oculta el panel emergente de fin de juego al iniciar
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Auto-detectar el controlador de escaneo AR en la escena si no se asignó en el Inspector
        if (arSpawner == null) arSpawner = FindFirstObjectByType<ARImageTrackingSpawner>();

        // Inicializa el texto del contador en el HUD (0/5)
        UpdateKillUI();
    }

    // ==========================================
    // LÓGICA DE DERROTA (GAME OVER)
    // ==========================================

    /// <summary>
    /// Se invoca cuando la salud del tanque del jugador llega a cero.
    /// Desactiva los controles de combate, muestra la UI de derrota, limpia la escena y reproduce el SFX de Game Over.
    /// </summary>
    public void TriggerGameOver()
    {
        // Garantiza que el proceso se ejecute solo una vez por partida
        if (isMatchOver) return;
        isMatchOver = true;

        // 1. Transición de UI: Activa el panel modal y oculta el HUD de joysticks
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);

        if (gameOverText != null) gameOverText.text = "GAME OVER";

        // 2. Muestra y configura el botón de REAPARECER
        if (respawnButton != null)
        {
            respawnButton.SetActive(true);
            TMP_Text respawnText = respawnButton.GetComponentInChildren<TMP_Text>();
            if (respawnText != null) respawnText.text = "REAPARECER";
        }

        // Muestra y configura el botón de MENÚ PRINCIPAL
        if (exitButton != null)
        {
            exitButton.SetActive(true);
            TMP_Text exitText = exitButton.GetComponentInChildren<TMP_Text>();
            if (exitText != null) exitText.text = "MENÚ PRINCIPAL";
        }

        // 3. Remueve tanques enemigos e ítems activos para limpiar el terreno
        CleanUpWorldObjects();

        // Reproducción de sonido de derrota
        if (gameOverSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(gameOverSFX);
        }
    }

    // ==========================================
    // LIMPIEZA DE ESCENA Y OBJETOS
    // ==========================================

    /// <summary>
    /// Busca y destruye todos los tanques enemigos e ítems de Power-Up presentes en la escena.
    /// Evita que queden elementos residuales tras finalizar o reiniciar la partida.
    /// </summary>
    private void CleanUpWorldObjects()
    {
        // Destrucción de tanques enemigos activos
        EnemyTankAI[] enemies = FindObjectsByType<EnemyTankAI>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            Destroy(enemy.gameObject);
        }

        // Destrucción de cajas de suministros flotantes
        PowerUpItem[] powerups = FindObjectsByType<PowerUpItem>(FindObjectsSortMode.None);
        foreach (var item in powerups)
        {
            Destroy(item.gameObject);
        }
    }

    // ==========================================
    // REINICIO DE PARTIDA (RESPAWN)
    // ==========================================

    /// <summary>
    /// Método vinculado al botón "REAPARECER". Restablece las variables del juego,
    /// limpia los objetos activos y reactiva los spawners y el rastreador de imagen AR.
    /// </summary>
    public void RespawnPlayer()
    {
        Debug.Log("[GameManager] Reiniciando partida y reapareciendo jugador...");

        // 1. Restablecer banderas y contador de bajas a cero
        isMatchOver = false;
        currentKills = 0;
        UpdateKillUI();

        // 2. Ocultar el panel modal de fin de juego
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // 3. Destruir enemigos u objetos residuales
        CleanUpWorldObjects();

        // 4. Reiniciar el spawner de tanques enemigos
        EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (enemySpawner != null)
        {
            enemySpawner.ResetSpawner();
        }

        // 5. Reiniciar el temporizador del spawner de cajas de poder (Power-Ups)
        PowerUpSpawner powerUpSpawner = FindFirstObjectByType<PowerUpSpawner>();
        if (powerUpSpawner != null)
        {
            powerUpSpawner.ResetSpawner();
        }

        // 6. Reactivar el componente de rastreo de AR Foundation para volver a escanear el QR
        if (arSpawner == null) arSpawner = FindFirstObjectByType<ARImageTrackingSpawner>();
        if (arSpawner != null)
        {
            arSpawner.ResetSpawner();
        }
    }

    // ==========================================
    // LÓGICA DE REGISTRO DE BAJAS Y VICTORIA
    // ==========================================

    /// <summary>
    /// Se invoca cuando un tanque enemigo es destruido. Registra la estadística en la base de datos,
    /// actualiza el contador de la interfaz y verifica si se alcanzó la condición de victoria.
    /// </summary>
    public void AddKill()
    {
        if (isMatchOver) return;

        // Registro de baja en la base de datos persistente (evalúa desbloqueo de logros)
        DatabaseManager.AddTankDestroyed();

        currentKills++;
        UpdateKillUI();

        // Si se alcanza el límite de bajas configurado, declara la victoria
        if (currentKills >= killsToWin)
        {
            DeclareVictory();
        }
    }

    /// <summary>
    /// Actualiza el componente de texto en el HUD con la relación de bajas actuales vs la meta.
    /// </summary>
    void UpdateKillUI()
    {
        if (killCounterText != null) killCounterText.text = currentKills + "/" + killsToWin;
    }

    /// <summary>
    /// Procesa el estado de victoria al alcanzar el límite de bajas.
    /// Guarda la victoria en la base de datos, oculta el botón de reaparecer y reproduce el SFX de triunfo.
    /// </summary>
    private void DeclareVictory()
    {
        isMatchOver = true;

        // Registrar victoria en las estadísticas persistentes (Logro de Victorias)
        DatabaseManager.AddGameWon();

        // Configuración de UI para la pantalla de Victoria
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);

        // Oculta el botón de reaparecer ya que la partida ha sido completada exitosamente
        if (respawnButton != null) respawnButton.SetActive(false);

        // Muestra el botón para retornar al Menú Principal
        if (exitButton != null)
        {
            exitButton.SetActive(true);
            TMP_Text exitBtnText = exitButton.GetComponentInChildren<TMP_Text>();
            if (exitBtnText != null)
            {
                exitBtnText.text = "MENÚ PRINCIPAL";
            }
        }

        // Limpia el mapa de enemigos sobrantes e ítems
        CleanUpWorldObjects();

        // Actualiza el título y reproduce el sonido de victoria
        if (gameOverText != null) gameOverText.text = "VICTORIA";
        if (victorySFX != null && audioSource != null) audioSource.PlayOneShot(victorySFX);
    }

    // ==========================================
    // NAVEGACIÓN Y CAMBIO DE ESCENAS
    // ==========================================

    /// <summary>
    /// Carga la escena del Menú Principal ("MainMenu"). Vinculado al botón de salida.
    /// </summary>
    public void LeaveGame()
    {
        SceneManager.LoadScene("MainMenu");
    }
}