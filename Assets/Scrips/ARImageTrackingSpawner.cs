using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARImageTrackingSpawner : MonoBehaviour
{
    [Header("UI de Instrucciones y Controles")]
    public GameObject qrInstructionPanel; // Recuadro "Apunta la cámara al código QR"
    public GameObject hudPanel;           // Contenedor del HUD (joysticks, salud, etc.)

    [Header("Referencias AR y Prefabs")]
    public ARTrackedImageManager trackedImageManager;
    public GameObject playerTankPrefab;    // Prefab del tanque del jugador local
    public GameObject arenaPrefab;

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
                SpawnLocalObjects(trackedImage.transform);
            }
        }

        foreach (var trackedImage in eventArgs.updated)
        {
            if (!hasSpawned && trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
            {
                SpawnLocalObjects(trackedImage.transform);
            }
        }
    }

    void SpawnLocalObjects(Transform imageTransform)
    {
        if (hasSpawned) return;
        hasSpawned = true;

        // Ocultar mensaje QR y encender la interfaz del juego
        if (qrInstructionPanel != null) qrInstructionPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);

        Vector3 spawnPosition = imageTransform.position;
        Quaternion spawnRotation = imageTransform.rotation;

        if (playerTankPrefab != null)
        {
            GameObject playerTank = Instantiate(playerTankPrefab, spawnPosition, spawnRotation);

            // Garantizar la etiqueta del jugador para la detección de la IA y los Power-Ups
            if (!playerTank.CompareTag("Player"))
            {
                playerTank.tag = "Player";
            }
        }
        else
        {
            Debug.LogError("ARImageTrackingSpawner: No has asignado el 'playerTankPrefab' en el Inspector.");
        }

        // --- GENERACIÓN AUTOMÁTICA DE COBERTURAS AR ---
        CoverSpawner coverSpawner = FindFirstObjectByType<CoverSpawner>();
        if (coverSpawner != null)
        {
            coverSpawner.SpawnCovers(imageTransform.position);
        }
        else
        {
            Debug.LogWarning("ARImageTrackingSpawner: No se encontró el objeto CoverSpawner en la escena.");
        }
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