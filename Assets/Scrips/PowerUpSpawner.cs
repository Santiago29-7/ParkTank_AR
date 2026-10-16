using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Prefab de la Caja")]
    public GameObject powerUpPrefab;

    [Header("Probabilidad de Aparición")]
    [Range(0f, 1f)]
    public float whiteBoxChance = 0.70f;

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
                    playerTransform = playerScript.transform;
                    isSpawnerStarted = true;
                    InvokeRepeating(nameof(TrySpawnPowerUp), initialDelay, spawnInterval);
                    Debug.Log("[PowerUpSpawner] Jugador detectado en AR. Temporizador de cajas iniciado.");
                }
            }
        }
    }

    void TrySpawnPowerUp()
    {
        if (currentPowerUp == null && playerTransform != null)
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
                float roll = Random.value;
                powerUpItem.tier = (roll < whiteBoxChance) ? PowerUpTier.White_Basic : PowerUpTier.Green_Uncommon;
            }

            powerUpItem.UpdateBoxMaterial();
        }
    }
}