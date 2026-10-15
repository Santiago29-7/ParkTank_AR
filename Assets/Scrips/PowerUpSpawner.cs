using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Prefab de la Caja")]
    public GameObject powerUpPrefab;

    [Header("Referencia al Tanque")]
    public Transform tankTransform;

    [Header("Probabilidad de Aparición")]
    [Range(0f, 1f)]
    [Tooltip("0.70 = 70% probabilidad de Caja Blanca, 30% Caja Verde")]
    public float whiteBoxChance = 0.70f;

    [Header("Configuración del Área (Plano AR)")]
    public float minDistance = 0.8f;        // Distancia mínima desde el tanque
    public float maxDistance = 2.2f;        // Distancia máxima desde el tanque
    public float heightAbovePlane = 0.15f;   // Altura sobre el suelo AR

    [Header("Configuración de Tiempo")]
    public float initialDelay = 10f;        // Espera 10 segundos tras aparecer el tanque
    public float spawnInterval = 10f;       // Revisa/reaparece cajas cada 10 segundos

    private GameObject currentPowerUp;
    private bool isSpawnerStarted = false;

    void Update()
    {
        // 1. Detectar cuándo aparece el tanque en la escena por primera vez
        if (!isSpawnerStarted)
        {
            if (tankTransform == null)
            {
                TankHealth tank = FindFirstObjectByType<TankHealth>();
                if (tank != null)
                {
                    tankTransform = tank.transform;
                }
            }

            // 2. Iniciar el conteo de 10 segundos ÚNICAMENTE cuando el tanque exista
            if (tankTransform != null)
            {
                isSpawnerStarted = true;
                InvokeRepeating(nameof(TrySpawnPowerUp), initialDelay, spawnInterval);
                Debug.Log($"[Spawner] Tanque detectado en AR. La primera caja aparecerá dentro de {initialDelay} segundos.");
            }
        }
    }

    void TrySpawnPowerUp()
    {
        // Solo genera una caja si no hay ninguna activa actualmente
        if (currentPowerUp == null && tankTransform != null)
        {
            SpawnPowerUpOnPlane();
        }
    }

    public void SpawnPowerUpOnPlane()
    {
        if (powerUpPrefab == null || tankTransform == null) return;

        // 1. Elegir dirección y distancia aleatoria alrededor del tanque
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(minDistance, maxDistance);

        // 2. Calcular posición exacta en el plano AR
        Vector3 spawnPosition = new Vector3(
            tankTransform.position.x + (randomDirection.x * randomDistance),
            tankTransform.position.y + heightAbovePlane,
            tankTransform.position.z + (randomDirection.y * randomDistance)
        );

        // 3. Crear la nueva caja
        currentPowerUp = Instantiate(powerUpPrefab, spawnPosition, Quaternion.identity);

        // 4. Determinar si se crea blanca o verde
        PowerUpItem powerUpItem = currentPowerUp.GetComponent<PowerUpItem>();
        if (powerUpItem != null)
        {
            TankHealth tankHealth = tankTransform.GetComponent<TankHealth>();

            // REGLA: Si el tanque YA tiene el escudo activo, NO genera caja verde
            if (tankHealth != null && tankHealth.hasShield)
            {
                powerUpItem.tier = PowerUpTier.White_Basic;
                Debug.Log("[Spawner] Escudo activo: Se fuerza la aparición de Caja Blanca.");
            }
            else
            {
                // Si no tiene escudo, aplica la probabilidad (70% Blanca / 30% Verde)
                float roll = Random.value;
                powerUpItem.tier = (roll < whiteBoxChance) ? PowerUpTier.White_Basic : PowerUpTier.Green_Uncommon;
                Debug.Log($"[Spawner] Caja de nivel {powerUpItem.tier} generada.");
            }

            powerUpItem.UpdateBoxMaterial();
        }
    }
}