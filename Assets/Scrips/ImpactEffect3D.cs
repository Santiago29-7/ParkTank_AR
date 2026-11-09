using System.Collections;
using UnityEngine;

/// <summary>
/// Controla la animación 3D del efecto visual de impacto (VFX) producido al colisionar proyectiles.
/// Aplica un escalado emergente rápido desde cero hasta la escala objetivo para simular un destello 
/// u onda expansiva, y administra la destrucción automática del objeto para liberar memoria.
/// </summary>
public class ImpactEffect3D : MonoBehaviour
{
    // ==========================================
    // CONFIGURACIÓN DE ANIMACIÓN Y TIEMPOS
    // ==========================================

    [Header("Configuración de Animación")]
    [Tooltip("Tiempo total en segundos antes de autodestruir el GameObject en la escena")]
    public float lifetime = 1.0f;

    [Tooltip("Duración en segundos del efecto de expansión inicial (aparición rápida)")]
    public float expandDuration = 0.15f;

    [Tooltip("Escala de respaldo en caso de que la escala original del transform sea (0,0,0)")]
    public Vector3 defaultScale = Vector3.one;

    private Vector3 targetScale; // Almacena las dimensiones finales objetivo que tomará el efecto

    // ==========================================
    // INICIALIZACIÓN
    // ==========================================

    void Awake()
    {
        // Captura la escala original configurada en el prefab o la jerarquía
        targetScale = transform.localScale;

        // Si la escala inicial es cero, asigna la escala por defecto para prevenir que sea invisible
        if (targetScale == Vector3.zero)
        {
            targetScale = defaultScale;
        }
    }

    void Start()
    {
        // Oculta temporalmente el objeto reduciendo su escala a cero antes de animar
        transform.localScale = Vector3.zero;

        // Ejecuta la animación de crecimiento de la onda de impacto
        StartCoroutine(AnimateImpact());

        // Programa la autodestrucción del objeto para no saturar la escena de memoria
        Destroy(gameObject, lifetime);
    }

    // ==========================================
    // CORRUTINA DE ANIMACIÓN (LERP)
    // ==========================================

    /// <summary>
    /// Escala suavemente el objeto desde Vector3.zero hasta la escala objetivo (targetScale)
    /// mediante interpolación lineal durante el tiempo definido en expandDuration.
    /// </summary>
    private IEnumerator AnimateImpact()
    {
        float elapsed = 0f;

        // Bucle de interpolación cuadro a cuadro
        while (elapsed < expandDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / expandDuration;

            // Calcula progresivamente la escala entre cero y el tamaño final deseado
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, progress);
            yield return null; // Espera al siguiente frame
        }

        // Garantiza que al finalizar la animación la escala quede fijada en su valor objetivo exacto
        transform.localScale = targetScale;
    }
}