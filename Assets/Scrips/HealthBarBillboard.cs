using UnityEngine;

/// <summary>
/// Controla el comportamiento de tipo "Billboard" para las barras de salud flotantes (UI World Space).
/// Asegura que el Canvas de vida ubicado sobre el tanque siempre mire hacia la cámara principal 
/// en tiempo real, evitando que el texto o las barras se vean invertidas o de lado cuando la cámara o el tanque se mueven.
/// </summary>
public class HealthBarBillboard : MonoBehaviour
{
    // ==========================================
    // REFERENCIAS INTERNAS
    // ==========================================

    private Transform mainCam; // Referencia al Transform de la cámara principal utilizada como punto focal

    // ==========================================
    // INICIALIZACIÓN
    // ==========================================

    void Start()
    {
        // Localiza y almacena el Transform de la cámara etiquetada como 'MainCamera' al arrancar
        if (Camera.main != null)
            mainCam = Camera.main.transform;
    }

    // ==========================================
    // ACTUALIZACIÓN DE ORIENTACIÓN (LATE UPDATE)
    // ==========================================

    /// <summary>
    /// Se ejecuta en cada frame justo después de que todos los métodos Update() y los cálculos de físicas hayan finalizado.
    /// Garantiza que la orientación de la UI se ajuste sobre la posición final exacta de la cámara.
    /// </summary>
    void LateUpdate()
    {
        // Validación de seguridad: Si la referencia se perdió o la cámara se instanció dinámicamente en AR, intenta reconectarla
        if (mainCam == null)
        {
            if (Camera.main != null) mainCam = Camera.main.transform;
            return;
        }

        // CÁLCULO DE ORIENTACIÓN BILLBOARD:
        // Orienta el transform alineando su eje frontal con la dirección de visión de la cámara y su eje superior con el 'Up' de la misma.
        transform.LookAt(transform.position + mainCam.rotation * Vector3.forward, mainCam.rotation * Vector3.up);
    }
}