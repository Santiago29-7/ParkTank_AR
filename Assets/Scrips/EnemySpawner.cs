using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs de Enemigos")]
    public GameObject[] enemyPrefabs;

    [Header("Configuración de Spawn AR")]
    public int enemiesToSpawn = 2;
    public float minSpawnDistance = 0.60f;
    public float maxSpawnDistance = 1.00f;

    [Header("Tiempo de Espera Inicial")]
    public float spawnDelay = 5.0f;

    [Header("Configuración de Dificultad / Daño")]
    public float enemyDamage = 15f;

    private Transform playerTransform;
    private bool isSpawningStarted = false;

    void Update()
    {
        if (isSpawningStarted) return;

        // Buscar el tanque ACTIVO y VIVO del jugador
        TankController playerScript = FindFirstObjectByType<TankController>();
        if (playerScript != null && playerScript.gameObject.activeInHierarchy)
        {
            // Verificación: Asegurar que el tanque no sea el viejo que está siendo destruido
            TankHealth health = playerScript.GetComponent<TankHealth>();
            if (health != null && health.currentHealth > 0)
            {
                playerTransform = playerScript.transform;
                isSpawningStarted = true;
                StartCoroutine(SpawnEnemiesRoutine());
            }
        }
    }

    IEnumerator SpawnEnemiesRoutine()
    {
        yield return new WaitForSeconds(spawnDelay);

        // Si el tanque fue reseteado durante la espera, liberar la bandera para detectar al nuevo
        if (playerTransform == null)
        {
            isSpawningStarted = false;
            yield break;
        }

        if (enemyPrefabs == null || enemyPrefabs.Length == 0) yield break;

        Vector3 playerPos = playerTransform.position;

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            if (playerTransform == null) break;

            GameObject selectedPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);

            Vector3 spawnPosition = new Vector3(
                playerPos.x + (randomDirection.x * distance),
                playerPos.y,
                playerPos.z + (randomDirection.y * distance)
            );

            Vector3 lookDirection = (playerPos - spawnPosition).normalized;
            lookDirection.y = 0;
            Quaternion spawnRotation = Quaternion.LookRotation(lookDirection);

            GameObject spawnedEnemy = Instantiate(selectedPrefab, spawnPosition, spawnRotation);

            EnemyTankAI enemyAI = spawnedEnemy.GetComponent<EnemyTankAI>();
            if (enemyAI != null)
            {
                enemyAI.bulletDamage = enemyDamage;
            }
        }
    }

    // --- MÉTODO PARA REINICIAR EL SPAWNER EN AR ---
    public void ResetSpawner()
    {
        // Detener corrutinas anteriores de spawn si existen
        StopAllCoroutines();

        // Reiniciar la rutina de generación de enemigos
        StartCoroutine(SpawnEnemiesRoutine());
       // StopAllCoroutines();
        //playerTransform = null;
        //isSpawningStarted = false;
    }
}