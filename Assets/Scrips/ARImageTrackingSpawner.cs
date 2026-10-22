using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Photon.Pun;

public class ARImageTrackingSpawner : MonoBehaviour
{
    [Header("Referencias AR")]
    public ARTrackedImageManager trackedImageManager;
    public GameObject arenaPrefab; // Objeto de la arena / mapa

    [Header("Photon Config")]
    [Tooltip("El Prefab del tanque DEBE estar dentro de Assets/Resources/")]
    public string tankPrefabName = "TankPrefab";

    private bool hasSpawned = false;

    void OnEnable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
        {
            SpawnNetworkObjects(trackedImage.transform);
        }
        foreach (var trackedImage in eventArgs.updated)
        {
            if (!hasSpawned && trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
            {
                SpawnNetworkObjects(trackedImage.transform);
            }
        }
    }

    void SpawnNetworkObjects(Transform imageTransform)
    {
        if (hasSpawned) return;
        hasSpawned = true;

        // 1. Instanciar la arena localmente sobre el código QR
        if (arenaPrefab != null)
        {
            Instantiate(arenaPrefab, imageTransform.position, imageTransform.rotation);
        }

        // 2. Calcular posición del tanque según el rol del jugador (Anfitrión a la izquierda, Cliente a la derecha)
        Vector3 offset = PhotonNetwork.IsMasterClient ? new Vector3(-0.3f, 0.05f, 0f) : new Vector3(0.3f, 0.05f, 0f);
        Vector3 spawnPosition = imageTransform.TransformPoint(offset);
        Quaternion spawnRotation = imageTransform.rotation;

        // 3. Instanciar el tanque en red usando Photon
        PhotonNetwork.Instantiate(tankPrefabName, spawnPosition, spawnRotation);
    }
}