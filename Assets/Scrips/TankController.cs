using UnityEngine;

public class TankController : MonoBehaviour
{
    [Header("Referencias de Joysticks")]
    public SimpleJoystick leftJoystick;
    public SimpleJoystick rightJoystick;

    [Header("Referencias del Tanque")]
    public Transform torretaPivot;

    [Header("Parámetros de Movimiento")]
    public float moveSpeed = 2f;
    public float turnSpeed = 80f;
    public float turretRotateSpeed = 110f;

    void Start()
    {
        AutoConnectJoysticks();
    }

    void Update()
    {
        if (leftJoystick == null || rightJoystick == null)
        {
            AutoConnectJoysticks();
        }

        MoveChassis();
        RotateTurret();
    }

    void AutoConnectJoysticks()
    {
        if (leftJoystick == null)
        {
            GameObject leftObj = GameObject.Find("LeftJoystick");
            if (leftObj != null) leftJoystick = leftObj.GetComponent<SimpleJoystick>();
        }

        if (rightJoystick == null)
        {
            GameObject rightObj = GameObject.Find("RightJoystick");
            if (rightObj != null) rightJoystick = rightObj.GetComponent<SimpleJoystick>();
        }
    }

    void MoveChassis()
    {
        if (leftJoystick == null) return;

        float moveInput = leftJoystick.Vertical;    // Arriba/Abajo = Avanzar/Retroceder
        float turnInput = leftJoystick.Horizontal;  // Izquierda/Derecha = Rotar chasis

        // Avanzar/Retroceder hacia donde apunta el FRENTE del tanque
        Vector3 moveDirection = transform.forward * moveInput * moveSpeed * Time.deltaTime;
        transform.position += moveDirection;

        // Girar el chasis sobre su eje Y
        float turnAmount = turnInput * turnSpeed * Time.deltaTime;
        transform.Rotate(0f, turnAmount, 0f);
    }

    void RotateTurret()
    {
        if (rightJoystick == null || torretaPivot == null) return;

        float turretTurnInput = rightJoystick.Horizontal;
        if (Mathf.Abs(turretTurnInput) > 0.05f)
        {
            torretaPivot.Rotate(0f, turretTurnInput * turretRotateSpeed * Time.deltaTime, 0f);
        }
    }
}