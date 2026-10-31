using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Photon.Pun;

public class ARImageTrackingSpawner : MonoBehaviour
{
    [Header("UI de Instrucciones y Controles")]
    public GameObject qrInstructionPanel; // Recuadro "Apunta la cámara al código QR"
    public GameObject hudPanel;           // Contenedor del HUD (joysticks, salud, etc.)

    [Header("Referencias AR")]
    public ARTrackedImageManager trackedImageManager;
    public GameObject arenaPrefab; 

    [Header("Photon Config")]
    public string tankPrefabName = "TankPrefab";

    private bool hasSpawned = false;

    void Start()
    {
        // Estado inicial: Solicitar código QR
        if (qrInstructionPanel != null) qrInstructionPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);
    }

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
        // Solo instanciar si no ha aparecido y la cámara detecta/actualiza el rastreo del QR
        foreach (var trackedImage in eventArgs.added)
        {
            if (!hasSpawned && trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
            {
                SpawnNetworkObjects(trackedImage.transform);
            }
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

        // Ocultar mensaje QR y encender la interfaz del juego
        if (qrInstructionPanel != null) qrInstructionPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);

        Vector3 offset = PhotonNetwork.IsMasterClient ? new Vector3(-0.3f, 0f, 0f) : new Vector3(0.3f, 0f, 0f);
        Vector3 spawnPosition = imageTransform.TransformPoint(offset);
        Quaternion spawnRotation = imageTransform.rotation;

        PhotonNetwork.Instantiate(tankPrefabName, spawnPosition, spawnRotation);
    }

    // --- REINICIAR ESTADO Y OBLIGAR A ESCANEAR EL QR OTRA VEZ ---
    public void ResetSpawner()
    {
        hasSpawned = false;

        // Mostrar pantalla de instrucción QR y ocultar el HUD hasta que se enfoque la tarjeta
        if (qrInstructionPanel != null) qrInstructionPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);
    }
}