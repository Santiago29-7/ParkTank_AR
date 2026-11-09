using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun; // Librería de Photon para soporte de red y multijugador

/// <summary>
/// Gestiona la interfaz del menú de opciones y pausa durante la partida.
/// Controla la congelación del tiempo (Time.timeScale), el volumen maestro global,
/// la alternancia de íconos de audio y el flujo de confirmación para salir al menú principal.
/// </summary>
public class GameOptionsManager : MonoBehaviourPunCallbacks
{
    // ==========================================
    // REFERENCIAS DE PANELES UI
    // ==========================================

    [Header("Paneles UI")]
    [Tooltip("Panel principal del HUD (controles, joysticks, barras) que se oculta al pausar")]
    public GameObject hudPanel;

    [Tooltip("Panel de opciones/pausa emergente con sliders de audio y configuración")]
    public GameObject optionsPanel;

    [Tooltip("Modal de confirmación de advertencia al intentar abandonar la partida")]
    public GameObject exitConfirmationPanel;

    // ==========================================
    // CONTROLES DE AUDIO Y ELEMENTOS VISUALES
    // ==========================================

    [Header("Controles de Sonido")]
    [Tooltip("Slider UI para ajustar el volumen maestro global del juego")]
    public Slider volumeSlider;

    [Tooltip("Componente Image del botón de silencio/sonido para cambiar su ícono")]
    public Image soundButtonImage;

    [Tooltip("Sprite del ícono de sonido activado")]
    public Sprite iconSoundOn;

    [Tooltip("Sprite del ícono de sonido deshabilitado / silenciado")]
    public Sprite iconSoundDisabled;

    // Almacena el último volumen válido antes de silenciar para poder restaurarlo
    private float lastVolume = 1f;

    // ==========================================
    // INICIALIZACIÓN
    // ==========================================

    void Start()
    {
        // Garantiza que la simulación del tiempo inicie en velocidad normal (1x)
        Time.timeScale = 1f;

        // Oculta los paneles de opciones y confirmación al arrancar la escena
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (exitConfirmationPanel != null) exitConfirmationPanel.SetActive(false);

        // Inicializa el slider con el nivel de volumen global de Unity y suscribe el evento de cambio
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        // Actualiza el estado inicial del ícono visual del sonido
        UpdateSoundUI(AudioListener.volume);
    }

    // ==========================================
    // PAUSA Y CONTROL DEL TIEMPO
    // ==========================================

    /// <summary>
    /// Pausa el juego congelando la escala de tiempo (Time.timeScale = 0),
    /// oculta los controles en pantalla (HUD) y abre la ventana de opciones.
    /// </summary>
    public void OpenOptions()
    {
        Time.timeScale = 0f; // Congela movimiento, disparo, animaciones e IA en pantalla
        if (hudPanel != null) hudPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
        if (exitConfirmationPanel != null) exitConfirmationPanel.SetActive(false);
    }

    /// <summary>
    /// Reanuda la simulación del juego (Time.timeScale = 1),
    /// oculta los paneles de menú y restablece la visibilidad del HUD.
    /// </summary>
    public void CloseOptions()
    {
        Time.timeScale = 1f; // Reanuda el tiempo y la simulación del motor
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (exitConfirmationPanel != null) exitConfirmationPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
    }

    // ==========================================
    // LÓGICA DE CONTROL DE VOLUMEN Y AUDIO
    // ==========================================

    /// <summary>
    /// Evento disparado automáticamente cuando el jugador arrastra la barra del slider de volumen.
    /// </summary>
    /// <param name="value">Nuevo valor de volumen entre 0.0 y 1.0.</param>
    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;

        // Si el usuario arrastra la barra completamente a cero
        if (value <= 0.001f)
        {
            AudioListener.volume = 0f;
            if (volumeSlider != null) volumeSlider.interactable = false; // Deshabilita la interacción del slider
            UpdateSoundUI(0f);
        }
        else
        {
            lastVolume = value; // Guarda el volumen actual como referencia de restauración
            UpdateSoundUI(value);
        }
    }

    /// <summary>
    /// Método vinculado al botón de silencio/activación de audio.
    /// Alterna el volumen entre 0 y el último valor registrado antes de silenciar.
    /// </summary>
    public void ToggleSoundButton()
    {
        // Si el volumen actual es cero o la barra está bloqueada -> Reactiva el sonido
        if (AudioListener.volume <= 0f || (volumeSlider != null && !volumeSlider.interactable))
        {
            // Restaura el último volumen guardado (o 1.0f si el registro previo era cero)
            AudioListener.volume = lastVolume > 0.05f ? lastVolume : 1f;

            if (volumeSlider != null)
            {
                volumeSlider.interactable = true; // Reactiva la interactividad del slider
                volumeSlider.value = AudioListener.volume;
            }

            UpdateSoundUI(AudioListener.volume);
        }
        else
        {
            // Si el sonido estaba activo -> Silencia y deshabilita temporalmente el slider
            lastVolume = AudioListener.volume;
            AudioListener.volume = 0f;

            if (volumeSlider != null)
            {
                volumeSlider.value = 0f;
                volumeSlider.interactable = false; // Bloquea el slider en silencio
            }

            UpdateSoundUI(0f);
        }
    }

    /// <summary>
    /// Actualiza el sprite del botón de audio (On / Off) según el nivel de volumen actual.
    /// </summary>
    /// <param name="currentVolume">Nivel actual de volumen en AudioListener.</param>
    private void UpdateSoundUI(float currentVolume)
    {
        if (soundButtonImage == null) return;

        if (currentVolume <= 0f)
        {
            if (iconSoundDisabled != null) soundButtonImage.sprite = iconSoundDisabled;
        }
        else
        {
            if (iconSoundOn != null) soundButtonImage.sprite = iconSoundOn;
        }
    }

    // ==========================================
    // FLUJO DE CONFIRMACIÓN Y SALIDA
    // ==========================================

    /// <summary>
    /// Despliega el modal emergente de confirmación de salida ("¿Deseas salir?").
    /// </summary>
    public void ShowExitConfirmation()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (exitConfirmationPanel != null) exitConfirmationPanel.SetActive(true);
    }

    /// <summary>
    /// Cierra el modal de confirmación y regresa a la vista de opciones principal.
    /// </summary>
    public void HideExitConfirmation()
    {
        if (exitConfirmationPanel != null) exitConfirmationPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    /// <summary>
    /// Confirma la salida de la partida. Restablece la velocidad del tiempo y
    /// abandona la sala de red de Photon o carga directamente el menú principal.
    /// </summary>
    public void ConfirmExitGame()
    {
        Time.timeScale = 1f; // Restablece el tiempo antes de cambiar de escena

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.LeaveRoom(); // Desconecta la sala multijugador (disparará OnLeftRoom)
        }
        else
        {
            SceneManager.LoadScene("MainMenu"); // Salida directa en modo solitario / local
        }
    }

    /// <summary>
    /// Callback de Photon que se invoca automáticamente al completar la salida de una sala en red.
    /// </summary>
    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("MainMenu");
    }
}