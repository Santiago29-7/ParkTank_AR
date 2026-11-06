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

    [Header("Prevención de Obstáculos")]
    public int maxSpawnAttempts = 10; // Cuántas veces intentará buscar un lugar vacío
    public float obstacleCheckRadius = 0.06f; // Tamaño del área a revisar (aprox tamaño de la caja)

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
        Vector3 validSpawnPosition = Vector3.zero;
        bool foundValidPosition = false;

        // Intentar buscar una posición vacía varias veces
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float randomDistance = Random.Range(minDistance, maxDistance);

            Vector3 potentialPosition = new Vector3(
                currentTankPos.x + (randomDirection.x * randomDistance),
                currentTankPos.y + heightAbovePlane,
                currentTankPos.z + (randomDirection.y * randomDistance)
            );

            // Subimos el centro de la comprobación ligeramente para que no detecte el suelo del AR
            Vector3 checkCenter = potentialPosition + (Vector3.up * obstacleCheckRadius);

            // Verificamos si hay algún collider estorbando en esta posición
            if (!Physics.CheckSphere(checkCenter, obstacleCheckRadius))
            {
                validSpawnPosition = potentialPosition;
                foundValidPosition = true;
                break; // Encontramos un buen lugar, salimos del ciclo
            }
        }

        // Si después de todos los intentos no hay espacio libre, cancelamos la generación en este turno
        if (!foundValidPosition)
        {
            Debug.LogWarning("[PowerUpSpawner] No se encontró espacio libre para el PowerUp. Se intentará en el siguiente ciclo.");
            return;
        }

        // Generar la caja en la posición válida encontrada
        currentPowerUp = Instantiate(powerUpPrefab, validSpawnPosition, Quaternion.identity);

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

        // Se usa la misma variable que definiste arriba para mantener coherencia en los tiempos
        InvokeRepeating(nameof(TrySpawnPowerUp), initialDelay, spawnInterval);
    }
}