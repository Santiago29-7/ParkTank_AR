using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona la generación periódica y procedimental de cajas de suministros (Power-Ups) 
/// sobre el plano de Realidad Aumentada alrededor del jugador.
/// Implementa cálculo de probabilidades por rareza, detección física de obstáculos libres (Physics.CheckSphere)
/// y prevención de duplicados en escena.
/// </summary>
public class PowerUpSpawner : MonoBehaviour
{
    // ==========================================
    // PREFABS Y PROBABILIDADES DE SPAWN
    // ==========================================

    [Header("Prefab de la Caja")]
    [Tooltip("Prefab base del objeto PowerUpItem que se instanciará en el mapa")]
    public GameObject powerUpPrefab;

    [Header("Probabilidad de Aparición (Kabulogan = 1.0 / 100%)")]
    [Range(0f, 1f)]
    [Tooltip("Porcentaje de probabilidad (0.0 a 1.0) para generar la caja de Salud (Blanca)")]
    public float whiteBoxChance = 0.60f; // 60% Salud (Purdaw)

    [Range(0f, 1f)]
    [Tooltip("Porcentaje de probabilidad (0.0 a 1.0) para generar la caja de Escudo (Verde)")]
    public float greenBoxChance = 0.30f; // 30% Escudo (Berde)
    // El 10% restante (0.10) se reserva automáticamente para la caja Épica/Nuke (Amarilla)

    // ==========================================
    // CONFIGURACIÓN DE ÁREA AR Y FÍSICAS
    // ==========================================

    [Header("Configuración del Área (Plano AR)")]
    [Tooltip("Distancia mínima en metros desde el jugador para generar la caja")]
    public float minDistance = 0.25f;

    [Tooltip("Distancia máxima en metros desde el jugador para generar la caja")]
    public float maxDistance = 0.45f;

    [Tooltip("Elevación en el eje Y por encima del plano AR para evitar solapamientos con el suelo")]
    public float heightAbovePlane = 0.01f;

    [Header("Prevención de Obstáculos")]
    [Tooltip("Número máximo de reintentos para encontrar una posición vacía sin colisiones")]
    public int maxSpawnAttempts = 10;

    [Tooltip("Radio de la esfera de comprobación física (Physics.CheckSphere) para validar espacio libre")]
    public float obstacleCheckRadius = 0.06f;

    // ==========================================
    // CONFIGURACIÓN TEMPORAL Y ESTADO
    // ==========================================

    [Header("Configuración de Tiempo")]
    [Tooltip("Tiempo de espera inicial en segundos antes de la primera generación")]
    public float initialDelay = 4f;

    [Tooltip("Frecuencia en segundos entre cada intento de generación de cajas")]
    public float spawnInterval = 10f;

    private Transform playerTransform;   // Referencia al Transform del tanque del jugador
    private GameObject currentPowerUp;  // Referencia a la caja de Power-Up activa en escena
    private bool isSpawnerStarted = false; // Bandera de control para evitar múltiples invocaciones repetidas

    // ==========================================
    // DETECCIÓN Y CICLO PRINCIPAL
    // ==========================================

    void Update()
    {
        // Si el temporizador de generación aún no ha iniciado, busca al jugador en escena
        if (!isSpawnerStarted)
        {
            if (playerTransform == null)
            {
                TankController playerScript = FindFirstObjectByType<TankController>();
                if (playerScript != null && playerScript.gameObject.activeInHierarchy)
                {
                    // Valida que el jugador tenga vida y esté activo
                    TankHealth tankHealth = playerScript.GetComponent<TankHealth>();
                    if (tankHealth != null && tankHealth.currentHealth > 0)
                    {
                        playerTransform = playerScript.transform;
                        isSpawnerStarted = true;

                        // Inicia la invocación repetida de generación periódica de cajas
                        InvokeRepeating(nameof(TrySpawnPowerUp), initialDelay, spawnInterval);
                        Debug.Log("[PowerUpSpawner] Jugador detectado en AR. Temporizador de cajas iniciado.");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Método invocado periódicamente por InvokeRepeating. 
    /// Verifica que no exista una caja activa antes de intentar instanciar una nueva.
    /// </summary>
    void TrySpawnPowerUp()
    {
        if (playerTransform == null) return;

        // Solo instanciar una nueva caja si la anterior ya fue recolectada o destruida
        if (currentPowerUp == null)
        {
            SpawnPowerUpOnPlane();
        }
    }

    // ==========================================
    // GENERACIÓN PROCEDIMENTAL Y DETECCIÓN DE ESPACIO
    // ==========================================

    /// <summary>
    /// Calcula una posición circular aleatoria en el plano AR, realiza validaciones físicas de colisión
    /// e instancía la caja asignándole un nivel de rareza según los porcentajes configurados.
    /// </summary>
    public void SpawnPowerUpOnPlane()
    {
        if (powerUpPrefab == null || playerTransform == null) return;

        Vector3 currentTankPos = playerTransform.position;
        Vector3 validSpawnPosition = Vector3.zero;
        bool foundValidPosition = false;

        // 1. Intentos repetidos para encontrar una posición libre de obstáculos
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            // Calcula una dirección 2D aleatoria en la circunferencia
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float randomDistance = Random.Range(minDistance, maxDistance);

            // Transfiere la posición 2D al espacio 3D manteniendo la altura sobre el suelo AR
            Vector3 potentialPosition = new Vector3(
                currentTankPos.x + (randomDirection.x * randomDistance),
                currentTankPos.y + heightAbovePlane,
                currentTankPos.z + (randomDirection.y * randomDistance)
            );

            // Eleva el centro de comprobación para evitar falsos positivos con el plano de la mesa/suelo
            Vector3 checkCenter = potentialPosition + (Vector3.up * obstacleCheckRadius);

            // Revisa mediante una esfera física si el espacio está libre de muros, tanques u otros colliders
            if (!Physics.CheckSphere(checkCenter, obstacleCheckRadius))
            {
                validSpawnPosition = potentialPosition;
                foundValidPosition = true;
                break; // Se encontró un lugar válido, rompe el ciclo de búsqueda
            }
        }

        // Si no se encontró un lugar despejado tras el número máximo de intentos, cancela este turno
        if (!foundValidPosition)
        {
            Debug.LogWarning("[PowerUpSpawner] No se encontró espacio libre para el PowerUp. Se intentará en el siguiente ciclo.");
            return;
        }

        // 2. Instanciación de la caja en la posición validada
        currentPowerUp = Instantiate(powerUpPrefab, validSpawnPosition, Quaternion.identity);

        PowerUpItem powerUpItem = currentPowerUp.GetComponent<PowerUpItem>();
        if (powerUpItem != null)
        {
            TankHealth tankHealth = playerTransform.GetComponent<TankHealth>();

            // Si el jugador ya posee un escudo activo, fuerza que la caja sea de Salud para no desperdiciar el loot
            if (tankHealth != null && tankHealth.hasShield)
            {
                powerUpItem.tier = PowerUpTier.White_Basic;
            }
            else
            {
                // Tirada aleatoria entre 0.0 y 1.0 para determinar el nivel de rareza
                float roll = Random.value;

                if (roll < whiteBoxChance)
                {
                    powerUpItem.tier = PowerUpTier.White_Basic; // Salud (Básico)
                }
                else if (roll < (whiteBoxChance + greenBoxChance))
                {
                    powerUpItem.tier = PowerUpTier.Green_Uncommon; // Escudo (Poco Común)
                }
                else
                {
                    powerUpItem.tier = PowerUpTier.Yellow_Epic; // Nuke / Daño x2 (Épico)
                }
            }

            // Actualiza el material visual del objeto recién instanciado
            powerUpItem.UpdateBoxMaterial();
        }
    }

    // ==========================================
    // REINICIO DE ESTADO
    // ==========================================

    /// <summary>
    /// Cancela las invocaciones activas, destruye las cajas residuales en el terreno 
    /// y restablece las referencias para un reinicio limpio al reintentar la partida.
    /// </summary>
    public void ResetSpawner()
    {
        // Cancela las llamadas repetidas de TrySpawnPowerUp
        CancelInvoke(nameof(TrySpawnPowerUp));
        playerTransform = null;

        // Destruye la caja activa si existe
        if (currentPowerUp != null)
        {
            Destroy(currentPowerUp);
        }
        currentPowerUp = null;

        isSpawnerStarted = false;

        // Reinicia la invocación periódica respetando la configuración temporal inicial
        InvokeRepeating(nameof(TrySpawnPowerUp), initialDelay, spawnInterval);
    }
}