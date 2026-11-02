using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashScreenController : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("Arrastra aquí el objeto SplashContent que contiene el CanvasGroup")]
    public CanvasGroup splashCanvasGroup;

    [Header("Tiempos de Transición")]
    [Tooltip("Tiempo en segundos para aparecer (Fade In)")]
    public float fadeInDuration = 1.5f;

    [Tooltip("Tiempo que permanece visible el logo en pantalla")]
    public float displayDuration = 2.0f;

    [Tooltip("Tiempo en segundos para desaparecer (Fade Out)")]
    public float fadeOutDuration = 1.5f;

    [Header("Escena Siguiente")]
    [Tooltip("Nombre exacto de la escena de carga")]
    public string loadingSceneName = "LoadingScene";

    private void Start()
    {
        // Asegurar que inicie completamente invisible
        if (splashCanvasGroup != null)
        {
            splashCanvasGroup.alpha = 0f;
            StartCoroutine(PlaySplashScreenSequence());
        }
        else
        {
            Debug.LogError("SplashScreenController: Falta asignar el CanvasGroup.");
        }
    }

    private IEnumerator PlaySplashScreenSequence()
    {
        // 1. FADE IN (Aparecer suavemente)
        float timer = 0f;
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            splashCanvasGroup.alpha = Mathf.Clamp01(timer / fadeInDuration);
            yield return null;
        }
        splashCanvasGroup.alpha = 1f;

        // 2. PAUSA (Mantener en pantalla)
        yield return new WaitForSeconds(displayDuration);

        // 3. FADE OUT (Desaparecer suavemente)
        timer = 0f;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            splashCanvasGroup.alpha = Mathf.Clamp01(1f - (timer / fadeOutDuration));
            yield return null;
        }
        splashCanvasGroup.alpha = 0f;

        // 4. CAMBIO A LA PANTALLA DE CARGA
        SceneManager.LoadScene(loadingSceneName);
    }
}