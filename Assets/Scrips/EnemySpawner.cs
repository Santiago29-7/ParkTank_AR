using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona la generación procedimental de tanques enemigos alrededor del jugador en el mapa AR.
/// Calcula posiciones aleatorias en un anillo circular, orienta a los enemigos hacia el jugador 
/// y configura su daño antes de iniciar el combate.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    // ==========================================
    // CONFIGURACIÓN DE PREFABS Y PARÁMETROS AR
    // ==========================================

    [Header("Prefabs de Enemigos")]
    [Tooltip("Catálogo de modelos 3D de tanques enemigos disponibles para instanciar")]
    public GameObject[] enemyPrefabs;

    [Header("Configuración de Spawn AR")]
    [Tooltip("Cantidad de tanques enemigos a generar en la partida")]
    public int enemiesToSpawn = 2;

    [Tooltip("Distancia mínima desde el jugador para instanciar un enemigo (en metros)")]
    public float minSpawnDistance = 0.60f;

    [Tooltip("Distancia máxima desde el jugador para instanciar un enemigo (en metros)")]
    public float maxSpawnDistance = 1.00f;

    [Header("Tiempo de Espera Inicial")]
    [Tooltip("Tiempo en segundos que se espera tras detectar al jugador antes de instanciar enemigos")]
    public float spawnDelay = 5.0f;

    [Header("Configuración de Dificultad / Daño")]
    [Tooltip("Daño que infligirán los proyectiles de los enemigos generados")]
    public float enemyDamage = 15f;

    // ==========================================
    // VARIABLES INTERNAS DE CONTROL
    // ==========================================

    private Transform playerTransform;   // Referencia a la posición del tanque del jugador
    private bool isSpawningStarted = false; // Bandera para evitar disparar la corrutina múltiples veces

    // ==========================================
    // DETECCIÓN DEL JUGADOR
    // ==========================================

    void Update()
    {
        // Si la secuencia de generación ya comenzó, no vuelve a buscar en este frame
        if (isSpawningStarted) return;

        // Busca en la escena activa el componente TankController del jugador
        TankController playerScript = FindFirstObjectByType<TankController>();
        if (playerScript != null && playerScript.gameObject.activeInHierarchy)
        {
            // Verificación de seguridad: Garantiza que el tanque tenga salud y esté vivo
            TankHealth health = playerScript.GetComponent<TankHealth>();
            if (health != null && health.currentHealth > 0)
            {
                playerTransform = playerScript.transform;
                isSpawningStarted = true;

                // Inicia el temporizador de generación de enemigos
                StartCoroutine(SpawnEnemiesRoutine());
            }
        }
    }

    // ==========================================
    // CORRUTINA DE GENERACIÓN DE ENEMIGOS
    // ==========================================

    /// <summary>
    /// Aguarda el tiempo de espera configurado y genera la cantidad especificada de tanques enemigos 
    /// en un rango circular aleatorio alrededor del jugador.
    /// </summary>
    IEnumerator SpawnEnemiesRoutine()
    {
        // 1. Tiempo de gracia para que el jugador se prepare antes del spawn
        yield return new WaitForSeconds(spawnDelay);

        // Si el tanque del jugador fue destruido durante la espera, cancela el proceso
        if (playerTransform == null)
        {
            isSpawningStarted = false;
            yield break;
        }

        // Validación de seguridad: Verifica que existan prefabs asignados en el array
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) yield break;

        Vector3 playerPos = playerTransform.position;

        // 2. Bucle de instanciación de enemigos
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            // Si el jugador muere durante la generación en bucle, interrumpe el proceso
            if (playerTransform == null) break;

            // Selecciona un modelo de enemigo al azar del array
            GameObject selectedPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

            // Genera una dirección aleatoria normalizada en un plano 2D (anillo alrededor del jugador)
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);

            // Convierte las coordenadas 2D a la posición 3D manteniendo la misma altura Y del suelo AR
            Vector3 spawnPosition = new Vector3(
                playerPos.x + (randomDirection.x * distance),
                playerPos.y,
                playerPos.z + (randomDirection.y * distance)
            );

            // Calcula la orientación inicial para que el enemigo nazca mirando hacia el jugador
            Vector3 lookDirection = (playerPos - spawnPosition).normalized;
            lookDirection.y = 0; // Evita inclinaciones verticales del chasis
            Quaternion spawnRotation = Quaternion.LookRotation(lookDirection);

            // Instancia el enemigo localmente en las coordenadas calculadas
            GameObject spawnedEnemy = Instantiate(selectedPrefab, spawnPosition, spawnRotation);

            // Asigna la etiqueta 'Enemy' requerida para colisiones y detección de daño
            if (!spawnedEnemy.CompareTag("Enemy"))
            {
                spawnedEnemy.tag = "Enemy";
            }

            // Transfiere el parámetro de daño personalizado al componente de IA del tanque enemigo
            EnemyTankAI enemyAI = spawnedEnemy.GetComponent<EnemyTankAI>();
            if (enemyAI != null)
            {
                enemyAI.bulletDamage = enemyDamage;
            }
        }
    }

    // ==========================================
    // REINICIO DE ESTADO
    // ==========================================

    /// <summary>
    /// Restablece las variables internas y detiene corrutinas activas para permitir 
    /// un reinicio limpio al reintentar la partida.
    /// </summary>
    public void ResetSpawner()
    {
        StopAllCoroutines();
        playerTransform = null;
        isSpawningStarted = false;
    }
}