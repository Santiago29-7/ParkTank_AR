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

    [Header("Configuración de Autocolocación")]
    public bool autoPlaceOnPlaneDetected = true;

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
            // 1. Crear el Tanque sobre el suelo detectado
            spawnedObject = Instantiate(objectToPlace, position, rotation);
            hasSpawned = true;

            // 2. Crear la Caja frente al tanque en el MISMO plano Y
            if (powerUpBoxPrefab != null)
            {
                // Posicionar 1.5 metros adelante del tanque
                Vector3 boxPosition = position + (spawnedObject.transform.forward * 1.5f);

                // Mantiene exactamente la misma altura Y del piso AR
                boxPosition.y = position.y + 0.05f;

                Instantiate(powerUpBoxPrefab, boxPosition, Quaternion.identity);
            }

            Debug.Log("Tanque y Caja colocados en el mismo plano Y.");
        }
    }
}