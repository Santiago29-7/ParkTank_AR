using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla la escena inicial de presentación (Splash Screen / Pantalla de bienvenida).
/// Gestiona la secuencia animada de entrada y salida mediante interpolación de transparencia (Fade In / Fade Out)
/// utilizando un CanvasGroup, y transiciona automáticamente hacia la escena de carga principal.
/// </summary>
public class SplashScreenController : MonoBehaviour
{
    // ==========================================
    // REFERENCIAS DE INTERFAZ DE USUARIO (UI)
    // ==========================================

    [Header("Referencias UI")]
    [Tooltip("Arrastra aquí el objeto SplashContent que contiene el CanvasGroup del logotipo de presentación")]
    public CanvasGroup splashCanvasGroup;

    // ==========================================
    // CONFIGURACIÓN DE TIEMPOS DE TRANSICIÓN
    // ==========================================

    [Header("Tiempos de Transición")]
    [Tooltip("Tiempo en segundos para aparecer suavemente (Fade In)")]
    public float fadeInDuration = 1.5f;

    [Tooltip("Tiempo en segundos que permanece el logotipo 100% visible en pantalla")]
    public float displayDuration = 2.0f;

    [Tooltip("Tiempo en segundos para desaparecer suavemente (Fade Out)")]
    public float fadeOutDuration = 1.5f;

    // ==========================================
    // NAVEGACIÓN DE ESCENAS
    // ==========================================

    [Header("Escena Siguiente")]
    [Tooltip("Nombre exacto de la escena de carga a la que se transicionará al finalizar")]
    public string loadingSceneName = "LoadingScene";

    // ==========================================
    // INICIALIZACIÓN
    // ==========================================

    private void Start()
    {
        // Garantiza que la UI inicie completamente invisible (Alpha = 0) antes de animar
        if (splashCanvasGroup != null)
        {
            splashCanvasGroup.alpha = 0f;
            StartCoroutine(PlaySplashScreenSequence());
        }
        else
        {
            Debug.LogError("SplashScreenController: Falta asignar el CanvasGroup en el Inspector.");
        }
    }

    // ==========================================
    // SECUENCIA DE TRANSICIÓN (CORRUTINA)
    // ==========================================

    /// <summary>
    /// Ejecuta de forma secuencial las fases del Splash Screen:
    /// 1. Fade In: Incrementa progresivamente el canal Alpha de 0.0 a 1.0.
    /// 2. Pausa: Mantiene el logo en pantalla por el tiempo configurado en 'displayDuration'.
    /// 3. Fade Out: Reduce progresivamente el canal Alpha de 1.0 a 0.0.
    /// 4. Carga de Escena: Redirige automáticamente a la escena de carga.
    /// </summary>
    private IEnumerator PlaySplashScreenSequence()
    {
        // 1. FADE IN (Aparecer suavemente)
        float timer = 0f;
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            splashCanvasGroup.alpha = Mathf.Clamp01(timer / fadeInDuration);
            yield return null; // Espera al siguiente cuadro
        }
        splashCanvasGroup.alpha = 1f; // Garantiza opacidad total al finalizar el ciclo

        // 2. PAUSA (Mantener el logo visible en pantalla)
        yield return new WaitForSeconds(displayDuration);

        // 3. FADE OUT (Desaparecer suavemente)
        timer = 0f;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            splashCanvasGroup.alpha = Mathf.Clamp01(1f - (timer / fadeOutDuration));
            yield return null; // Espera al siguiente cuadro
        }
        splashCanvasGroup.alpha = 0f; // Garantiza transparencia total al finalizar

        // 4. CAMBIO A LA PANTALLA DE CARGA
        SceneManager.LoadScene(loadingSceneName);
    }
}