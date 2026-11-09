using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona la generación procedimental y aleatoria de coberturas (rocas, tumbas, calabazas, etc.)
/// en el campo de batalla de Realidad Aumentada alrededor de la posición del marcador QR.
/// </summary>
public class CoverSpawner : MonoBehaviour
{
    // ==========================================
    // CONFIGURACIÓN DE PREFABS Y PARÁMETROS AR
    // ==========================================

    [Header("Prefabs de Coberturas")]
    [Tooltip("Arrastra aquí todos los prefabs de la carpeta CoberturasScale")]
    [SerializeField] private GameObject[] coverPrefabs; // Arreglo con los modelos 3D de obstáculos disponibles para instanciar

    [Header("Configuración del Mapa AR")]
    [Tooltip("Cantidad de coberturas a generar en la partida")]
    [SerializeField] private int numberOfCovers = 8; // Número total de obstáculos que se intentarán colocar en el mapa

    [Tooltip("Radio de dispersión desde el centro del mapa (en metros)")]
    [SerializeField] private float spawnRadius = 2.0f; // Distancia máxima desde el centro donde pueden aparecer objetos

    [Tooltip("Distancia mínima entre coberturas para evitar superposiciones")]
    [SerializeField] private float minDistanceBetween = 0.5f; // Separación mínima requerida entre dos coberturas

    [Tooltip("Distancia limpia alrededor del punto de origen del tanque")]
    [SerializeField] private float safeZoneRadius = 0.8f; // Área despejada alrededor del jugador para evitar colisiones al nacer

    // ==========================================
    // LÓGICA DE GENERACIÓN DE MAPA
    // ==========================================

    /// <summary>
    /// Genera coberturas aleatorias en un plano horizontal (X, Z) manteniendo la altura Y del punto de referencia.
    /// Este método es invocado automáticamente por ARImageTrackingSpawner al detectar el código QR.
    /// </summary>
    /// <param name="centerPoint">Coordenada central de la escena AR (posición del QR o tanque principal).</param>
    public void SpawnCovers(Vector3 centerPoint)
    {
        // Validación de seguridad: verifica que existan modelos 3D asignados en el Inspector antes de proceder
        if (coverPrefabs == null || coverPrefabs.Length == 0)
        {
            Debug.LogWarning("CoverSpawner: No hay prefabs asignados en el array de coberturas.");
            return;
        }

        List<Vector3> spawnedPositions = new List<Vector3>(); // Lista temporal para almacenar las coordenadas ocupadas
        int attempts = 0;      // Contador de intentos para evitar bucles infinitos si el mapa está lleno
        int spawnedCount = 0;  // Contador de coberturas colocadas exitosamente en la escena

        // Bucle de generación: intenta colocar la cantidad deseada de objetos con un tope máximo de 150 intentos
        while (spawnedCount < numberOfCovers && attempts < 150)
        {
            attempts++;

            // Genera un punto aleatorio dentro de un círculo 2D delimitado por el radio de dispersión
            Vector2 randomPoint = Random.insideUnitCircle * spawnRadius;

            // Convierte el punto 2D a coordenadas 3D, fijando la coordenada Y exacta del origen del plano AR
            Vector3 candidatePos = new Vector3(
                centerPoint.x + randomPoint.x,
                centerPoint.y,
                centerPoint.z + randomPoint.y
            );

            // 1. VALIDACIÓN ZONA SEGURA: Descarta la posición si queda dentro del área inicial del tanque del jugador
            if (Vector3.Distance(candidatePos, centerPoint) < safeZoneRadius)
            {
                continue; // Salta al siguiente intento
            }

            // 2. VALIDACIÓN DE SOLAPAMIENTO: Comprueba la distancia contra coberturas previamente creadas
            bool positionValid = true;
            foreach (Vector3 pos in spawnedPositions)
            {
                if (Vector3.Distance(candidatePos, pos) < minDistanceBetween)
                {
                    positionValid = false; // Descarta si está demasiado cerca de un objeto existente
                    break;
                }
            }

            // Si la coordenada candidata pasó todas las pruebas de validación
            if (positionValid)
            {
                // Selecciona aleatoriamente un modelo de cobertura del catálogo
                GameObject prefabToSpawn = coverPrefabs[Random.Range(0, coverPrefabs.Length)];

                // Genera una rotación aleatoria únicamente en el eje Y (0 a 360°) para mayor variedad estética
                Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                // Instancia el objeto en el mundo asignando este Spawner como objeto padre en la Jerarquía
                GameObject spawnedCover = Instantiate(prefabToSpawn, candidatePos, randomRotation, transform);

                // Registra la posición ocupada y actualiza el contador de éxito
                spawnedPositions.Add(candidatePos);
                spawnedCount++;
            }
        }
    }
}