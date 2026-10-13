using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SimpleJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Componentes del Joystick")]
    public RectTransform bgNode;
    public RectTransform handleNode;

    private Vector2 inputVector = Vector2.zero;
    private Canvas parentCanvas;
    private int activePointerId = -100;

    // Evento que se dispara al soltar el joystick tras apuntar
    public event System.Action OnJoystickReleased;

    public float Horizontal => inputVector.x;
    public float Vertical => inputVector.y;

    void Awake()
    {
        Input.multiTouchEnabled = true;

        if (bgNode == null)
            bgNode = GetComponent<RectTransform>();

        parentCanvas = GetComponentInParent<Canvas>();

        if (handleNode != null)
        {
            Image handleImage = handleNode.GetComponent<Image>();
            if (handleImage != null)
                handleImage.raycastTarget = false;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointerId == -100)
        {
            activePointerId = eventData.pointerId;
            OnDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;

        Camera cam = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = parentCanvas.worldCamera;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            bgNode,
            eventData.position,
            cam,
            out Vector2 localPoint))
        {
            Vector2 localCenter = new Vector2(
                (0.5f - bgNode.pivot.x) * bgNode.rect.width,
                (0.5f - bgNode.pivot.y) * bgNode.rect.height
            );

            Vector2 positionFromCenter = localPoint - localCenter;
            float radius = bgNode.rect.width / 2f;

            inputVector = positionFromCenter / radius;

            if (inputVector.magnitude > 1.0f)
            {
                inputVector = inputVector.normalized;
            }

            if (handleNode != null)
            {
                handleNode.anchoredPosition = inputVector * radius;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId)
        {
            // Solo disparar si el usuario estaba tirando de la palanca (umbral mínimo)
            if (inputVector.magnitude > 0.2f)
            {
                OnJoystickReleased?.Invoke();
            }

            ResetJoystick();
        }
    }

    private void ResetJoystick()
    {
        activePointerId = -100;
        inputVector = Vector2.zero;
        if (handleNode != null)
            handleNode.anchoredPosition = Vector2.zero;
    }

    void OnDisable()
    {
        ResetJoystick();
    }
}