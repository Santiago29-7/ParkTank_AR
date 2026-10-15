using System.Collections;
using UnityEngine;
using TMPro;

public class PopupNotification : MonoBehaviour
{
    public static PopupNotification Instance;

    [Header("Configuración de Duración (Inspector)")]
    [Tooltip("Tiempo en segundos que el texto permanece 100% visible")]
    public float defaultDisplayDuration = 3.0f; // Cambia este valor para más o menos tiempo

    [Tooltip("Tiempo en segundos que tarda en desvanecerse por completo")]
    public float defaultFadeDuration = 1.0f;

    private TMP_Text textComponent;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        Instance = this;
        textComponent = GetComponent<TMP_Text>();
        if (textComponent != null)
        {
            textComponent.text = ""; // Iniciar invisible
        }
    }

    // Sobrecarga para llamadas simples desde otros scripts
    public void ShowMessage(string message, Color textColor)
    {
        ShowMessage(message, textColor, defaultDisplayDuration, defaultFadeDuration);
    }

    // Método completo por si quieres pasarle tiempos personalizados puntuales
    public void ShowMessage(string message, Color textColor, float displayDuration, float fadeDuration)
    {
        if (textComponent == null) return;

        // Si ya había un mensaje desvaneciéndose, reiniciarlo
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeOutRoutine(message, textColor, displayDuration, fadeDuration));
    }

    private IEnumerator FadeOutRoutine(string message, Color textColor, float displayDuration, float fadeDuration)
    {
        textComponent.text = message;
        textComponent.color = new Color(textColor.r, textColor.g, textColor.b, 1f);

        // Mantiene el texto 100% visible durante displayDuration segundos
        yield return new WaitForSeconds(displayDuration);

        // Desvanecimiento suave del canal Alpha (1 a 0)
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            textComponent.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
            yield return null;
        }

        textComponent.text = "";
    }
}