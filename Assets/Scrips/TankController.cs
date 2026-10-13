using UnityEngine;

public class TankController : MonoBehaviour
{
    [Header("Referencias de Joysticks")]
    public SimpleJoystick leftJoystick;
    public SimpleJoystick rightJoystick;

    [Header("Referencias del Tanque")]
    public Transform torretaPivot;

    [Header("Mecánica de Disparo")]
    public GameObject bulletPrefab;      // Prefab del proyectil/bala
    public GameObject cannonMuzzle;     // Cambiado a GameObject para evitar 'Type mismatch'
    public float bulletForce = 25f;      // Fuerza del disparo

    [Header("Parámetros de Movimiento")]
    public float moveSpeed = 0.4f;
    public float turnSpeed = 2f;
    public float turretRotateSpeed = 40f;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        AutoConnectJoysticks();
    }

    void OnDisable()
    {
        if (rightJoystick != null)
        {
            rightJoystick.OnJoystickReleased -= Shoot;
        }
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
            if (rightObj != null)
            {
                if (rightJoystick != null) rightJoystick.OnJoystickReleased -= Shoot;

                rightJoystick = rightObj.GetComponent<SimpleJoystick>();

                if (rightJoystick != null)
                {
                    rightJoystick.OnJoystickReleased += Shoot;
                }
            }
        }
    }

    void MoveChassis()
    {
        if (leftJoystick == null) return;

        float moveX = leftJoystick.Horizontal;
        float moveZ = leftJoystick.Vertical;
        Vector3 inputDir = new Vector3(moveX, 0f, moveZ);

        if (inputDir.magnitude > 0.05f)
        {
            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDirection = camForward * moveZ + camRight * moveX;
            transform.position += moveDirection * moveSpeed * Time.deltaTime;

            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }
    }

    void RotateTurret()
    {
        if (rightJoystick == null || torretaPivot == null) return;

        float turretX = rightJoystick.Horizontal;
        float turretZ = rightJoystick.Vertical;
        Vector3 inputDir = new Vector3(turretX, 0f, turretZ);

        if (inputDir.magnitude > 0.05f)
        {
            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 targetTurretDirection = camForward * turretZ + camRight * turretX;

            if (targetTurretDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetTurretDirection);
                torretaPivot.rotation = Quaternion.RotateTowards(
                    torretaPivot.rotation,
                    targetRotation,
                    turretRotateSpeed * Time.deltaTime
                );
            }
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null || cannonMuzzle == null)
        {
            Debug.LogWarning("TankController: Falta asignar bulletPrefab o cannonMuzzle en el Inspector.");
            return;
        }

        // Usamos .transform para obtener la posición y rotación del GameObject
        GameObject bullet = Instantiate(bulletPrefab, cannonMuzzle.transform.position, cannonMuzzle.transform.rotation);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(cannonMuzzle.transform.forward * bulletForce, ForceMode.Impulse);
        }

        Destroy(bullet, 4f);
    }
}