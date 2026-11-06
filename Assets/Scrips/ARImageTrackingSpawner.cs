using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARImageTrackingSpawner : MonoBehaviour
{
    [Header("UI de Instrucciones y Controles")]
    public GameObject qrInstructionPanel;
    public GameObject hudPanel;

    [Header("Referencias AR y Prefabs")]
    public ARTrackedImageManager trackedImageManager;
    public GameObject playerTankPrefab;
    public GameObject arenaPrefab;

    private static bool isAlreadyInstantiatedInScene = false;

    private void Awake()
    {
        isAlreadyInstantiatedInScene = false;
    }

    void Start()
    {
        if (qrInstructionPanel != null) qrInstructionPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);
    }

    void Update()
    {
#if UNITY_EDITOR
        // En PC (Editor): Solo inicia la partida cuando presiones manualmente la tecla 'T'
        if (Input.GetKeyDown(KeyCode.T) && !isAlreadyInstantiatedInScene)
        {
            Debug.Log("[EDITOR] Partida iniciada manualmente con la tecla 'T'");
            SpawnLocalObjects(this.transform);
        }
#endif
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
#if UNITY_EDITOR
        // BLOQUEO EN PC: Evita que Unity XR Simulation simule un escaneo automático al abrir la escena
        return;
#else
        // DISPOSITIVO MÓVIL: Escaneo real del código QR físico con la cámara
        foreach (var trackedImage in eventArgs.added)
        {
            if (!isAlreadyInstantiatedInScene && trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
            {
                SpawnLocalObjects(trackedImage.transform);
            }
        }

        foreach (var trackedImage in eventArgs.updated)
        {
            if (!isAlreadyInstantiatedInScene && trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
            {
                SpawnLocalObjects(trackedImage.transform);
            }
        }
#endif
    }

    void SpawnLocalObjects(Transform imageTransform)
    {
        if (isAlreadyInstantiatedInScene) return;
        isAlreadyInstantiatedInScene = true;

        // 1. Instanciar el tanque del jugador
        if (playerTankPrefab != null)
        {
            GameObject playerTank = Instantiate(playerTankPrefab, imageTransform.position, imageTransform.rotation);
            if (!playerTank.CompareTag("Player"))
            {
                playerTank.tag = "Player";
            }
        }

        // 2. Generar elementos del mapa/coberturas
        CoverSpawner coverSpawner = FindFirstObjectByType<CoverSpawner>();
        if (coverSpawner != null)
        {
            coverSpawner.SpawnCovers(imageTransform.position);
        }

        // 3. Ocultar pantalla de escaneo y mostrar HUD de juego
        if (qrInstructionPanel != null) qrInstructionPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);

        // 4. AL FINAL: Incrementar la partida en +1 e iniciar el contador para el logro
        DatabaseManager.AddGamePlayed();
    }

    public void ResetSpawner()
    {
        isAlreadyInstantiatedInScene = false;
        if (qrInstructionPanel != null) qrInstructionPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);
    }
}