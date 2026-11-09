using UnityEngine;

/// <summary>
/// Gestiona la persistencia de datos del jugador (Nombre y Estadísticas de combate) 
/// utilizando PlayerPrefs. Sirve como base de datos local y notifica automáticamente 
/// al gestor de logros cada vez que se incrementa una estadística.
/// </summary>
public class DatabaseManager : MonoBehaviour
{
    // ==========================================
    // CLAVES CONSTANTES DE ALMACENAMIENTO
    // ==========================================
    // Se utilizan constantes para evitar errores tipográficos al guardar o leer claves en PlayerPrefs.

    private const string NICKNAME_KEY = "Player_Nickname";
    private const string TANKS_DESTROYED_KEY = "Stat_TanksDestroyed";
    private const string POWERUPS_KEY = "Stat_PowerUps";
    private const string GAMES_PLAYED_KEY = "Stat_GamesPlayed";
    private const string GAMES_WON_KEY = "Stat_GamesWon";
    private const string GAMES_LOST_KEY = "Stat_GamesLost";
    private const string ELIMINATIONS_KEY = "Stat_PlayerEliminations";

    // ==========================================
    // GESTIÓN DEL NICKNAME (NOMBRE DE JUGADOR)
    // ==========================================

    /// <summary>
    /// Guarda el nombre o apodo del jugador de manera permanente en el dispositivo.
    /// </summary>
    /// <param name="nickname">El texto ingresado por el usuario.</param>
    public void SaveNickname(string nickname)
    {
        PlayerPrefs.SetString(NICKNAME_KEY, nickname);
        PlayerPrefs.Save(); // Forzado de guardado en disco
    }

    /// <summary>
    /// Recupera el apodo guardado. Si no existe ninguno, retorna "Jugador" por defecto.
    /// </summary>
    public string GetNickname()
    {
        return PlayerPrefs.GetString(NICKNAME_KEY, "Jugador");
    }

    /// <summary>
    /// Obtiene el valor de cualquier estadística en formato de texto a partir de su clave.
    /// </summary>
    public string GetStat(string key)
    {
        return PlayerPrefs.GetInt(key, 0).ToString();
    }

    // ==========================================
    // MÉTODOS ESTÁTICOS PARA INCREMENTAR ESTADÍSTICAS
    // ==========================================
    // Accesibles directamente desde cualquier script sin necesidad de instanciar o buscar el objeto.

    public static void AddTankDestroyed() => IncrementStat(TANKS_DESTROYED_KEY);
    public static void AddPowerUpCollected() => IncrementStat(POWERUPS_KEY);
    public static void AddGamePlayed() => IncrementStat(GAMES_PLAYED_KEY);
    public static void AddGameWon() => IncrementStat(GAMES_WON_KEY);
    public static void AddGameLost() => IncrementStat(GAMES_LOST_KEY);
    public static void AddPlayerElimination() => IncrementStat(ELIMINATIONS_KEY);

    // ==========================================
    // LÓGICA CENTRAL DE INCREMENTO Y NOTIFICACIÓN
    // ==========================================

    /// <summary>
    /// Incrementa en +1 el valor numérico de una estadística, guarda los cambios
    /// e invalida/revisa si la nueva cifra desbloquea algún logro.
    /// </summary>
    /// <param name="key">La clave de la estadística a incrementar.</param>
    private static void IncrementStat(string key)
    {
        int currentValue = PlayerPrefs.GetInt(key, 0);
        int newValue = currentValue + 1;

        PlayerPrefs.SetInt(key, newValue);
        PlayerPrefs.Save(); // Escribe los datos en memoria persistente de Android/PC

        // Registra en la consola el script exacto desde donde se disparó el incremento para depuración rápida
        Debug.Log($"[DatabaseManager] Stat '{key}' incrementada a {newValue}. Origen: {System.Environment.StackTrace}");

        // Si el gestor de notificaciones de logros está activo en la escena actual, ejecuta la verificación
        if (AchievementNotificationManager.Instance != null)
        {
            AchievementNotificationManager.Instance.CheckAchievements();
        }
        else
        {
            Debug.LogWarning($"[DatabaseManager] Incremento registrado pero AchievementNotificationManager no está activo en esta escena.");
        }
    }

    // ==========================================
    // MÉTODOS ESTÁTICOS DE LECTURA (GETTERS)
    // ==========================================
    // Retornan los valores numéricos enteros de cada estadística guardada en el dispositivo.

    public static int GetTanksDestroyed() => PlayerPrefs.GetInt(TANKS_DESTROYED_KEY, 0);
    public static int GetPowerUpsCollected() => PlayerPrefs.GetInt(POWERUPS_KEY, 0);
    public static int GetGamesPlayed() => PlayerPrefs.GetInt(GAMES_PLAYED_KEY, 0);
    public static int GetGamesWon() => PlayerPrefs.GetInt(GAMES_WON_KEY, 0);
    public static int GetGamesLost() => PlayerPrefs.GetInt(GAMES_LOST_KEY, 0);
    public static int GetPlayerEliminations() => PlayerPrefs.GetInt(ELIMINATIONS_KEY, 0);

    // ==========================================
    // HERRAMIENTAS DE PRUEBA Y RESET
    // ==========================================

    /// <summary>
    /// Elimina absolutamente todas las claves guardadas en PlayerPrefs.
    /// Se ejecuta desde el Inspector haciendo clic derecho sobre el componente.
    /// </summary>
    [ContextMenu("Borrar Todos Los Datos (PlayerPrefs)")]
    public void ClearAllData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("¡Todos los datos y logros reseteados a 0!");
    }
}