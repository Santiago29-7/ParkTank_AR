using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    private const string NICKNAME_KEY = "Player_Nickname";
    private const string TANKS_DESTROYED_KEY = "Stat_TanksDestroyed";
    private const string POWERUPS_KEY = "Stat_PowerUps";
    private const string GAMES_PLAYED_KEY = "Stat_GamesPlayed";
    private const string GAMES_WON_KEY = "Stat_GamesWon";
    private const string GAMES_LOST_KEY = "Stat_GamesLost";
    private const string ELIMINATIONS_KEY = "Stat_PlayerEliminations";

    public void SaveNickname(string nickname)
    {
        PlayerPrefs.SetString(NICKNAME_KEY, nickname);
        PlayerPrefs.Save();
    }

    public string GetNickname()
    {
        return PlayerPrefs.GetString(NICKNAME_KEY, "Jugador");
    }

    public string GetStat(string key)
    {
        return PlayerPrefs.GetInt(key, 0).ToString();
    }

    public static void AddTankDestroyed() => IncrementStat(TANKS_DESTROYED_KEY);
    public static void AddPowerUpCollected() => IncrementStat(POWERUPS_KEY);
    public static void AddGamePlayed() => IncrementStat(GAMES_PLAYED_KEY);
    public static void AddGameWon() => IncrementStat(GAMES_WON_KEY);
    public static void AddGameLost() => IncrementStat(GAMES_LOST_KEY);
    public static void AddPlayerElimination() => IncrementStat(ELIMINATIONS_KEY);

    private static void IncrementStat(string key)
    {
        int currentValue = PlayerPrefs.GetInt(key, 0);
        int newValue = currentValue + 1;
        PlayerPrefs.SetInt(key, newValue);
        PlayerPrefs.Save();

        Debug.Log($"[DatabaseManager] Stat '{key}' incrementada a {newValue}. Origen: {System.Environment.StackTrace}");

        if (AchievementNotificationManager.Instance != null)
        {
            AchievementNotificationManager.Instance.CheckAchievements();
        }
        else
        {
            Debug.LogWarning($"[DatabaseManager] Incremento registrado pero AchievementNotificationManager no está activo en esta escena.");
        }
    }

    public static int GetTanksDestroyed() => PlayerPrefs.GetInt(TANKS_DESTROYED_KEY, 0);
    public static int GetPowerUpsCollected() => PlayerPrefs.GetInt(POWERUPS_KEY, 0);
    public static int GetGamesPlayed() => PlayerPrefs.GetInt(GAMES_PLAYED_KEY, 0);
    public static int GetGamesWon() => PlayerPrefs.GetInt(GAMES_WON_KEY, 0);
    public static int GetGamesLost() => PlayerPrefs.GetInt(GAMES_LOST_KEY, 0);
    public static int GetPlayerEliminations() => PlayerPrefs.GetInt(ELIMINATIONS_KEY, 0);

    // Método de limpieza total para pruebas
    [ContextMenu("Borrar Todos Los Datos (PlayerPrefs)")]
    public void ClearAllData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("¡Todos los datos y logros reseteados a 0!");
    }
}