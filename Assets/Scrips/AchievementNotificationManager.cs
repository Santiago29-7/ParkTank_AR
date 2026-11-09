using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementNotificationManager : MonoBehaviour
{
    // ==========================================
    // PATRÓN SINGLETON Y COMPONENTES DE UI
    // ==========================================

    public static AchievementNotificationManager Instance; // Instancia única global para llamar las notificaciones desde cualquier script

    [Header("UI Toast Components")]
    public GameObject toastPanel;            // Panel principal de la notificación desplegable
    public CanvasGroup toastCanvasGroup;    // Controla la transparencia (alpha) para el efecto Fade In / Fade Out
    public Image toastIcon;                  // Imagen del ícono del logro dentro de la notificación
    public TextMeshProUGUI toastTitleText;   // Texto del título del logro desbloqueado

    [Header("Color Sprites (Order 1-5)")]
    public Sprite[] unlockedSprites;         // Arreglo con los íconos a color de los 5 logros (orden 1 a 5)

    // ==========================================
    // CONFIGURACIÓN DE ANIMACIÓN Y TIEMPOS
    // ==========================================

    [Header("Ajustes de Tiempo y Animación")]
    [Tooltip("Tiempo de espera solo para el logro de inicio de partida")]
    public float startDelay = 1.5f;          // Retraso inicial para dar tiempo a que cargue la escena antes de notificar

    [Tooltip("Distancia en Y desde la cual caerá la notificación")]
    public float slideOffsetY = 150f;        // Desfase vertical desde donde aparece el banner deslizante

    public float animDuration = 0.4f;        // Duración en segundos de la animación de entrada y salida
    public float displayTime = 3.0f;         // Tiempo en segundos que la notificación permanece fija en pantalla

    // ==========================================
    // DATOS INTERNOS DE LOS LOGROS
    // ==========================================

    // Títulos de los logros que se mostrarán en la notificación flotante
    private string[] titles = new string[]
    {
        "La primera vez nunca se olvida",
        "¿Alguien trajo escoba y recogedor?",
        "¿Quién dejó todo esto botado?",
        "Pura habilidad... y un poco de suerte",
        "Tutorial de como NO jugar"
    };

    // Umbrales requeridos para cada logro (deben coincidir exactamente con AchievementDisplayManager)
    private float[] thresholds = new float[] { 1f, 15f, 10f, 5f, 1f };

    // Control interno de animaciones y posiciones UI
    private Coroutine activeToastCoroutine;      // Referencia a la corrutina actual para evitar sobreposición de notificaciones
    private RectTransform toastRectTransform;    // Referencia al transform del panel UI para moverlo suavemente
    private Vector2 targetAnchoredPosition;      // Posición final donde reposa la notificación en pantalla
    private Vector2 startAnchoredPosition;       // Posición inicial fuera de pantalla (por encima del objetivo)

    // ==========================================
    // INICIALIZACIÓN
    // ==========================================

    private void Awake()
    {
        // Configuración del patrón Singleton para asegurar una sola instancia en toda la ejecución
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Obtención de referencias y cálculo de posiciones para la animación de caída
        if (toastPanel != null)
        {
            toastRectTransform = toastPanel.GetComponent<RectTransform>();
            if (toastRectTransform != null)
            {
                targetAnchoredPosition = toastRectTransform.anchoredPosition; // Guarda la posición asignada en el Canvas
                startAnchoredPosition = targetAnchoredPosition + new Vector2(0, slideOffsetY); // Calcula el punto de partida superior
            }
        }
    }

    private void Start()
    {
        // Aseguramos que el panel inicie oculto y completamente transparente
        if (toastPanel != null) toastPanel.SetActive(false);
        if (toastCanvasGroup != null) toastCanvasGroup.alpha = 0f;

        // Comprobación automática al cargar la escena por si se alcanzó un logro durante el juego
        CheckAchievements();
    }

    // ==========================================
    // LÓGICA DE COMPROBACIÓN Y NOTIFICACIÓN
    // ==========================================

    /// <summary>
    /// Recorre todos los logros, verifica si se alcanzaron las metas y no se han notificado anteriormente.
    /// </summary>
    public void CheckAchievements()
    {
        for (int i = 0; i < thresholds.Length; i++)
        {
            float currentValue = GetStatisticValue(i);
            bool reachedThreshold = currentValue >= thresholds[i];

            // Revisa en PlayerPrefs si la bandera de notificación ya fue registrada
            bool alreadyNotified = PlayerPrefs.GetInt("Notified_Achievement_" + i, 0) == 1;

            if (reachedThreshold && !alreadyNotified)
            {
                // Guarda la bandera persistentemente para no volver a notificar este logro en el futuro
                PlayerPrefs.SetInt("Notified_Achievement_" + i, 1);
                PlayerPrefs.Save();

                // Lanza el banner animado en pantalla
                TriggerToast(i);
                break; // Notifica uno a la vez para evitar colisiones de UI
            }
        }
    }

    /// <summary>
    /// Cancela animaciones previas e inicia la corrutina de visualización para el logro especificado.
    /// </summary>
    /// <param name="achievementIndex">Índice del logro a notificar (0 a 4).</param>
    private void TriggerToast(int achievementIndex)
    {
        if (activeToastCoroutine != null) StopCoroutine(activeToastCoroutine);
        activeToastCoroutine = StartCoroutine(ShowToastCoroutine(achievementIndex));
    }

    // ==========================================
    // CORRUTINA DE ANIMACIÓN Y TRANSICIÓN DE UI
    // ==========================================

    /// <summary>
    /// Maneja todo el ciclo de vida visual de la notificación: Entrada (Fade in + Desplazamiento), Espera y Salida.
    /// </summary>
    private IEnumerator ShowToastCoroutine(int index)
    {
        // Aplica un retardo opcional solo al logro de bienvenida (índice 0)
        if (index == 0 && startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        // Asignación de imagen del logro según el índice
        if (toastIcon != null && index < unlockedSprites.Length)
        {
            toastIcon.sprite = unlockedSprites[index];
        }

        // Asignación del título del logro
        if (toastTitleText != null && index < titles.Length)
        {
            toastTitleText.text = titles[index];
        }

        // Activa el objeto en jerarquía y resetea su posición fuera de pantalla
        toastPanel.SetActive(true);
        if (toastRectTransform != null) toastRectTransform.anchoredPosition = startAnchoredPosition;

        // --------------------------------------------------
        // FASE 1: ENTRADA (Deslizar hacia abajo + Fade In)
        // --------------------------------------------------
        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t); // Suavizado de aceleración/desaceleración (SmoothStep)

            toastCanvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothT); // Aumenta la opacidad progresivamente
            if (toastRectTransform != null)
            {
                // Interpola la posición desde la parte superior hacia la posición destino
                toastRectTransform.anchoredPosition = Vector2.Lerp(startAnchoredPosition, targetAnchoredPosition, smoothT);
            }
            yield return null;
        }

        // Asegura valores finales exactos tras completar la entrada
        toastCanvasGroup.alpha = 1f;
        if (toastRectTransform != null) toastRectTransform.anchoredPosition = targetAnchoredPosition;

        // --------------------------------------------------
        // FASE 2: PERMANENCIA EN PANTALLA
        // --------------------------------------------------
        yield return new WaitForSeconds(displayTime);

        // --------------------------------------------------
        // FASE 3: SALIDA (Deslizar hacia arriba + Fade Out)
        // --------------------------------------------------
        elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            toastCanvasGroup.alpha = Mathf.Lerp(1f, 0f, smoothT); // Reduce la opacidad progresivamente
            if (toastRectTransform != null)
            {
                // Regresa el panel hacia la parte superior fuera de pantalla
                toastRectTransform.anchoredPosition = Vector2.Lerp(targetAnchoredPosition, startAnchoredPosition, smoothT);
            }
            yield return null;
        }

        // Restaura los valores iniciales y oculta el GameObject para optimizar el rendimiento
        toastCanvasGroup.alpha = 0f;
        if (toastRectTransform != null) toastRectTransform.anchoredPosition = startAnchoredPosition;
        toastPanel.SetActive(false);
    }

    // ==========================================
    // MÉTODOS AUXILIARES Y HERRAMIENTAS
    // ==========================================

    /// <summary>
    /// Mapea el índice del logro con la función de lectura estadística en DatabaseManager.
    /// </summary>
    private float GetStatisticValue(int index)
    {
        switch (index)
        {
            case 0: return DatabaseManager.GetGamesPlayed();       // Partidas Jugadas
            case 1: return DatabaseManager.GetTanksDestroyed();    // Tanques Destruidos
            case 2: return DatabaseManager.GetPowerUpsCollected(); // Power-Ups Recolectados
            case 3: return DatabaseManager.GetGamesWon();          // Partidas Ganadas
            case 4: return DatabaseManager.GetGamesLost();         // Partidas Perdidas
            default: return 0f;
        }
    }

    /// <summary>
    /// Herramienta de pruebas ejecutable desde el Inspector de Unity (clic derecho sobre el componente).
    /// Elimina las banderas de notificaciones enviadas para poder probar la UI nuevamente.
    /// </summary>
    [ContextMenu("Resetear Notificaciones de Logros")]
    public void ResetNotifications()
    {
        for (int i = 0; i < 5; i++)
        {
            PlayerPrefs.DeleteKey("Notified_Achievement_" + i);
        }
        PlayerPrefs.Save();
        Debug.Log("Banderas de notificación reseteadas con éxito.");
    }
}