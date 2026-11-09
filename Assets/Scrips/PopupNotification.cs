using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Gestiona la visualización de mensajes emergentes (notificaciones flotantes o avisos UI en pantalla).
/// Utiliza un patrón Singleton para llamadas rápidas desde cualquier script, soportando tiempo de lectura 
/// personalizable y un desvanecimiento progresivo (Fade Out) mediante el canal Alpha de TextMeshPro.
/// </summary>
public class PopupNotification : MonoBehaviour
{
    // ==========================================
    // PATRÓN SINGLETON Y PARÁMETROS
    // ==========================================

    public static PopupNotification Instance; // Instancia única global para invocar mensajes desde cualquier script

    [Header("Configuración de Duración (Inspector)")]
    [Tooltip("Tiempo en segundos que el texto permanece 100% visible")]
    public float defaultDisplayDuration = 3.0f; // Duración base de visualización en pantalla antes de iniciar el desvanecimiento

    [Tooltip("Tiempo en segundos que tarda en desvanecerse por completo")]
    public float defaultFadeDuration = 1.0f; // Duración de la transición Fade Out (1.0 = 1 segundo)

    // ==========================================
    // COMPONENTES Y CORRUTINAS
    // ==========================================

    private TMP_Text textComponent;    // Referencia al componente de texto de TextMeshPro
    private Coroutine fadeCoroutine;   // Referencia a la corrutina activa para reiniciar animaciones en caso de nuevo mensaje

    // ==========================================
    // INICIALIZACIÓN
    // ==========================================

    void Awake()
    {
        // Asignación de la instancia Singleton
        Instance = this;

        // Captura del componente TextMeshPro y reseteo de texto inicial a vacío
        textComponent = GetComponent<TMP_Text>();
        if (textComponent != null)
        {
            textComponent.text = ""; // Garantiza que inicie completamente invisible en pantalla
        }
    }

    // ==========================================
    // MÉTODOS PÚBLICOS DE INVOCACIÓN (SOBRECARGAS)
    // ==========================================

    /// <summary>
    /// Muestra un mensaje emergente utilizando los tiempos por defecto configurados en el Inspector.
    /// </summary>
    /// <param name="message">Texto que se desplegará en pantalla.</param>
    /// <param name="textColor">Color base del texto (ej. Color.green, Color.red).</param>
    public void ShowMessage(string message, Color textColor)
    {
        ShowMessage(message, textColor, defaultDisplayDuration, defaultFadeDuration);
    }

    /// <summary>
    /// Muestra un mensaje emergente permitiendo personalizar los tiempos de permanencia y desvanecimiento.
    /// </summary>
    /// <param name="message">Texto que se desplegará en pantalla.</param>
    /// <param name="textColor">Color base del texto.</param>
    /// <param name="displayDuration">Tiempo en segundos que el texto se mantiene opaco.</param>
    /// <param name="fadeDuration">Tiempo en segundos que tarda la transición de transparencia.</param>
    public void ShowMessage(string message, Color textColor, float displayDuration, float fadeDuration)
    {
        if (textComponent == null) return;

        // Si ya había una corrutina de desvanecimiento ejecutándose, la interrumpe para reiniciar con el nuevo mensaje
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Inicia el proceso de visualización y animación
        fadeCoroutine = StartCoroutine(FadeOutRoutine(message, textColor, displayDuration, fadeDuration));
    }

    // ==========================================
    // CORRUTINA DE DESVANECIMIENTO (FADE OUT)
    // ==========================================

    /// <summary>
    /// Maneja el ciclo de vida del aviso en UI:
    /// 1. Asigna el texto y restablece la opacidad al 100% (Alpha = 1).
    /// 2. Aguarda el tiempo estático de lectura.
    /// 3. Interpola el canal Alpha de 1.0 a 0.0 suavemente.
    /// 4. Limpia el componente de texto.
    /// </summary>
    private IEnumerator FadeOutRoutine(string message, Color textColor, float displayDuration, float fadeDuration)
    {
        // Asignación de texto y reseteo del canal Alpha a 1.0 (Completamente visible)
        textComponent.text = message;
        textComponent.color = new Color(textColor.r, textColor.g, textColor.b, 1f);

        // 1. Mantiene el texto estático durante el tiempo configurado
        yield return new WaitForSeconds(displayDuration);

        // 2. Transición suave de desvanecimiento (Fade Out) del canal Alpha (1.0 -> 0.0)
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            textComponent.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
            yield return null; // Espera al siguiente frame
        }

        // 3. Limpia la cadena de texto al finalizar la animación
        textComponent.text = "";
    }
}