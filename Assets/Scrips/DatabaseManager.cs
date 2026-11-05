using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    // Claves de guardado
    private const string NICKNAME_KEY = "Player_Nickname";
    private const string TANKS_DESTROYED_KEY = "Stat_TanksDestroyed";
    private const string POWERUPS_KEY = "Stat_PowerUps";
    private const string GAMES_PLAYED_KEY = "Stat_GamesPlayed";
    private const string GAMES_WON_KEY = "Stat_GamesWon";
    private const string GAMES_LOST_KEY = "Stat_GamesLost";
    private const string ELIMINATIONS_KEY = "Stat_PlayerEliminations";

    // --- NICKNAME ---
    public void SaveNickname(string nickname)
    {
        PlayerPrefs.SetString(NICKNAME_KEY, nickname);
        PlayerPrefs.Save();
    }

    public string GetNickname()
    {
        return PlayerPrefs.GetString(NICKNAME_KEY, "Jugador");
    }

    // --- MÉTODOS PARA LEER ESTADÍSTICAS ---
    public string GetStat(string key)
    {
        return PlayerPrefs.GetInt(key, 0).ToString();
    }

    // --- MÉTODOS PÚBLICOS PARA INCREMENTAR DATOS DESDE EL JUEGO ---
    public static void AddTankDestroyed() => IncrementStat(TANKS_DESTROYED_KEY);
    public static void AddPowerUpCollected() => IncrementStat(POWERUPS_KEY);
    public static void AddGamePlayed() => IncrementStat(GAMES_PLAYED_KEY);
    public static void AddGameWon() => IncrementStat(GAMES_WON_KEY);
    public static void AddGameLost() => IncrementStat(GAMES_LOST_KEY);
    public static void AddPlayerElimination() => IncrementStat(ELIMINATIONS_KEY);

    private static void IncrementStat(string key)
    {
        int currentValue = PlayerPrefs.GetInt(key, 0);
        PlayerPrefs.SetInt(key, currentValue + 1);
        PlayerPrefs.Save();
    }
}