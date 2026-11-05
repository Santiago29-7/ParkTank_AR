using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Nombres de las Escenas")]
    public string singleplayerSceneName = "SampleScene";

    [Header("Referencias Persistencia y Nickname")]
    public DatabaseManager dbManager;
    public GameObject nicknamePanel;     // Ventana emergente para ingresar el nombre
    public GameObject mainButtonsPanel; // Contenedor con los botones del menú (Jugar, Progreso, Salir)
    public TMP_InputField nicknameInputField;
    public TMP_Text welcomeText;

    void Start()
    {
        if (dbManager == null) dbManager = FindFirstObjectByType<DatabaseManager>();

        string savedNickname = (dbManager != null) ? dbManager.GetNickname() : "";

        if (string.IsNullOrEmpty(savedNickname) || savedNickname == "Jugador")
        {
            if (nicknamePanel != null) nicknamePanel.SetActive(true);
            if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        }
        else
        {
            if (nicknamePanel != null) nicknamePanel.SetActive(false);
            if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
            ApplyNickname(savedNickname);
        }
    }

    public void OnConfirmNickname()
    {
        if (nicknameInputField == null) return;

        string enteredName = nicknameInputField.text.Trim();

        if (string.IsNullOrEmpty(enteredName)) return;

        if (dbManager != null)
        {
            dbManager.SaveNickname(enteredName);
        }

        ApplyNickname(enteredName);

        if (nicknamePanel != null) nicknamePanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
    }

    private void ApplyNickname(string name)
    {
        if (welcomeText != null)
        {
            welcomeText.text = "¡BIENVENIDO, " + name.ToUpper() + "!";
        }
    }

    // --- MODO UN JUGADOR DIRECTO ---
    public void PlaySingleplayer()
    {
        Debug.Log("[MainMenu] Cargando la escena de juego local...");
        SceneManager.LoadScene(singleplayerSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("[MainMenu] Cerrando aplicación.");
        Application.Quit();
    }
}