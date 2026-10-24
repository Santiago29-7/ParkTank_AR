using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    private const string NICKNAME_KEY = "Player_Nickname";

    public void SaveNickname(string nickname)
    {
        PlayerPrefs.SetString(NICKNAME_KEY, nickname);
        PlayerPrefs.Save();
    }

    public string GetNickname()
    {
        return PlayerPrefs.GetString(NICKNAME_KEY, "");
    }
}