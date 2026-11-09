using UnityEngine;
using UnityEngine.XR.ARFoundation; // Librería de AR Foundation para detección e interacción en Realidad Aumentada

public class ARImageTrackingSpawner : MonoBehaviour
{
    // ==========================================
    // REFERENCIAS DE INTERFAZ Y PREFABS
    // ==========================================

    [Header("UI de Instrucciones y Controles")]
    public GameObject qrInstructionPanel; // Panel inicial que le pide al jugador apuntar la cámara al código QR
    public GameObject hudPanel;           // Panel de controles del juego (Joysticks, vida, etc.) que se activa al iniciar

    [Header("Referencias AR y Prefabs")]
    public ARTrackedImageManager trackedImageManager; // Componente de AR Foundation que detecta la imagen QR
    public GameObject playerTankPrefab;                // Prefab del tanque del jugador a instanciar
    public GameObject arenaPrefab;                    // Prefab opcional para la arena de combate

    // Bandera estática para asegurar que la escena solo genere el tanque y las coberturas UNA sola vez
    private static bool isAlreadyInstantiatedInScene = false;

    // ==========================================
    // INICIALIZACIÓN Y EVENTOS DE AR
    // ==========================================

    private void Awake()
    {
        // Resetea la bandera al cargar la escena
        isAlreadyInstantiatedInScene = false;

        // Auto-detectar el gestor de imágenes AR en la escena si no se asignó manualmente en el Inspector
        if (trackedImageManager == null)
        {
            trackedImageManager = FindFirstObjectByType<ARTrackedImageManager>();
        }
    }

    void Start()
    {
        // Estado inicial de UI: Muestra la guía de escaneo QR y oculta los controles de juego
        if (qrInstructionPanel != null) qrInstructionPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);
    }

    void Update()
    {
#if UNITY_EDITOR
        // MODO PRUEBAS EN PC (EDITOR): Permite simular el escaneo presionando la tecla 'T'
        if (Input.GetKeyDown(KeyCode.T) && !isAlreadyInstantiatedInScene)
        {
            Debug.Log("[EDITOR] Partida iniciada manualmente con la tecla 'T'");
            SpawnLocalObjects(this.transform);
        }
#endif
    }

    /// <summary>
    /// Se ejecuta al activar el script. Suscribe el evento de detección de imágenes de AR Foundation.
    /// </summary>
    void OnEnable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    /// <summary>
    /// Se ejecuta al desactivar el script. Desuscribe el evento para evitar fugas de memoria.
    /// </summary>
    void OnDisable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    // ==========================================
    // DETECCIÓN Y GENERACIÓN DE OBJETOS
    // ==========================================

    /// <summary>
    /// Método delegado que se ejecuta automáticamente cuando la cámara detecta o actualiza una imagen en el espacio AR.
    /// </summary>
    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
#if UNITY_EDITOR
        // BLOQUEO EN PC: Evita que Unity XR Simulation simule un escaneo automático no deseado al abrir la escena
        return;
#else
        // DISPOSITIVO MÓVIL: Procesamiento del escaneo real del código QR físico
        
        // Comprueba si se añadió una nueva imagen detectada
        foreach (var trackedImage in eventArgs.added)
        {
            if (!isAlreadyInstantiatedInScene && trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
            {
                SpawnLocalObjects(trackedImage.transform);
            }
        }

        // Comprueba si se actualizó el rastreo de una imagen existente
        foreach (var trackedImage in eventArgs.updated)
        {
            if (!isAlreadyInstantiatedInScene && trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
            {
                SpawnLocalObjects(trackedImage.transform);
            }
        }
#endif
    }

    /// <summary>
    /// Genera el tanque del jugador y los elementos del mapa sobre las coordenadas exactas de la imagen QR detectada.
    /// </summary>
    /// <param name="imageTransform">Transform de la imagen rastreada que servirá como origen del mapa.</param>
    void SpawnLocalObjects(Transform imageTransform)
    {
        if (isAlreadyInstantiatedInScene) return;
        isAlreadyInstantiatedInScene = true; // Bloquea futuras instanciaciones en esta partida

        // 1. Instanciar el tanque del jugador en la posición y rotación del código QR
        if (playerTankPrefab != null)
        {
            GameObject playerTank = Instantiate(playerTankPrefab, imageTransform.position, imageTransform.rotation);
            if (!playerTank.CompareTag("Player"))
            {
                playerTank.tag = "Player"; // Garantiza la etiqueta 'Player' para la lógica de daño e IA
            }
        }

        // 2. Buscar el spawner de coberturas e instanciar los obstáculos alrededor del mapa
        CoverSpawner coverSpawner = FindFirstObjectByType<CoverSpawner>();
        if (coverSpawner != null)
        {
            coverSpawner.SpawnCovers(imageTransform.position);
        }

        // 3. Transición de UI: Oculta la pantalla de escaneo y activa el HUD con los joysticks
        if (qrInstructionPanel != null) qrInstructionPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);

        // 4. Registro estadístico: Incrementa el contador de partidas jugadas en la base de datos
        DatabaseManager.AddGamePlayed();

        // 5. OPTIMIZACIÓN CRÍTICA DE RENDIMIENTO:
        // Desactiva el rastreo continuo de la cámara una vez creado el mapa para ahorrar batería y ganar FPS
        if (trackedImageManager != null)
        {
            trackedImageManager.enabled = false;
            Debug.Log("[AR Optimización] Escaneo de QR detenido con éxito para ganar FPS.");
        }
    }

    // ==========================================
    // RECONEXIÓN / RESET
    // ==========================================

    /// <summary>
    /// Reinicia los valores del spawner para permitir escanear un nuevo QR si la partida se reinicia.
    /// </summary>
    public void ResetSpawner()
    {
        isAlreadyInstantiatedInScene = false;

        // Reactivar el componente de rastreo de imágenes de AR Foundation
        if (trackedImageManager != null)
        {
            trackedImageManager.enabled = true;
        }

        // Restaura la interfaz inicial
        if (qrInstructionPanel != null) qrInstructionPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);
    }
}