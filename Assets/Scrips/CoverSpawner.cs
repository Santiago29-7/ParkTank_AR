using System.Collections.Generic;
using UnityEngine;

public class CoverSpawner : MonoBehaviour
{
    [Header("Prefabs de Coberturas")]
    [Tooltip("Arrastra aquí todos los prefabs de la carpeta CoberturasScale")]
    [SerializeField] private GameObject[] coverPrefabs;

    [Header("Configuración del Mapa AR")]
    [Tooltip("Cantidad de coberturas a generar en la partida")]
    [SerializeField] private int numberOfCovers = 8;

    [Tooltip("Radio de dispersión desde el centro del mapa (en metros)")]
    [SerializeField] private float spawnRadius = 2.0f;

    [Tooltip("Distancia mínima entre coberturas para evitar superposiciones")]
    [SerializeField] private float minDistanceBetween = 0.5f;

    [Tooltip("Distancia limpia alrededor del punto de origen del tanque")]
    [SerializeField] private float safeZoneRadius = 0.8f;

    /// <summary>
    /// Genera coberturas aleatorias manteniendo la misma altura Y que el plano/tanque.
    /// Llama a este método al detectar el marcador AR o al iniciar la partida.
    /// </summary>
    public void SpawnCovers(Vector3 centerPoint)
    {
        if (coverPrefabs == null || coverPrefabs.Length == 0)
        {
            Debug.LogWarning("CoverSpawner: No hay prefabs asignados en el array de coberturas.");
            return;
        }

        List<Vector3> spawnedPositions = new List<Vector3>();
        int attempts = 0;
        int spawnedCount = 0;

        while (spawnedCount < numberOfCovers && attempts < 150)
        {
            attempts++;

            // Generar posición aleatoria en un disco horizontal (X, Z)
            Vector2 randomPoint = Random.insideUnitCircle * spawnRadius;

            // Fijar la coordenada Y exactamente al mismo nivel que el punto de referencia (tanque/tablero)
            Vector3 candidatePos = new Vector3(
                centerPoint.x + randomPoint.x,
                centerPoint.y,
                centerPoint.z + randomPoint.y
            );

            // 1. Validar que no aparezca en la zona inicial del tanque
            if (Vector3.Distance(candidatePos, centerPoint) < safeZoneRadius)
            {
                continue;
            }

            // 2. Validar distancia contra coberturas ya generadas
            bool positionValid = true;
            foreach (Vector3 pos in spawnedPositions)
            {
                if (Vector3.Distance(candidatePos, pos) < minDistanceBetween)
                {
                    positionValid = false;
                    break;
                }
            }

            if (positionValid)
            {
                // Seleccionar prefab aleatorio del catálogo
                GameObject prefabToSpawn = coverPrefabs[Random.Range(0, coverPrefabs.Length)];

                // Rotación aleatoria únicamente en Y para variedad estética
                Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                // Instanciar como hijo del Spawner
                GameObject spawnedCover = Instantiate(prefabToSpawn, candidatePos, randomRotation, transform);

                spawnedPositions.Add(candidatePos);
                spawnedCount++;
            }
        }
    }
}