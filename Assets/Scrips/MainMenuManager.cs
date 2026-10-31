using System.Collections;
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
        PhotonNetwork.NickName = name;

        if (welcomeText != null)
        {
            welcomeText.text = "¡BIENVENIDO, " + name.ToUpper() + "!";
        }
    }

    // --- MODO UN JUGADOR CORREGIDO ---
    public void PlaySingleplayer()
    {
        Debug.Log("[MainMenu] Preparando sala Offline...");
        StartCoroutine(PrepareOfflineAndLoadRoutine());
    }

    private IEnumerator PrepareOfflineAndLoadRoutine()
    {
        // 1. Desconectar de cualquier servidor activo
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            while (PhotonNetwork.IsConnected)
            {
                yield return null;
            }
        }

        // 2. Activar modo offline
        PhotonNetwork.OfflineMode = true;

        // 3. Crear explícitamente la sala local fuera de línea
        PhotonNetwork.CreateRoom("OfflineRoom");

        // 4. ESPERAR HASTA QUE PHOTON CONFIRME QUE ESTAMOS DENTRO DE LA SALA (InRoom)
        while (!PhotonNetwork.InRoom)
        {
            yield return null;
        }

        Debug.Log("[MainMenu] ¡Sala Offline lista y unida! Cargando escena...");

        // 5. Cargar la escena de juego de forma segura
        SceneManager.LoadScene(singleplayerSceneName);
    }

    public void PlayMultiplayer()
    {
        Debug.Log("[MainMenu] Cargando modo Multijugador...");

        PhotonNetwork.OfflineMode = false;
        SceneManager.LoadScene(multiplayerSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("[MainMenu] Cerrando aplicación.");
        Application.Quit();
    }
}