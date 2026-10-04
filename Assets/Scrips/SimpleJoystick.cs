using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Componentes del Joystick")]
    public RectTransform bgNode;
    public RectTransform handleNode;

    private Vector2 inputVector = Vector2.zero;

    // Propiedades públicas para leer el movimiento desde TankController
    public float Horizontal => inputVector.x;
    public float Vertical => inputVector.y;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            bgNode,
            eventData.position,
            eventData.pressEventCamera,
            out position))
        {
            position.x = (position.x / bgNode.sizeDelta.x);
            position.y = (position.y / bgNode.sizeDelta.y);

            inputVector = new Vector2(position.x * 2, position.y * 2);
            inputVector = (inputVector.magnitude > 1.0f) ? inputVector.normalized : inputVector;

            // Desplazar la perilla visual
            if (handleNode != null)
            {
                handleNode.anchoredPosition = new Vector2(
                    inputVector.x * (bgNode.sizeDelta.x / 3f),
                    inputVector.y * (bgNode.sizeDelta.y / 3f)
                );
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        if (handleNode != null)
        {
            handleNode.anchoredPosition = Vector2.zero;
        }
    }
}