using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun; // 1. Librería de Photon añadida

// 2. Heredamos de MonoBehaviourPunCallbacks para recibir los eventos de red
public class LoadingScreenManager : MonoBehaviourPunCallbacks
{
    [Header("Configuración de la Carga")]
    public string targetSceneName = "MainMenu";
    public float loadingDuration = 12f;

    [Header("Referencias UI")]
    public Slider progressBar;
    public TMP_Text progressText;
    public TMP_Text tipText;                    // Texto para mostrar los consejos

    [Header("Audio")]
    public AudioSource bgmAudioSource;
    public AudioClip bgmClip;

    [Header("Consejos de Juego")]
    public string[] tips = new string[]
    {
        "¡Recoge las cajas del mapa para activar potenciadores!",
        "Mantén la movilidad para no ser un blanco fácil.",
        "Usa las estructuras como cobertura frente a los disparos.",
        "Los potenciadores pueden cambiar el rumbo de la batalla."
    };

    private float tipChangeInterval = 4f; // Cambia de consejo cada 4 segundos
    private bool isPhotonReady = false;

    void Start()
    {
        if (bgmAudioSource != null && bgmClip != null)
        {
            bgmAudioSource.clip = bgmClip;
            bgmAudioSource.Play();
        }

        // 3. Sincronizar escenas e iniciar conexión a servidores de Photon
        PhotonNetwork.AutomaticallySyncScene = true;

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("[LoadingScreenManager] Conectando a Photon en segundo plano...");
        }
        else
        {
            isPhotonReady = true;
        }

        StartCoroutine(LoadSceneRoutine());
        StartCoroutine(CycleTipsRoutine());
    }

    // Callback automático cuando Photon establece conexión
    public override void OnConnectedToMaster()
    {
        Debug.Log("[LoadingScreenManager] ¡Servidor de Photon conectado con éxito!");
        isPhotonReady = true;
    }

    private IEnumerator CycleTipsRoutine()
    {
        if (tips == null || tips.Length == 0 || tipText == null) yield break;

        int index = Random.Range(0, tips.Length);

        while (true)
        {
            tipText.text = tips[index];
            index = (index + 1) % tips.Length;
            yield return new WaitForSeconds(tipChangeInterval);
        }
    }

    private IEnumerator LoadSceneRoutine()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        asyncLoad.allowSceneActivation = false;

        float timer = 0f;

        // Progresar la barra de carga visual durante los 12 segundos
        while (timer < loadingDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / loadingDuration);

            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            if (progressText != null)
            {
                // Corregida multiplicación de porcentaje
                progressText.text = Mathf.RoundToInt(progress * 100f) + "%";
            }

            yield return null;
        }

        // Si terminan los 12 segundos pero el internet es lento y Photon aún no conecta, espera aquí
        if (!isPhotonReady && !PhotonNetwork.IsConnectedAndReady)
        {
            if (progressText != null) progressText.text = "Estableciendo red...";
            while (!PhotonNetwork.IsConnectedAndReady)
            {
                yield return null;
            }
        }

        if (progressBar != null) progressBar.value = 1f;
        if (progressText != null) progressText.text = "100%";

        yield return new WaitForSeconds(0.5f);
        asyncLoad.allowSceneActivation = true;
    }
}