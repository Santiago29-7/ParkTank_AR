using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controlador principal de la escena de Menú Principal (MainMenu).
/// Gestiona la tasa de refresco (FPS) del dispositivo, el registro inicial y persistencia del Nickname del jugador,
/// las advertencias de seguridad para Realidad Aumentada, la secuencia animada de créditos y la transición de escenas.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    // ==========================================
    // CONFIGURACIÓN DE RENDIMIENTO Y ESCENAS
    // ==========================================

    void Awake()
    {
        // Desactiva la sincronización vertical (VSync) para permitir fijar la tasa de cuadros por código
        QualitySettings.vSyncCount = 0;

        // Limita la ejecución a 60 FPS estables en Android para evitar sobrecalentamiento y ahorrar batería
        Application.targetFrameRate = 60;
    }

    [Header("Nombres de las Escenas")]
    [Tooltip("Nombre exacto de la escena de juego / escaneo QR de Realidad Aumentada")]
    public string singleplayerSceneName = "SampleScene";

    // ==========================================
    // PERSISTENCIA Y REGISTRO DE JUGADOR
    // ==========================================

    [Header("Referencias Persistencia y Nickname")]
    [Tooltip("Referencia al administrador de base de datos local (PlayerPrefs)")]
    public DatabaseManager dbManager;

    [Tooltip("Panel modal para ingresar el Nickname la primera vez que se abre la app")]
    public GameObject nicknamePanel;

    [Tooltip("Contenedor de los botones principales del menú (JUGAR, LOGROS, CRÉDITOS, SALIR)")]
    public GameObject mainButtonsPanel;

    [Tooltip("Campo de texto (InputField) donde el jugador escribe su apodo")]
    public TMP_InputField nicknameInputField;

    [Tooltip("Texto MeshPro que despliega el saludo personalizado de bienvenida")]
    public TMP_Text welcomeText;

    // ==========================================
    // PANELES AUXILIARES Y ADVERTENCIAS AR
    // ==========================================

    [Header("Paneles Adicionales")]
    [Tooltip("Panel modal de advertencias de seguridad en Realidad Aumentada (Cuidado con obstáculos reales)")]
    public GameObject warningsPanel;

    [Tooltip("Panel contenedor de la secuencia de créditos")]
    public GameObject creditsPanel;

    // ==========================================
    // CONFIGURACIÓN DE ANIMACIÓN DE CRÉDITOS
    // ==========================================

    [Header("Configuración de Texto de Créditos")]
    [Tooltip("Transform del texto largo de créditos que se desplazará verticalmente")]
    public RectTransform creditsTextRect;

    [Tooltip("Velocidad normal de desplazamiento hacia arriba (en píxeles por segundo)")]
    public float scrollSpeed = 60f;

    [Tooltip("Multiplicador de velocidad al mantener presionada la pantalla para acelerar los créditos")]
    public float fastForwardMultiplier = 4f;

    [Tooltip("Posición inicial Y fuera de pantalla (por abajo) donde inicia el texto de créditos")]
    public float startPositionY = -400f;

    [Tooltip("Posición final Y fuera de pantalla (por arriba) donde termina el texto de créditos")]
    public float endPositionY = 500f;

    [Header("Configuración de Pantalla de Agradecimiento")]
    [Tooltip("CanvasGroup del logo/texto final '¡Gracias por jugar!' para efectos de transparencia Fade In/Out")]
    public CanvasGroup thanksCanvasGroup;

    [Tooltip("Duración en segundos de las animaciones de desvanecimiento (Fade In / Fade Out)")]
    public float fadeDuration = 1.2f;

    [Tooltip("Tiempo en segundos que el mensaje de agradecimiento se mantiene estático en pantalla")]
    public float thanksDisplayTime = 3f;

    private Coroutine creditsCoroutine; // Referencia a la corrutina activa de créditos para poder cancelarla si el usuario sale

    // ==========================================
    // INICIALIZACIÓN
    // ==========================================

    void Start()
    {
        // Auto-detectar la base de datos si no fue asignada en el Inspector
        if (dbManager == null) dbManager = FindFirstObjectByType<DatabaseManager>();

        // Desactivar paneles secundarios al iniciar la escena
        if (warningsPanel != null) warningsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);

        // Ocultar y volver transparente la pantalla de agradecimiento
        if (thanksCanvasGroup != null)
        {
            thanksCanvasGroup.alpha = 0f;
            thanksCanvasGroup.gameObject.SetActive(false);
        }

        // Consultar en la base de datos si ya existe un Nickname registrado previamente
        string savedNickname = (dbManager != null) ? dbManager.GetNickname() : "";

        // Si es la primera vez que inicia o el nickname es el valor por defecto "Jugador", exige el registro
        if (string.IsNullOrEmpty(savedNickname) || savedNickname == "Jugador")
        {
            if (nicknamePanel != null) nicknamePanel.SetActive(true);
            if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        }
        else
        {
            // Si ya cuenta con nickname guardado, habilita los botones del menú y actualiza la bienvenida
            if (nicknamePanel != null) nicknamePanel.SetActive(false);
            if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
            ApplyNickname(savedNickname);
        }
    }

    // ==========================================
    // FLUJO DE REGISTRO DE NICKNAME
    // ==========================================

    /// <summary>
    /// Evento vinculado al botón de confirmar Nickname.
    /// Valida que el texto no esté vacío, lo almacena de forma persistente y habilita el menú principal.
    /// </summary>
    public void OnConfirmNickname()
    {
        if (nicknameInputField == null) return;

        string enteredName = nicknameInputField.text.Trim();

        // Cancela el proceso si el jugador no escribió nada
        if (string.IsNullOrEmpty(enteredName)) return;

        // Guarda el nombre en PlayerPrefs mediante DatabaseManager
        if (dbManager != null)
        {
            dbManager.SaveNickname(enteredName);
        }

        // Despliega el nombre en pantalla y conmuta los paneles
        ApplyNickname(enteredName);

        if (nicknamePanel != null) nicknamePanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
    }

    /// <summary>
    /// Formatea y muestra el saludo de bienvenida en la UI con el nombre en mayúsculas.
    /// </summary>
    private void ApplyNickname(string name)
    {
        if (welcomeText != null)
        {
            welcomeText.text = "¡BIENVENIDO, " + name.ToUpper() + "!";
        }
    }

    // ==========================================
    // FLUJO DE ADVERTENCIAS DE SEGURIDAD AR
    // ==========================================

    /// <summary>
    /// Muestra la pantalla de advertencias de seguridad antes de iniciar la experiencia en AR.
    /// </summary>
    public void ShowWarningsPanel()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (warningsPanel != null) warningsPanel.SetActive(true);
    }

    /// <summary>
    /// Cancela la vista de advertencias y retorna a los botones del menú principal.
    /// </summary>
    public void HideWarningsPanel()
    {
        if (warningsPanel != null) warningsPanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
    }

    /// <summary>
    /// Invocado cuando el usuario acepta las advertencias de seguridad AR. Carga la escena de juego.
    /// </summary>
    public void AcceptWarningsAndPlay()
    {
        Debug.Log("[MainMenu] Advertencias aceptadas. Cargando la escena de juego/QR...");
        SceneManager.LoadScene(singleplayerSceneName);
    }

    // ==========================================
    // FLUJO Y ANIMACIÓN COMPLETA DE CRÉDITOS
    // ==========================================

    /// <summary>
    /// Activa la vista de créditos e inicia la corrutina de animación vertical y agradecimiento.
    /// </summary>
    public void ShowCreditsPanel()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);

        if (creditsCoroutine != null) StopCoroutine(creditsCoroutine);
        creditsCoroutine = StartCoroutine(FullCreditsSequenceRoutine());
    }

    /// <summary>
    /// Detiene la secuencia de créditos y regresa de inmediato al menú principal.
    /// </summary>
    public void HideCreditsPanel()
    {
        if (creditsCoroutine != null) StopCoroutine(creditsCoroutine);

        if (thanksCanvasGroup != null)
        {
            thanksCanvasGroup.alpha = 0f;
            thanksCanvasGroup.gameObject.SetActive(false);
        }

        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
    }

    /// <summary>
    /// Corrutina completa que maneja la secuencia de créditos:
    /// 1. Desplazamiento vertical del texto desde startPositionY hasta endPositionY.
    /// 2. Detección de toque táctil/mouse para acelerar el desplazamiento (Fast Forward).
    /// 3. Aparecer progresivo (Fade In) de la imagen de agradecimiento.
    /// 4. Desvanecimiento (Fade Out) y retorno automático al menú principal.
    /// </summary>
    private IEnumerator FullCreditsSequenceRoutine()
    {
        // 1. Resetea el panel de agradecimiento
        if (thanksCanvasGroup != null)
        {
            thanksCanvasGroup.alpha = 0f;
            thanksCanvasGroup.gameObject.SetActive(false);
        }

        // 2. Animación de desplazamiento del texto hacia arriba
        if (creditsTextRect != null)
        {
            creditsTextRect.gameObject.SetActive(true);
            Vector2 currentPos = creditsTextRect.anchoredPosition;
            currentPos.y = startPositionY; // Posiciona el texto en el punto de inicio inferior
            creditsTextRect.anchoredPosition = currentPos;

            while (currentPos.y < endPositionY)
            {
                // Detecta si el usuario mantiene presionada la pantalla o el clic izquierdo para acelerar
                bool isPressing = Input.GetMouseButton(0) || Input.touchCount > 0;
                float currentSpeed = isPressing ? (scrollSpeed * fastForwardMultiplier) : scrollSpeed;

                currentPos.y += currentSpeed * Time.deltaTime;
                creditsTextRect.anchoredPosition = currentPos;
                yield return null;
            }

            creditsTextRect.gameObject.SetActive(false);
        }

        // 3. Aparecer suavemente (Fade In) el mensaje "GRACIAS POR JUGAR"
        if (thanksCanvasGroup != null)
        {
            thanksCanvasGroup.gameObject.SetActive(true);
            float timer = 0f;

            while (timer < fadeDuration)
            {
                bool isPressing = Input.GetMouseButton(0) || Input.touchCount > 0;
                float timeDelta = isPressing ? (Time.deltaTime * fastForwardMultiplier) : Time.deltaTime;

                timer += timeDelta;
                thanksCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            thanksCanvasGroup.alpha = 1f;

            // 4. Mantener visible durante el tiempo configurado (acelerable)
            float displayTimer = 0f;
            while (displayTimer < thanksDisplayTime)
            {
                bool isPressing = Input.GetMouseButton(0) || Input.touchCount > 0;
                float timeDelta = isPressing ? (Time.deltaTime * fastForwardMultiplier) : Time.deltaTime;

                displayTimer += timeDelta;
                yield return null;
            }

            // 5. Desvanecer suavemente (Fade Out)
            timer = 0f;
            while (timer < fadeDuration)
            {
                bool isPressing = Input.GetMouseButton(0) || Input.touchCount > 0;
                float timeDelta = isPressing ? (Time.deltaTime * fastForwardMultiplier) : Time.deltaTime;

                timer += timeDelta;
                thanksCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                yield return null;
            }
            thanksCanvasGroup.alpha = 0f;
        }

        // 6. Regresar al menú principal automáticamente al finalizar toda la secuencia
        HideCreditsPanel();
    }

    // ==========================================
    // CIERRE DE LA APLICACIÓN
    // ==========================================

    /// <summary>
    /// Cierra la aplicación de forma limpia en compilaciones ejecutables o móviles.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[MainMenu] Cerrando aplicación.");
        Application.Quit();
    }
}