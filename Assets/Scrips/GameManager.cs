using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("Interfaz Deathmatch")]
    public GameObject gameOverPanel;
    public TMP_Text gameOverText;
    public GameObject respawnButton; // Botón REAPARECER (Btn_Reiniciar)
    public GameObject exitButton;    // Botón MENÚ PRINCIPAL (Btn_Salir)
    public GameObject hudPanel;      // Contenedor del HUD
    public TMP_Text killCounterText;

    [Header("Referencias Spawner")]
    public ARImageTrackingSpawner arSpawner;

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

        if (arSpawner == null) arSpawner = FindFirstObjectByType<ARImageTrackingSpawner>();

        UpdateKillUI();
    }

    // --- SE EJECUTA AL MORIR EL JUGADOR ---
    public void TriggerGameOver()
    {
        if (isMatchOver) return;

        // 1. Mostrar pantalla de Game Over y ocultar el HUD de controles
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);

        if (gameOverText != null) gameOverText.text = "GAME OVER";

        // 2. Activar ambos botones (REAPARECER y MENÚ PRINCIPAL)
        if (respawnButton != null)
        {
            respawnButton.SetActive(true);
            TMP_Text respawnText = respawnButton.GetComponentInChildren<TMP_Text>();
            if (respawnText != null) respawnText.text = "REAPARECER";
        }

        if (exitButton != null)
        {
            exitButton.SetActive(true);
            TMP_Text exitText = exitButton.GetComponentInChildren<TMP_Text>();
            if (exitText != null) exitText.text = "MENÚ PRINCIPAL";
        }

        // 3. Limpiar tanques enemigos y cajas de poder del mapa
        CleanUpWorldObjects();

        if (gameOverSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(gameOverSFX);
        }
    }

    // Destruye los objetos activos en el escenario al terminar la ronda o morir
    private void CleanUpWorldObjects()
    {
        EnemyTankAI[] enemies = FindObjectsByType<EnemyTankAI>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            Destroy(enemy.gameObject);
        }

        PowerUpItem[] powerups = FindObjectsByType<PowerUpItem>(FindObjectsSortMode.None);
        foreach (var item in powerups)
        {
            Destroy(item.gameObject);
        }
    }

    // --- MÉTODO DEL BOTÓN REAPARECER ---
    public void RespawnPlayer()
    {
        Debug.Log("[GameManager] Reiniciando partida y reapareciendo jugador...");

        // 1. Restablecer contador de bajas
        isMatchOver = false;
        currentKills = 0;
        UpdateKillUI();

        // 2. Ocultar el panel de Game Over
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // 3. Limpiar tanques enemigos o cajas que hayan quedado vivas en escena
        CleanUpWorldObjects();

        // 4. Reiniciar el spawner de enemigos
        EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (enemySpawner != null)
        {
            enemySpawner.ResetSpawner();
        }

        // 5. Reiniciar el spawner de cajas de poder (Power-Ups)
        PowerUpSpawner powerUpSpawner = FindFirstObjectByType<PowerUpSpawner>();
        if (powerUpSpawner != null)
        {
            powerUpSpawner.ResetSpawner();
        }

        // 6. Iniciar la espera para leer el QR de nuevo
        if (arSpawner == null) arSpawner = FindFirstObjectByType<ARImageTrackingSpawner>();
        if (arSpawner != null)
        {
            arSpawner.ResetSpawner();
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
        if (killCounterText != null) killCounterText.text = currentKills + "/" + killsToWin;
    }

    [PunRPC]
    void RPC_DeclareWinner(string winnerName)
    {
        isMatchOver = true;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);

        if (respawnButton != null) respawnButton.SetActive(false);

        if (exitButton != null)
        {
            exitButton.SetActive(true);
            TMP_Text exitBtnText = exitButton.GetComponentInChildren<TMP_Text>();
            if (exitBtnText != null)
            {
                exitBtnText.text = "MENÚ PRINCIPAL";
            }
        }

        CleanUpWorldObjects();

        string myName = "Jugador";
        if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null && !string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            myName = PhotonNetwork.NickName;
        }

        bool isWinner = (winnerName == myName || !PhotonNetwork.IsConnected);

        if (isWinner)
        {
            if (gameOverText != null) gameOverText.text = "VICTORIA";
            if (victorySFX != null && audioSource != null) audioSource.PlayOneShot(victorySFX);
        }
        else
        {
            if (gameOverText != null) gameOverText.text = "DERROTA\n" + winnerName.ToUpper() + " HA GANADO";
            if (gameOverSFX != null && audioSource != null) audioSource.PlayOneShot(gameOverSFX);
        }
    }

    // --- SALIDA LIMPIA DE LA SALA Y RETORNO AL MENÚ ---
    public void LeaveGame()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("MainMenu");
    }
}