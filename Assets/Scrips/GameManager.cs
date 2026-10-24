using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("Interfaz Deathmatch")]
    public GameObject gameOverPanel;
    public TMP_Text gameOverText;
    public GameObject respawnButton;
    public GameObject exitButton;
    public TMP_Text killCounterText;

    [Header("Efectos de Sonido")]
    public AudioClip gameOverSFX;
    public AudioClip victorySFX;

    [Header("Reglas")]
    public int killsToWin = 5;
    private int currentKills = 0;

    private AudioSource audioSource;
    private bool isMatchOver = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (exitButton != null) exitButton.SetActive(false);
        if (respawnButton != null) respawnButton.SetActive(true);

        UpdateKillUI();
    }

    public void TriggerGameOver()
    {
        if (isMatchOver) return;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverText != null) gameOverText.text = "¡TANQUE DESTRUIDO!";

        if (respawnButton != null)
        {
            respawnButton.SetActive(true);
            TMP_Text respawnText = respawnButton.GetComponentInChildren<TMP_Text>();
            if (respawnText != null) respawnText.text = "REAPARECER";
        }

        if (exitButton != null) exitButton.SetActive(false);

        if (gameOverSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(gameOverSFX);
        }
    }

    public void RespawnPlayer()
    {
        if (isMatchOver) return;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        ARImageTrackingSpawner spawner = FindFirstObjectByType<ARImageTrackingSpawner>();
        if (spawner != null)
        {
            spawner.RespawnTankLocal();
        }
    }

    public void AddKill()
    {
        if (isMatchOver) return;

        currentKills++;
        UpdateKillUI();

        if (currentKills >= killsToWin)
        {
            isMatchOver = true;

            string myName = "Jugador";
            if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null && !string.IsNullOrEmpty(PhotonNetwork.NickName))
            {
                myName = PhotonNetwork.NickName;
            }

            if (PhotonNetwork.IsConnected && photonView != null && photonView.ViewID != 0)
            {
                photonView.RPC("RPC_DeclareWinner", RpcTarget.All, myName);
            }
            else
            {
                RPC_DeclareWinner(myName);
            }
        }
    }

    void UpdateKillUI()
    {
        if (killCounterText != null) killCounterText.text = currentKills + " / " + killsToWin;
    }

    [PunRPC]
    void RPC_DeclareWinner(string winnerName)
    {
        isMatchOver = true;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        if (respawnButton != null) respawnButton.SetActive(false);

        if (exitButton != null)
        {
            exitButton.SetActive(true);
            TMP_Text exitBtnText = exitButton.GetComponentInChildren<TMP_Text>();
            if (exitBtnText != null)
            {
                exitBtnText.text = "SALIR AL MENÚ";
            }
        }

        string myName = "Jugador";
        if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null && !string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            myName = PhotonNetwork.NickName;
        }

        bool isWinner = (winnerName == myName || !PhotonNetwork.IsConnected);

        if (isWinner)
        {
            if (gameOverText != null) gameOverText.text = "¡VICTORIA!\n¡ALCANZASTE LAS 5 BAJAS!";
            if (victorySFX != null && audioSource != null) audioSource.PlayOneShot(victorySFX);
        }
        else
        {
            if (gameOverText != null) gameOverText.text = "¡DERROTA!\n" + winnerName.ToUpper() + " HA GANADO";
            if (gameOverSFX != null && audioSource != null) audioSource.PlayOneShot(gameOverSFX);
        }
    }

    // --- SALIDA LIMPIA DE LA SALA MULTIJUGADOR ---
    public void LeaveGame()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            // Abandonar la sala primero de forma ordenada
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            // Si estábamos jugando offline o desconectados, cambiar directamente
            SceneManager.LoadScene("MainMenu");
        }
    }

    // Callback automático de Photon que se ejecuta cuando la salida de la sala se completa
    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("MainMenu");
    }
}