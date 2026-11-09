using System.Collections;
using UnityEngine;

/// <summary>
/// Controla la animación 3D del fogonazo/chispazo (Muzzle Flash) al disparar un cañón.
/// Modifica aleatoriamente la rotación en el eje Z y escala progresivamente el modelo 
/// usando una curva senoidal para simular la expansión y contracción de la llama de disparo.
/// </summary>
public class MuzzleFlash3D : MonoBehaviour
{
    // ==========================================
    // PARÁMETROS DE ANIMACIÓN Y TAMAÑO
    // ==========================================

    [Header("Parámetros de Animación")]
    [Tooltip("Duración en segundos del destello visual del cañón")]
    public float flashDuration = 0.2f; // Aumentado a 0.2s para garantizar visibilidad en pantalla

    [Tooltip("Escala base original asignada al objeto 3D")]
    public Vector3 baseScale;

    private Coroutine flashCoroutine; // Referencia a la corrutina activa para reiniciar animaciones superpuestas

    // ==========================================
    // INICIALIZACIÓN
    // ==========================================

    void Awake()
    {
        // Captura automáticamente las dimensiones originales asignadas al objeto en el Editor 3D
        baseScale = transform.localScale;

        // Si la escala inicial guardada es cero, asigna (1,1,1) como valor seguro
        if (baseScale == Vector3.zero)
        {
            baseScale = Vector3.one;
        }
    }

    // ==========================================
    // ACTIVACIÓN DEL EFECTO
    // ==========================================

    /// <summary>
    /// Invoca el destello del disparo. Interrumpe cualquier animación en curso 
    /// y reactiva el GameObject para iniciar una nueva secuencia visual.
    /// </summary>
    public void TriggerFlash()
    {
        // Detiene la corrutina anterior si se dispara a una velocidad superior a la duración del destello
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        gameObject.SetActive(true);
        flashCoroutine = StartCoroutine(AnimateFlash());
    }

    // ==========================================
    // CORRUTINA DE ANIMACIÓN SENOIDAL
    // ==========================================

    /// <summary>
    /// Maneja el ciclo de vida del fogonazo:
    /// 1. Asigna una rotación aleatoria en Z para dinamismo.
    /// 2. Escala el objeto de 0 a 1 y de vuelta a 0 usando la función Mathf.Sin.
    /// 3. Desactiva el objeto al finalizar.
    /// </summary>
    private IEnumerator AnimateFlash()
    {
        // Asigna una rotación aleatoria en el eje Z (0° a 360°) para variaciones estéticas por cada disparo
        transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        float elapsedTime = 0f;

        // Bucle de animación paso a paso
        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / flashDuration; // Progreso normalizado de 0.0 a 1.0

            // CURVA SENOIDAL DE ESCALADO:
            // Mathf.Sin(progress * PI) genera un valor suave que inicia en 0, alcanza 1 en la mitad del tiempo y regresa a 0
            float scaleFactor = Mathf.Sin(progress * Mathf.PI);
            transform.localScale = baseScale * scaleFactor;

            yield return null; // Espera al siguiente frame
        }

        // Apaga el objeto para optimizar el rendimiento cuando termina la animación
        gameObject.SetActive(false);
    }
}