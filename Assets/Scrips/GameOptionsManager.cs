using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class GameOptionsManager : MonoBehaviourPunCallbacks
{
    [Header("Paneles UI")]
    public GameObject hudPanel;
    public GameObject optionsPanel;
    public GameObject exitConfirmationPanel;

    [Header("Controles de Sonido")]
    public Slider volumeSlider;
    public Image soundButtonImage;       // Componente Image del objeto BtnSound
    public Sprite iconSoundOn;          // Arrastrar sprite 'icon_sound'
    public Sprite iconSoundDisabled;    // Arrastrar sprite 'icon_sound_disabled'

    private float lastVolume = 1f;

    void Start()
    {
        Time.timeScale = 1f;
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (exitConfirmationPanel != null) exitConfirmationPanel.SetActive(false);

        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        UpdateSoundUI(AudioListener.volume);
    }

    // --- ABRIR / CERRAR Y PAUSAR EL JUEGO ---
    public void OpenOptions()
    {
        Time.timeScale = 0f; // Congela movimiento, disparo e IA
        if (hudPanel != null) hudPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
        if (exitConfirmationPanel != null) exitConfirmationPanel.SetActive(false);
    }

    public void CloseOptions()
    {
        Time.timeScale = 1f; // Reanuda la simulación del juego
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (exitConfirmationPanel != null) exitConfirmationPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
    }

    // --- LÓGICA DEL SLIDER Y BOTÓN DE SONIDO ---
    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;

        // Si el jugador baja el slider por completo a 0
        if (value <= 0.001f)
        {
            AudioListener.volume = 0f;
            if (volumeSlider != null) volumeSlider.interactable = false; // Deshabilita el slider
            UpdateSoundUI(0f);
        }
        else
        {
            lastVolume = value;
            UpdateSoundUI(value);
        }
    }

    public void ToggleSoundButton()
    {
        // Si el sonido está en 0 o el slider está deshabilitado -> Se reactiva
        if (AudioListener.volume <= 0f || (volumeSlider != null && !volumeSlider.interactable))
        {
            AudioListener.volume = lastVolume > 0.05f ? lastVolume : 1f;

            if (volumeSlider != null)
            {
                volumeSlider.interactable = true; // Habilita el slider
                volumeSlider.value = AudioListener.volume;
            }

            UpdateSoundUI(AudioListener.volume);
        }
        else
        {
            // Si estaba sonando -> Se silencia y bloquea el slider
            lastVolume = AudioListener.volume;
            AudioListener.volume = 0f;

            if (volumeSlider != null)
            {
                volumeSlider.value = 0f;
                volumeSlider.interactable = false; // Deshabilita el slider
            }

            UpdateSoundUI(0f);
        }
    }

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

    // --- SALIDA Y ADVERTENCIA ---
    public void ShowExitConfirmation()
    {
        if (exitConfirmationPanel != null) exitConfirmationPanel.SetActive(true);
    }

    public void HideExitConfirmation()
    {
        if (exitConfirmationPanel != null) exitConfirmationPanel.SetActive(false);
    }

    public void ConfirmExitGame()
    {
        Time.timeScale = 1f;
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("MainMenu");
    }
}