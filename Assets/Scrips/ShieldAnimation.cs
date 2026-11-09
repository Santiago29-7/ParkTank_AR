using UnityEngine;

/// <summary>
/// Controla la animación visual del escudo protector (Shield) alrededor del tanque.
/// Aplica una rotación continua sobre sus ejes locales y genera un efecto de pulsación/respiración 
/// mediante una onda senoidal sobre su escala base para mayor dinamismo visual.
/// </summary>
public class ShieldAnimation : MonoBehaviour
{
    // ==========================================
    // CONFIGURACIÓN DE ROTACIÓN Y PULSACIÓN
    // ==========================================

    [Header("Configuración de Rotación")]
    [Tooltip("Velocidad de giro continuo en grados por segundo sobre los ejes X, Y y Z locales")]
    public Vector3 rotationSpeed = new Vector3(0f, 40f, 0f); // Velocidad de giro en Y por defecto

    [Header("Efecto de Pulsación (Respiración)")]
    [Tooltip("Habilita o deshabilita la animación de escalado periódico (latido)")]
    public bool enablePulse = true;

    [Tooltip("Frecuencia o velocidad con la que el escudo se agranda y encoge")]
    public float pulseSpeed = 3f;

    [Tooltip("Amplitud o porcentaje de variación de la escala (cuánto se expande/contrae)")]
    public float pulseAmount = 0.04f;

    // ==========================================
    // ESTADO INTERNO
    // ==========================================

    private Vector3 baseScale; // Almacena las dimensiones originales del objeto asignadas en el Editor

    // ==========================================
    // INICIALIZACIÓN Y CICLO DE VIDA
    // ==========================================

    void Awake()
    {
        // Guarda la escala inicial del objeto antes de comenzar cualquier animación
        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        // Restablece la escala base cada vez que el escudo o componente se activa en escena
        if (baseScale != Vector3.zero)
        {
            transform.localScale = baseScale;
        }
    }

    // ==========================================
    // BUCLE DE ACTUALIZACIÓN VISUAL
    // ==========================================

    void Update()
    {
        // 1. Rotación continua del escudo sobre sus propios ejes locales (Space.Self)
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);

        // 2. Movimiento de latido/respiración usando una función senoidal
        if (enablePulse)
        {
            // Mathf.Sin genera una oscilación suave entre -1 y 1 multiplicada por la amplitud (pulseAmount)
            float scaleFactor = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = baseScale * scaleFactor;
        }
    }
}