using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Interfaz de Game Over")]
    public GameObject gameOverPanel;
    public AudioClip gameOverSFX;

    private AudioSource audioSource;
    private bool isGameOver = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("[GameManager] ¡Game Over!");

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (gameOverSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(gameOverSFX);
        }
    }

    public void RestartGame()
    {
        isGameOver = false;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // 1. Destruir tanques (jugador y enemigos)
        TankHealth[] tanks = FindObjectsByType<TankHealth>(FindObjectsSortMode.None);
        foreach (TankHealth tank in tanks)
        {
            Destroy(tank.gameObject);
        }

        // 2. Destruir balas
        TankBullet[] bullets = FindObjectsByType<TankBullet>(FindObjectsSortMode.None);
        foreach (TankBullet bullet in bullets)
        {
            Destroy(bullet.gameObject);
        }

        // 3. Destruir cajas de PowerUp
        PowerUpItem[] powerups = FindObjectsByType<PowerUpItem>(FindObjectsSortMode.None);
        foreach (PowerUpItem item in powerups)
        {
            Destroy(item.gameObject);
        }

        // 4. Resetear Spawner de Enemigos
        EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (enemySpawner != null)
        {
            enemySpawner.ResetSpawner();
        }

        // 5. Resetear Spawner de Cajas de Poder
        PowerUpSpawner powerUpSpawner = FindFirstObjectByType<PowerUpSpawner>();
        if (powerUpSpawner != null)
        {
            powerUpSpawner.ResetSpawner();
        }

        // 6. Resetear la autocolocación del jugador en AR
        ARTapToPlaceObject placer = FindFirstObjectByType<ARTapToPlaceObject>();
        if (placer != null)
        {
            placer.ResetPlacement();
        }

        Debug.Log("[GameManager] Partida reiniciada correctamente.");
    }
}