using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))]
public class ARTapToPlaceObject : MonoBehaviour
{
    [Header("Prefabs a Generar")]
    public GameObject objectToPlace;   // Tu Prefab PlayerTank
    public GameObject powerUpBoxPrefab; // Tu Prefab PowerUpBox
    public GameObject[] enemyPrefabs;   // Arreglo para Leopard2A4 y T80B

    [Header("Configuración de Autocolocación")]
    public bool autoPlaceOnPlaneDetected = true;

    [Header("Configuración de Enemigos")]
    public float enemySpawnDistance = 2.0f; // Distancia desde el jugador (en metros)

    private ARRaycastManager raycastManager;
    private GameObject spawnedObject;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool hasSpawned = false;

    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
        if (hasSpawned) return;

        // Raycast desde el centro de la pantalla hacia el plano AR
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;

            if (autoPlaceOnPlaneDetected)
            {
                SpawnObjects(hitPose.position, hitPose.rotation);
            }
        }
    }

    void SpawnObjects(Vector3 position, Quaternion rotation)
    {
        if (objectToPlace != null && spawnedObject == null)
        {
            // 1. Crear el Tanque Jugador sobre el suelo detectado
            spawnedObject = Instantiate(objectToPlace, position, rotation);
            hasSpawned = true;

            // 2. Crear la Caja frente al tanque en el MISMO plano Y
            if (powerUpBoxPrefab != null)
            {
                Vector3 boxPosition = position + (spawnedObject.transform.forward * 1.5f);
                boxPosition.y = position.y + 0.05f;

                Instantiate(powerUpBoxPrefab, boxPosition, Quaternion.identity);
            }

            // 3. Crear los Tanques Enemigos repartidos alrededor en el MISMO plano Y
            if (enemyPrefabs != null && enemyPrefabs.Length > 0)
            {
                for (int i = 0; i < enemyPrefabs.Length; i++)
                {
                    if (enemyPrefabs[i] == null) continue;

                    // Calcular posición formando un semicírculo/abanico frente al jugador
                    float angle = -45f + (90f / Mathf.Max(1, enemyPrefabs.Length - 1)) * i;
                    Vector3 enemyDirection = Quaternion.Euler(0, angle, 0) * spawnedObject.transform.forward;

                    Vector3 enemyPosition = position + (enemyDirection * enemySpawnDistance);
                    enemyPosition.y = position.y; // Mantiene exactamente el nivel del suelo AR

                    // Rotar el enemigo hacia el jugador
                    Quaternion enemyRotation = Quaternion.LookRotation(position - enemyPosition);

                    Instantiate(enemyPrefabs[i], enemyPosition, enemyRotation);
                }
            }

            Debug.Log("Tanque, Caja y Enemigos colocados en el mismo plano Y.");
        }
    }
}