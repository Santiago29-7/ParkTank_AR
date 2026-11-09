using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun; // Librería de Photon PUN 2 para la gestión de servicios multijugador y servidores de red

/// <summary>
/// Gestiona la pantalla de carga (Loading Screen) entre escenas.
/// Administra la carga asincrónica de la escena objetivo, la animación progresiva de la barra de carga,
/// el ciclo aleatorio de consejos de juego (tips), la música de fondo y la conexión en segundo plano a Photon.
/// </summary>
public class LoadingScreenManager : MonoBehaviourPunCallbacks
{
    // ==========================================
    // CONFIGURACIÓN DE ESCENA Y TIEMPOS
    // ==========================================

    [Header("Configuración de la Carga")]
    [Tooltip("Nombre exacto de la escena que se cargará al finalizar la barra de progreso")]
    public string targetSceneName = "MainMenu";

    [Tooltip("Tiempo de duración mínimo en segundos que tomará la simulación visual de la barra de carga")]
    public float loadingDuration = 12f;

    // ==========================================
    // REFERENCIAS DE INTERFAZ DE USUARIO (UI)
    // ==========================================

    [Header("Referencias UI")]
    [Tooltip("Slider UI que representa visualmente el avance del porcentaje de carga")]
    public Slider progressBar;

    [Tooltip("Texto MeshPro que despliega el porcentaje formateado (ej. 45%)")]
    public TMP_Text progressText;

    [Tooltip("Texto MeshPro donde se rotarán los consejos de juego en pantalla")]
    public TMP_Text tipText;

    // ==========================================
    // SISTEMA DE AUDIO Y CONSEJOS
    // ==========================================

    [Header("Audio")]
    [Tooltip("Componente AudioSource para reproducir la música de fondo de la pantalla de carga")]
    public AudioSource bgmAudioSource;

    [Tooltip("Pista de audio (BGM) asignada para la pantalla de carga")]
    public AudioClip bgmClip;

    [Header("Consejos de Juego")]
    [Tooltip("Catálogo de frases de ayuda o consejos que se irán mostrando progresivamente")]
    public string[] tips = new string[]
    {
        "¡Recoge las cajas del mapa para activar potenciadores!",
        "Mantén la movilidad para no ser un blanco fácil.",
        "Usa las estructuras como cobertura frente a los disparos.",
        "Los potenciadores pueden cambiar el rumbo de la batalla."
    };

    private float tipChangeInterval = 4f; // Intervalo en segundos entre cada cambio de consejo
    private bool isPhotonReady = false;  // Bandera que confirma si Photon completó la conexión exitosamente

    // ==========================================
    // INICIALIZACIÓN Y CONEXIÓN DE RED
    // ==========================================

    void Start()
    {
        // Configuración y reproducción automática de la música de fondo (BGM)
        if (bgmAudioSource != null && bgmClip != null)
        {
            bgmAudioSource.clip = bgmClip;
            bgmAudioSource.Play();
        }

        // Habilita la sincronización automática de escenas para todos los jugadores en la sala Photon
        PhotonNetwork.AutomaticallySyncScene = true;

        // Verifica el estado de conexión al servidor maestro de Photon
        if (!PhotonNetwork.IsConnected)
        {
            // Inicia la conexión en segundo plano utilizando la configuración predeterminada de Photon
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("[LoadingScreenManager] Conectando a Photon en segundo plano...");
        }
        else
        {
            isPhotonReady = true;
        }

        // Inicia las corrutinas de carga asincrónica y de rotación de consejos
        StartCoroutine(LoadSceneRoutine());
        StartCoroutine(CycleTipsRoutine());
    }

    // ==========================================
    // CALLBACKS DE PHOTON NETWORK
    // ==========================================

    /// <summary>
    /// Callback automático ejecutado por Photon cuando el cliente logra conectarse con éxito al servidor maestro.
    /// </summary>
    public override void OnConnectedToMaster()
    {
        Debug.Log("[LoadingScreenManager] ¡Servidor de Photon conectado con éxito!");
        isPhotonReady = true;
    }

    // ==========================================
    // CORRUTINA DE ROTACIÓN DE CONSEJOS (TIPS)
    // ==========================================

    /// <summary>
    /// Selecciona un consejo aleatorio y lo alterna en la interfaz cada 'tipChangeInterval' segundos.
    /// </summary>
    private IEnumerator CycleTipsRoutine()
    {
        if (tips == null || tips.Length == 0 || tipText == null) yield break;

        // Selecciona un índice inicial aleatorio
        int index = Random.Range(0, tips.Length);

        while (true)
        {
            tipText.text = tips[index];
            index = (index + 1) % tips.Length; // Avance cíclico mediante residuo de división
            yield return new WaitForSeconds(tipChangeInterval);
        }
    }

    // ==========================================
    // CORRUTINA DE CARGA ASINCRÓNICA Y SIMULACIÓN
    // ==========================================

    /// <summary>
    /// Carga la escena objetivo en segundo plano (Async), anima el porcentaje de 0% a 100%,
    /// valida que los servicios de red estén listos y activa la escena final.
    /// </summary>
    private IEnumerator LoadSceneRoutine()
    {
        // Inicia la carga asincrónica de la escena sin activarla inmediatamente
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        asyncLoad.allowSceneActivation = false;

        float timer = 0f;

        // 1. Simulación visual del progreso durante el tiempo definido (loadingDuration)
        while (timer < loadingDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / loadingDuration);

            // Actualización de la barra de progreso
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            // Actualización del texto con porcentaje calculado (0% a 100%)
            if (progressText != null)
            {
                progressText.text = Mathf.RoundToInt(progress * 100f) + "%";
            }

            yield return null;
        }

        // 2. Control de latencia / Red: Si la barra llegó al 100% pero Photon aún no se conecta, aguarda
        if (!isPhotonReady && !PhotonNetwork.IsConnectedAndReady)
        {
            if (progressText != null) progressText.text = "Estableciendo red...";
            while (!PhotonNetwork.IsConnectedAndReady)
            {
                yield return null;
            }
        }

        // 3. Forzar estado final visual
        if (progressBar != null) progressBar.value = 1f;
        if (progressText != null) progressText.text = "100%";

        // Pequeño retardo de medio segundo para dar sensación de fluidez antes de la transición
        yield return new WaitForSeconds(0.5f);

        // Permite la activación e ingreso a la nueva escena
        asyncLoad.allowSceneActivation = true;
    }
}