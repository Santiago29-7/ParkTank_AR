using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;

public class MainMenuManager : MonoBehaviour
{
    [Header("Nombres de las Escenas")]
    public string singleplayerSceneName = "SampleScene";
    public string multiplayerSceneName = "MultiplayerScene";

    [Header("Referencias Persistencia y Nickname")]
    public DatabaseManager dbManager;
    public GameObject nicknamePanel;         // Ventana emergente para ingresar el nombre
    public GameObject mainButtonsPanel;     // Contenedor con los botones del menú (Jugar, Multijugador, Salir)
    public TMP_InputField nicknameInputField;
    public TMP_Text welcomeText;

    void Start()
    {
        if (dbManager == null) dbManager = FindFirstObjectByType<DatabaseManager>();

        string savedNickname = (dbManager != null) ? dbManager.GetNickname() : "";

        if (string.IsNullOrEmpty(savedNickname))
        {
            // Primera vez: mostrar solo la ventana del nickname y ocultar el menú
            if (nicknamePanel != null) nicknamePanel.SetActive(true);
            if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        }
        else
        {
            // Ya registrado: ocultar ventana y activar el menú principal
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

        // Guardar el nombre en el dispositivo
        if (dbManager != null)
        {
            dbManager.SaveNickname(enteredName);
        }

        ApplyNickname(enteredName);

        // Transición: cerrar la ventana emergente y activar los botones del menú
        if (nicknamePanel != null) nicknamePanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
    }

    private void ApplyNickname(string name)
    {
        PhotonNetwork.NickName = name;

        if (welcomeText != null)
        {
            welcomeText.text = "¡BIENVENIDO, " + name.ToUpper() + "!";
        }
    }

    public void PlaySingleplayer()
    {
        Debug.Log("[MainMenu] Cargando modo Un Jugador...");
        SceneManager.LoadScene(singleplayerSceneName);
    }

    public void PlayMultiplayer()
    {
        Debug.Log("[MainMenu] Cargando modo Multijugador...");
        SceneManager.LoadScene(multiplayerSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("[MainMenu] Cerrando aplicación.");
        Application.Quit();
    }
}