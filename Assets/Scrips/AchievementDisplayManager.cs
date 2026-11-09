using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementDisplayManager : MonoBehaviour
{
    // ==========================================
    // REFERENCIAS DE LA INTERFAZ DE USUARIO (UI)
    // ==========================================

    [Header("Contenedores de Vista Internos")]
    public GameObject mainContent;  // Referencia al panel principal que contiene los botones y estadísticas
    public GameObject detailPanel;  // Referencia al panel emergente que muestra los detalles de un logro específico

    [Header("Achievement Buttons in ProgressPanel (Order 1-5)")]
    public Button[] achievementButtons; // Arreglo que guarda los 5 botones de los logros en la interfaz

    [Header("Color Sprites (Unlocked - Order 1-5)")]
    public Sprite[] unlockedSprites; // Imágenes a color que se muestran cuando el logro está completado

    [Header("Grayscale Sprites (Locked - Order 1-5)")]
    public Sprite[] lockedSprites;   // Imágenes en blanco y negro que se muestran cuando el logro está bloqueado

    [Header("UI Pop-up Detalle Logro")]
    public Image detailIconImage;                 // Imagen grande del logro dentro del panel de detalles
    public TextMeshProUGUI detailTitleText;       // Texto del título del logro en el detalle
    public TextMeshProUGUI detailDescriptionText; // Texto descriptivo sobre cómo obtener el logro
    public TextMeshProUGUI detailStatusText;      // Texto que indica si está "DESBLOQUEADO" o "BLOQUEADO"

    // ==========================================
    // DATOS DE LOS LOGROS
    // ==========================================

    // Títulos de los 5 logros del juego
    private string[] titles = new string[]
    {
        "La primera vez nunca se olvida",
        "¿Alguien trajo escoba y recogedor?",
        "¿Quién dejó todo esto botado?",
        "Pura habilidad... y un poco de suerte",
        "Tutorial de como NO jugar"
    };

    // Descripciones de lo que el jugador debe hacer para desbloquearlos
    private string[] descriptions = new string[]
    {
        "Juega tu primera partida.",
        "Destruye un total de 15 tanques enemigos.",
        "Recolecta 10 Power-Ups.",
        "Consigue un total de 5 victorias.",
        "Sufre tu primera derrota en combate."
    };

    // Valores meta que el jugador debe alcanzar para ganar cada logro (ej: 1 partida, 15 tanques, etc.)
    private float[] thresholds = new float[] { 1f, 15f, 10f, 5f, 1f };

    // ==========================================
    // MÉTODOS DEL SCRIPT
    // ==========================================

    /// <summary>
    /// Se ejecuta automáticamente cada vez que el panel que contiene este script se activa en pantalla.
    /// </summary>
    private void OnEnable()
    {
        UpdateAchievementVisuals(); // Actualiza las imágenes (color/escala de grises) de los botones
        SetupButtonListeners();     // Conecta los botones a sus funciones

        // Al abrir la ventana, nos aseguramos de mostrar siempre la lista principal y ocultar el detalle
        if (mainContent != null) mainContent.SetActive(true);
        if (detailPanel != null) detailPanel.SetActive(false);
    }

    /// <summary>
    /// Le asigna a cada botón del logro la función de abrir su ventana de detalles correspondiente.
    /// </summary>
    private void SetupButtonListeners()
    {
        for (int i = 0; i < achievementButtons.Length; i++)
        {
            int index = i; // Guardamos el índice actual en una variable local para que el listener funcione correctamente

            if (achievementButtons[i] != null)
            {
                achievementButtons[i].onClick.RemoveAllListeners(); // Limpiamos eventos previos por seguridad
                achievementButtons[i].onClick.AddListener(() => OpenAchievementDetail(index)); // Asignamos el clic
            }
        }
    }

    /// <summary>
    /// Comprueba el progreso del jugador y cambia las imágenes de los botones a color o blanco y negro.
    /// </summary>
    public void UpdateAchievementVisuals()
    {
        for (int i = 0; i < achievementButtons.Length; i++)
        {
            // Medida de seguridad por si faltan configurar imágenes o valores en el inspector
            if (i >= thresholds.Length || i >= unlockedSprites.Length || i >= lockedSprites.Length) break;

            float currentValue = GetStatisticValue(i);             // Obtenemos el progreso actual desde la base de datos
            bool isUnlocked = currentValue >= thresholds[i];       // Comparamos si ya superó la meta del logro

            Image btnImage = achievementButtons[i].GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.color = Color.white; // Reseteamos el color por si acaso
                // Asignamos la imagen a color si está desbloqueado, o en grises si sigue bloqueado
                btnImage.sprite = isUnlocked ? unlockedSprites[i] : lockedSprites[i];
            }
        }
    }

    /// <summary>
    /// Abre el panel de detalles y carga toda la información del logro seleccionado.
    /// </summary>
    /// <param name="index">El número identificador (0 al 4) del logro presionado.</param>
    public void OpenAchievementDetail(int index)
    {
        // Evita errores si se envía un índice que no existe
        if (index < 0 || index >= titles.Length) return;

        float currentValue = GetStatisticValue(index);
        bool isUnlocked = currentValue >= thresholds[index]; // Comprueba si el logro fue completado

        // Actualizamos la imagen grande del detalle
        if (detailIconImage != null)
        {
            detailIconImage.sprite = isUnlocked ? unlockedSprites[index] : lockedSprites[index];
        }

        // Asignamos el texto del título y la descripción
        if (detailTitleText != null) detailTitleText.text = titles[index];
        if (detailDescriptionText != null) detailDescriptionText.text = descriptions[index];

        // Configuramos el texto de estado y le ponemos color (Verde = Desbloqueado, Rojo = Bloqueado)
        if (detailStatusText != null)
        {
            detailStatusText.text = isUnlocked ? "¡DESBLOQUEADO!" : "BLOQUEADO";
            detailStatusText.color = isUnlocked ? Color.green : Color.red;
        }

        // Transición de UI: Ocultamos el contenido principal y mostramos el panel de detalle
        if (mainContent != null) mainContent.SetActive(false);
        if (detailPanel != null) detailPanel.SetActive(true);
    }

    /// <summary>
    /// Cierra el panel de detalles y regresa a la vista de la lista de logros.
    /// </summary>
    public void CloseAchievementDetail()
    {
        // Transición de UI: Oculta el detalle y restaura la vista principal
        if (detailPanel != null) detailPanel.SetActive(false);
        if (mainContent != null) mainContent.SetActive(true);
    }

    /// <summary>
    /// Función auxiliar que conecta el índice del logro con la estadística correspondiente en la base de datos.
    /// </summary>
    /// <param name="statisticIndex">Índice del logro (0 al 4).</param>
    /// <returns>El valor numérico de esa estadística en específico.</returns>
    private float GetStatisticValue(int statisticIndex)
    {
        switch (statisticIndex)
        {
            case 0: return DatabaseManager.GetGamesPlayed();       // Logro 1: Partidas Jugadas
            case 1: return DatabaseManager.GetTanksDestroyed();    // Logro 2: Tanques Destruidos
            case 2: return DatabaseManager.GetPowerUpsCollected(); // Logro 3: Power-Ups Recolectados
            case 3: return DatabaseManager.GetGamesWon();          // Logro 4: Victorias
            case 4: return DatabaseManager.GetGamesLost();         // Logro 5: Derrotas
            default: return 0f;
        }
    }
}