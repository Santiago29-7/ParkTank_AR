using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LoadingScreenManager : MonoBehaviour
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

    void Start()
    {
        if (bgmAudioSource != null && bgmClip != null)
        {
            bgmAudioSource.clip = bgmClip;
            bgmAudioSource.Play();
        }

        StartCoroutine(LoadSceneRoutine());
        StartCoroutine(CycleTipsRoutine());
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
                progressText.text = Mathf.RoundToInt(progress * 100f) + "%";
            }

            yield return null;
        }

        if (progressBar != null) progressBar.value = 1f;
        if (progressText != null) progressText.text = "100%";

        yield return new WaitForSeconds(0.5f);
        asyncLoad.allowSceneActivation = true;
    }
}