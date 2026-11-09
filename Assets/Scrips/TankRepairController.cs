using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestiona la mecánica manual de reparación del tanque del jugador en tiempo real.
/// Mientras el jugador mantenga presionado el botón de reparación (PointerDown/PointerUp),
/// el tanque queda inmovilizado temporalmente, desprendiendo una interfaz radial de progreso 
/// con una llave inglesa animada (rotación 360°) que restaura salud periódicamente por ciclos.
/// </summary>
public class TankRepairController : MonoBehaviour
{
    // ==========================================
    // CONFIGURACIÓN DE REPARACIÓN Y TIEMPOS
    // ==========================================

    [Header("Configuración de Reparación")]
    [Tooltip("Tiempo en segundos necesario para completar 1 ciclo completo de reparación")]
    public float repairCycleTime = 2.5f;

    [Tooltip("Puntos de salud (HP) otorgados al tanque tras completar exitosamente cada ciclo")]
    public float healAmountPerCycle = 10f;

    // ==========================================
    // REFERENCIAS DE INTERFAZ DE USUARIO (UI)
    // ==========================================

    [Header("Referencias UI")]
    [Tooltip("Objeto contenedor del panel emergente de reparación en la UI (RepairUIPanel)")]
    public GameObject repairUIPanel;

    [Tooltip("Imagen UI tipo Filled Radial (RepairProgressImage) para visualizar la carga del ciclo")]
    public Image repairProgressImage;

    [Tooltip("Transform del icono de la herramienta (WrenchIcon) para animar su rotación")]
    public RectTransform wrenchIcon;

    // ==========================================
    // REFERENCIAS AL TANQUE DEL JUGADOR
    // ==========================================

    [Header("Referencias del Tanque")]
    [Tooltip("Componente TankHealth del jugador para modificar los puntos de vida")]
    public TankHealth tankHealth;

    [Tooltip("Componente TankController del jugador para bloquear/desbloquear el movimiento durante la reparación")]
    public TankController tankController;

    // ==========================================
    // ESTADO INTERNO Y CORRUTINAS
    // ==========================================

    private bool isHoldingRepair = false; // Indica si el usuario mantiene pulsado el botón táctil de reparación
    private Coroutine repairCoroutine;    // Referencia al bucle de reparación activo para su control y cancelación

    // ==========================================
    // INICIALIZACIÓN
    // ==========================================

    void Start()
    {
        // Asegura que el panel de UI de reparación inicie oculto
        if (repairUIPanel != null)
        {
            repairUIPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Localiza automáticamente el tanque del jugador en la escena si las referencias no fueron asignadas en el Inspector.
    /// </summary>
    private void FindLocalTank()
    {
        if (tankController == null || tankHealth == null)
        {
            TankController tank = FindFirstObjectByType<TankController>();
            if (tank != null)
            {
                tankController = tank;
                tankHealth = tank.GetComponent<TankHealth>();
            }
        }
    }

    // ==========================================
    // EVENTOS DE INTERACCIÓN TÁCTIL (POINTER)
    // ==========================================

    /// <summary>
    /// Evento invocado al presionar el botón de reparación en pantalla táctil (OnPointerDown).
    /// Valida que el tanque requiera reparación e inicia la corrutina de ciclo.
    /// </summary>
    public void OnPointerDownRepair()
    {
        FindLocalTank();

        // Cancela si no hay tanque o si la salud ya se encuentra al 100%
        if (tankHealth == null || tankHealth.currentHealth >= tankHealth.maxHealth) return;

        isHoldingRepair = true;

        // Detiene cualquier corrutina previa para evitar ejecuciones superpuestas
        if (repairCoroutine != null)
            StopCoroutine(repairCoroutine);

        repairCoroutine = StartCoroutine(RepairRoutine());
    }

    /// <summary>
    /// Evento invocado al soltar el botón de reparación (OnPointerUp).
    /// Cancela la corrutina activa y restablece los controles y la interfaz.
    /// </summary>
    public void OnPointerUpRepair()
    {
        isHoldingRepair = false;

        if (repairCoroutine != null)
        {
            StopCoroutine(repairCoroutine);
            repairCoroutine = null;
        }

        ResetRepairState();
    }

    // ==========================================
    // CORRUTINA DE CICLO DE REPARACIÓN
    // ==========================================

    /// <summary>
    /// Maneja el bucle continuo de reparación:
    /// 1. Inmoviliza el chasis del tanque.
    /// 2. Despliega la UI radial y anima la herramienta girando 360°.
    /// 3. Al completar el tiempo por ciclo, otorga salud y reinicia la carga si se sigue presionando.
    /// </summary>
    private IEnumerator RepairRoutine()
    {
        // Inmoviliza el tanque para impedir desplazamientos durante las tareas de mantenimiento
        if (tankController != null)
        {
            tankController.SetCanMove(false);
        }

        // Muestra el panel emergente de reparación en la UI
        if (repairUIPanel != null)
        {
            repairUIPanel.SetActive(true);
        }

        // Bucle continuo mientras el jugador mantenga presionado el botón
        while (isHoldingRepair)
        {
            // Si la salud se reabastece por completo, interrumpe el bucle
            if (tankHealth != null && tankHealth.currentHealth >= tankHealth.maxHealth)
            {
                break;
            }

            float timer = 0f;
            if (repairProgressImage != null) repairProgressImage.fillAmount = 0f;

            // Transición del ciclo individual de reparación
            while (timer < repairCycleTime && isHoldingRepair)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / repairCycleTime);

                // Actualización del llenado radial (0.0 a 1.0)
                if (repairProgressImage != null)
                {
                    repairProgressImage.fillAmount = progress;
                }

                // Giro de 360° en sentido horario sincronizado exactamente con la carga del progreso
                if (wrenchIcon != null)
                {
                    wrenchIcon.localRotation = Quaternion.Euler(0f, 0f, -progress * 360f);
                }

                yield return null; // Espera al siguiente frame
            }

            // Al completarse el ciclo sin soltar el botón, otorga la curación
            if (timer >= repairCycleTime && isHoldingRepair)
            {
                if (tankHealth != null)
                {
                    tankHealth.HealAmount(healAmountPerCycle);
                }

                // Restablece indicadores visuales para el siguiente ciclo continuo
                if (repairProgressImage != null)
                {
                    repairProgressImage.fillAmount = 0f;
                }

                if (wrenchIcon != null)
                {
                    wrenchIcon.localRotation = Quaternion.identity;
                }
            }
        }

        // Restablece el estado general al finalizar la reparación
        ResetRepairState();
    }

    // ==========================================
    // RESTABLECIMIENTO DE ESTADO Y LIMPIEZA
    // ==========================================

    /// <summary>
    /// Restaura la capacidad de movimiento del tanque, oculta la UI de reparación 
    /// y reinicia la orientación del icono de la herramienta.
    /// </summary>
    private void ResetRepairState()
    {
        // Reactiva la capacidad de movimiento del chasis
        if (tankController != null)
        {
            tankController.SetCanMove(true);
        }

        // Oculta el panel emergente
        if (repairUIPanel != null)
        {
            repairUIPanel.SetActive(false);
        }

        // Restablece el llenado radial y la rotación del icono
        if (repairProgressImage != null)
        {
            repairProgressImage.fillAmount = 0f;
        }

        if (wrenchIcon != null)
        {
            wrenchIcon.localRotation = Quaternion.identity;
        }
    }
}