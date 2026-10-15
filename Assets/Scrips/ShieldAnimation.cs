using UnityEngine;

public class ShieldAnimation : MonoBehaviour
{
    [Header("Configuración de Rotación")]
    public Vector3 rotationSpeed = new Vector3(0f, 40f, 0f); // Velocidad de giro en Y

    [Header("Efecto de Pulsación (Respiración)")]
    public bool enablePulse = true;
    public float pulseSpeed = 3f;      // Qué tan rápido escala
    public float pulseAmount = 0.04f;  // Cuánto se agranda/achica

    private Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        // Restablece la escala base cada vez que se activa el escudo
        if (baseScale != Vector3.zero)
        {
            transform.localScale = baseScale;
        }
    }

    void Update()
    {
        // 1. Rotación continua sobre su propio eje
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);

        // 2. Movimiento de latido/respiración usando una onda senoidal
        if (enablePulse)
        {
            float scaleFactor = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = baseScale * scaleFactor;
        }
    }
}