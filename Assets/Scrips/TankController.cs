using UnityEngine;
using UnityEngine.UI; // Requerido para la imagen de recarga en el Canvas

public class TankController : MonoBehaviour
{
    [Header("Referencias de Joysticks")]
    public SimpleJoystick leftJoystick;
    public SimpleJoystick rightJoystick;

    [Header("Referencias del Tanque")]
    public Transform torretaPivot;

    [Header("Mecánica de Disparo")]
    public GameObject bulletPrefab;
    public Transform cannonMuzzle;
    public float bulletForce = 25f;
    public float reloadTime = 6.0f;       // Tiempo de recarga ajustado a 6 segundos
    private float nextFireTime = 0f;     // Control interno del tiempo para el próximo disparo
    private float lastFireTime = -999f;  // Registro del momento del último disparo

    [Header("UI de Recarga")]
    public Image reloadOverlayImage;      // Referencia a la imagen radial del joystick

    [Header("Efectos de Disparo (VFX / SFX)")]
    public MuzzleFlash3D muzzleFlash3D;   // Referencia al fogonazo 3D
    public ParticleSystem muzzleFlashVFX; // Partículas adicionales (opcional)
    public AudioSource audioSource;
    public AudioClip shootSFX;

    [Header("Guía de Trayectoria")]
    public LineRenderer trajectoryLine;
    public int trajectoryPoints = 30;
    public float timeBetweenPoints = 0.05f;

    [Header("Parámetros de Movimiento")]
    public float moveSpeed = 0.4f;
    public float turnSpeed = 2f;
    public float turretRotateSpeed = 40f;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        AutoConnectJoysticks();

        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (reloadOverlayImage != null)
        {
            reloadOverlayImage.fillAmount = 0f;
            reloadOverlayImage.enabled = false;
        }
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
        if (leftJoystick == null || rightJoystick == null || reloadOverlayImage == null || muzzleFlash3D == null)
        {
            AutoConnectJoysticks();
        }

        MoveChassis();
        RotateTurret();
        UpdateTrajectory();
        UpdateReloadUI(); // Actualiza la animación de recarga en el joystick
    }

    void AutoConnectJoysticks()
    {
        // Conectar Joystick Izquierdo
        if (leftJoystick == null)
        {
            GameObject leftObj = GameObject.Find("LeftJoystick");
            if (leftObj != null) leftJoystick = leftObj.GetComponent<SimpleJoystick>();
        }

        // Conectar Joystick Derecho
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

        // Conectar la imagen de recarga automáticamente
        if (reloadOverlayImage == null)
        {
            GameObject overlayObj = GameObject.Find("ReloadOverlay");
            if (overlayObj != null)
            {
                reloadOverlayImage = overlayObj.GetComponent<Image>();
            }
        }

        // Conectar el script del fogonazo 3D automáticamente
        if (muzzleFlash3D == null && cannonMuzzle != null)
        {
            muzzleFlash3D = cannonMuzzle.GetComponentInChildren<MuzzleFlash3D>(true);
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

    void UpdateTrajectory()
    {
        if (trajectoryLine == null || cannonMuzzle == null || rightJoystick == null) return;

        float turretX = rightJoystick.Horizontal;
        float turretZ = rightJoystick.Vertical;
        Vector3 inputDir = new Vector3(turretX, 0f, turretZ);

        // Mostrar la guía solo si se mantiene apuntando Y el cañón ya terminó de recargar
        if (inputDir.magnitude > 0.1f && Time.time >= nextFireTime)
        {
            trajectoryLine.enabled = true;
            DrawTrajectory();
        }
        else
        {
            trajectoryLine.enabled = false;
        }
    }

    void DrawTrajectory()
    {
        Vector3 startPosition = cannonMuzzle.position;

        float bulletMass = 1f;
        if (bulletPrefab != null && bulletPrefab.GetComponent<Rigidbody>() != null)
        {
            bulletMass = bulletPrefab.GetComponent<Rigidbody>().mass;
        }

        Vector3 startVelocity = cannonMuzzle.forward * (bulletForce / bulletMass);

        trajectoryLine.positionCount = trajectoryPoints;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            float time = i * timeBetweenPoints;
            Vector3 point = startPosition + startVelocity * time + 0.5f * Physics.gravity * (time * time);

            if (i > 0)
            {
                Vector3 previousPoint = trajectoryLine.GetPosition(i - 1);
                Vector3 direction = point - previousPoint;
                float distance = direction.magnitude;

                if (Physics.Raycast(previousPoint, direction.normalized, out RaycastHit hit, distance))
                {
                    trajectoryLine.SetPosition(i, hit.point);
                    trajectoryLine.positionCount = i + 1;
                    break;
                }
            }

            trajectoryLine.SetPosition(i, point);
        }
    }

    void UpdateReloadUI()
    {
        if (reloadOverlayImage == null) return;

        if (Time.time < nextFireTime)
        {
            reloadOverlayImage.enabled = true;
            float timePassed = Time.time - lastFireTime;

            // La sombra empieza totalmente cubierta (1.0) y se vacía en sentido horario hacia 0.0
            float progress = 1f - (timePassed / reloadTime);
            reloadOverlayImage.fillAmount = Mathf.Clamp01(progress);
        }
        else
        {
            reloadOverlayImage.fillAmount = 0f;
            reloadOverlayImage.enabled = false;
        }
    }

    void Shoot()
    {
        // 1. Verificar si el cañón está en recarga
        if (Time.time < nextFireTime)
        {
            Debug.Log($"Cañón recargando... Tiempo restante: {(nextFireTime - Time.time):F1}s");
            return;
        }

        if (bulletPrefab == null || cannonMuzzle == null)
        {
            Debug.LogWarning("TankController: Falta asignar bulletPrefab o cannonMuzzle en el Inspector.");
            return;
        }

        // 2. Registrar el momento exacto del disparo y la hora de la próxima recarga
        lastFireTime = Time.time;
        nextFireTime = Time.time + reloadTime;

        if (trajectoryLine != null)
            trajectoryLine.enabled = false;

        // 3. Instanciar y lanzar proyectil
        GameObject bullet = Instantiate(bulletPrefab, cannonMuzzle.position, cannonMuzzle.rotation);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(cannonMuzzle.forward * bulletForce, ForceMode.Impulse);
        }

        // 4. Activar el fogonazo 3D
        if (muzzleFlash3D != null)
        {
            muzzleFlash3D.TriggerFlash();
        }

        // 5. Partículas opcionales de destello en la boca del cañón
        if (muzzleFlashVFX != null)
        {
            muzzleFlashVFX.Play();
        }

        // 6. Sonido de disparo
        if (audioSource != null && shootSFX != null)
        {
            audioSource.PlayOneShot(shootSFX);
        }
    }
}