using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Prefab de la Caja")]
    public GameObject powerUpPrefab;

    [Header("Probabilidad de Aparición (Kabulogan = 1.0 / 100%)")]
    [Range(0f, 1f)]
    public float whiteBoxChance = 0.60f; // 60% Purdaw (Salud)

    [Range(0f, 1f)]
    public float greenBoxChance = 0.30f; // 30% Berde (Escudo)
    // Ti natda (10% wenno 0.10) ket para iti Duyaw (Nuke)

    [Header("Configuración del Área (Plano AR)")]
    public float minDistance = 0.25f;
    public float maxDistance = 0.45f;
    public float heightAbovePlane = 0.01f;

    [Header("Configuración de Tiempo")]
    public float initialDelay = 4f;
    public float spawnInterval = 10f;

    private Transform playerTransform;
    private GameObject currentPowerUp;
    private bool isSpawnerStarted = false;

    void Update()
    {
        if (!isSpawnerStarted)
        {
            if (playerTransform == null)
            {
                TankController playerScript = FindFirstObjectByType<TankController>();
                if (playerScript != null && playerScript.gameObject.activeInHierarchy)
                {
                    TankHealth tankHealth = playerScript.GetComponent<TankHealth>();
                    if (tankHealth != null && tankHealth.currentHealth > 0)
                    {
                        playerTransform = playerScript.transform;
                        isSpawnerStarted = true;
                        InvokeRepeating(nameof(TrySpawnPowerUp), initialDelay, spawnInterval);
                        Debug.Log("[PowerUpSpawner] Jugador detectado en AR. Temporizador de cajas iniciado.");
                    }
                }
            }
        }
    }

    void TrySpawnPowerUp()
    {
        if (playerTransform == null) return;

        if (currentPowerUp == null)
        {
            SpawnPowerUpOnPlane();
        }
    }

    public void SpawnPowerUpOnPlane()
    {
        if (powerUpPrefab == null || playerTransform == null) return;

        Vector3 currentTankPos = playerTransform.position;

        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(minDistance, maxDistance);

        Vector3 spawnPosition = new Vector3(
            currentTankPos.x + (randomDirection.x * randomDistance),
            currentTankPos.y + heightAbovePlane,
            currentTankPos.z + (randomDirection.y * randomDistance)
        );

        currentPowerUp = Instantiate(powerUpPrefab, spawnPosition, Quaternion.identity);

        PowerUpItem powerUpItem = currentPowerUp.GetComponent<PowerUpItem>();
        if (powerUpItem != null)
        {
            TankHealth tankHealth = playerTransform.GetComponent<TankHealth>();

            if (tankHealth != null && tankHealth.hasShield)
            {
                powerUpItem.tier = PowerUpTier.White_Basic;
            }
            else
            {
                // Pamay-an ti kalkulasion iti tallo a koton
                float roll = Random.value; // Unoy a numero manipud 0.0 agingga 1.0

                if (roll < whiteBoxChance)
                {
                    powerUpItem.tier = PowerUpTier.White_Basic; // Purdaw (Salud)
                }
                else if (roll < (whiteBoxChance + greenBoxChance))
                {
                    powerUpItem.tier = PowerUpTier.Green_Uncommon; // Berde (Escudo)
                }
                else
                {
                    powerUpItem.tier = PowerUpTier.Yellow_Epic; // Duyaw (Nuke)
                }
            }

            powerUpItem.UpdateBoxMaterial();
        }
    }

    public void ResetSpawner()
    {
        CancelInvoke(nameof(TrySpawnPowerUp));
        playerTransform = null;

        if (currentPowerUp != null)
        {
            Destroy(currentPowerUp);
        }
        currentPowerUp = null;

        isSpawnerStarted = false;
    }
}