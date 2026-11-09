using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Controla un Joystick táctil virtual en la interfaz de usuario (UI) para la pantalla táctil de Android.
/// Implementa las interfaces de EventSystems de Unity para procesar gestos táctiles (Down, Drag, Up),
/// soporta multitouch, normaliza los vectores de dirección (Horizontal/Vertical) y dispara eventos al soltar la palanca.
/// </summary>
public class SimpleJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    // ==========================================
    // COMPONENTES Y ESTRUCTURA DEL JOYSTICK
    // ==========================================

    [Header("Componentes del Joystick")]
    [Tooltip("RectTransform que actúa como fondo o límite circular del joystick")]
    public RectTransform bgNode;

    [Tooltip("RectTransform de la perilla o palanca móvil que se desplaza con el dedo")]
    public RectTransform handleNode;

    // ==========================================
    // VARIABLES INTERNAS Y PROPIEDADES DE ENTRADA
    // ==========================================

    private Vector2 inputVector = Vector2.zero; // Vector bidimensional (-1 a 1) de la dirección del joystick
    private Canvas parentCanvas;                 // Referencia al Canvas contenedor para cálculos de coordenadas de pantalla
    private int activePointerId = -100;          // Identificador único del dedo activo para soporte de multitouch

    // Evento C# que se dispara al soltar la palanca tras apuntar (útil para mecánicas de disparo por desenganche)
    public event System.Action OnJoystickReleased;

    /// <summary>
    /// Devuelve el valor del eje horizontal normalizado (-1.0 a 1.0).
    /// </summary>
    public float Horizontal => inputVector.x;

    /// <summary>
    /// Devuelve el valor del eje vertical normalizado (-1.0 a 1.0).
    /// </summary>
    public float Vertical => inputVector.y;

    // ==========================================
    // INICIALIZACIÓN
    // ==========================================

    void Awake()
    {
        // Habilita la detección de múltiples toques simultáneos en el dispositivo móvil
        Input.multiTouchEnabled = true;

        // Si no se asignó manualmente el nodo de fondo, usa el RectTransform del propio objeto
        if (bgNode == null)
            bgNode = GetComponent<RectTransform>();

        // Obtiene la referencia al Canvas contenedor en la jerarquía superior
        parentCanvas = GetComponentInParent<Canvas>();

        // Desactiva el Raycast Target de la perilla para evitar que bloquee los eventos táctiles del fondo
        if (handleNode != null)
        {
            Image handleImage = handleNode.GetComponent<Image>();
            if (handleImage != null)
                handleImage.raycastTarget = false;
        }
    }

    // ==========================================
    // GESTIÓN DE EVENTOS TÁCTILES (EVENT SYSTEM)
    // ==========================================

    /// <summary>
    /// Se invoca al presionar inicialmente sobre el área táctil del joystick.
    /// Registra el ID del dedo que inició el toque para bloquear el control a ese puntero específico.
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointerId == -100)
        {
            activePointerId = eventData.pointerId; // Bloquea la interacción al puntero actual
            OnDrag(eventData);                     // Procesa el movimiento inicial
        }
    }

    /// <summary>
    /// Se invoca en cada cuadro mientras el dedo se desplaza por la pantalla.
    /// Calcula la posición relativa respecto al centro del fondo y restringe el movimiento dentro del radio.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        // Filtra eventos pertenecientes a otros dedos en toques multitouch
        if (eventData.pointerId != activePointerId) return;

        // Determina la cámara de renderizado del Canvas si no es ScreenSpaceOverlay
        Camera cam = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = parentCanvas.worldCamera;
        }

        // Convierte las coordenadas de pantalla a coordenadas locales del RectTransform del fondo
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            bgNode,
            eventData.position,
            cam,
            out Vector2 localPoint))
        {
            // Calcula el punto central del fondo considerando su pivote (Pivot)
            Vector2 localCenter = new Vector2(
                (0.5f - bgNode.pivot.x) * bgNode.rect.width,
                (0.5f - bgNode.pivot.y) * bgNode.rect.height
            );

            // Vector de distancia desde el centro hacia la posición actual del toque
            Vector2 positionFromCenter = localPoint - localCenter;
            float radius = bgNode.rect.width / 2f;

            // Normaliza el vector dividiendo por el radio máximo
            inputVector = positionFromCenter / radius;

            // Restringe la magnitud del vector a un máximo de 1.0 (círculo límite)
            if (inputVector.magnitude > 1.0f)
            {
                inputVector = inputVector.normalized;
            }

            // Desplaza visualmente la perilla del joystick hasta la posición calculada
            if (handleNode != null)
            {
                handleNode.anchoredPosition = inputVector * radius;
            }
        }
    }

    /// <summary>
    /// Se invoca al levantar el dedo de la pantalla.
    /// Dispara el evento OnJoystickReleased si se superó el umbral de arrastre y resetea la palanca.
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId)
        {
            // Solo dispara el evento de liberación si la palanca fue desplazada más del 20% (umbral de intención)
            if (inputVector.magnitude > 0.2f)
            {
                OnJoystickReleased?.Invoke();
            }

            ResetJoystick();
        }
    }

    // ==========================================
    // RESTABLECIMIENTO Y LIMPIEZA DE ESTADO
    // ==========================================

    /// <summary>
    /// Restablece la palanca a su posición central de reposo (0,0) y libera el puntero activo.
    /// </summary>
    private void ResetJoystick()
    {
        activePointerId = -100;
        inputVector = Vector2.zero;
        if (handleNode != null)
            handleNode.anchoredPosition = Vector2.zero;
    }

    void OnDisable()
    {
        // Fuerza el reseteo del joystick si el elemento UI es desactivado repentinamente
        ResetJoystick();
    }
}