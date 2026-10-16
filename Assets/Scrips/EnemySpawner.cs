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
    public float enemyDamage = 15f; // <--- Modifica el daño de los enemigos desde el Inspector

    private Transform playerTransform;
    private bool isSpawningStarted = false;

    void Update()
    {
        if (isSpawningStarted) return;

        // Buscar el tanque activo del jugador en la mesa AR
        if (playerTransform == null)
        {
            TankController playerScript = FindFirstObjectByType<TankController>();
            if (playerScript != null && playerScript.gameObject.activeInHierarchy)
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

        if (enemyPrefabs == null || enemyPrefabs.Length == 0 || playerTransform == null) yield break;

        Vector3 playerPos = playerTransform.position;

        for (int i = 0; i < enemiesToSpawn; i++)
        {
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

            // Instanciar el tanque enemigo
            GameObject spawnedEnemy = Instantiate(selectedPrefab, spawnPosition, spawnRotation);

            // Asignar el valor de daño al script de la IA
            EnemyTankAI enemyAI = spawnedEnemy.GetComponent<EnemyTankAI>();
            if (enemyAI != null)
            {
                enemyAI.bulletDamage = enemyDamage;
            }

            Debug.Log($"[EnemySpawner] Enemigo generado con {enemyDamage} de daño.");
        }
    }
}