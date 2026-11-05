using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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
    public float bulletForce = 6f;
    public float reloadTime = 6.0f;
    private float nextFireTime = 0f;
    private float lastFireTime = -999f;

    [Header("Mecánica Caja Amarilla (Doble Daño)")]
    private int doubleDamageShotsLeft = 0; // Contador de disparos potenciados

    [Header("UI de Recarga")]
    public Image reloadOverlayImage;

    [Header("Efectos y Sonidos (VFX / SFX)")]
    public MuzzleFlash3D muzzleFlash3D;
    public ParticleSystem muzzleFlashVFX;
    public AudioSource audioSource;
    public AudioClip shootSFX;

    [Header("Sonidos de Motor")]
    public AudioClip engineStartSFX;
    public AudioClip engineIdleSFX;
    public AudioClip engineDrivingSFX;

    [Header("Guía de Trayectoria")]
    public LineRenderer trajectoryLine;
    public int trajectoryPoints = 30;
    public float timeBetweenPoints = 0.05f;

    [Header("Parámetros de Movimiento")]
    public float moveSpeed = 0.4f;
    public float turnSpeed = 2f;
    public float turretRotateSpeed = 40f;

    private bool isEngineReady = false;
    private bool isMoving = false;
    private bool canMove = true; // Control de inmovilización (por reparación)
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        // Auto-conectar joysticks e interfaz al instanciar
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

        StartCoroutine(StartEngineSequence());
    }

    // --- MÉTODO PARA BLOQUEAR / DESBLOQUEAR EL MOVIMIENTO (USADO POR REPARACIÓN) ---
    public void SetCanMove(bool state)
    {
        canMove = state;
        if (!canMove)
        {
            isMoving = false;
            if (trajectoryLine != null) trajectoryLine.enabled = false;
        }
    }

    public bool CanMove => canMove;

    IEnumerator StartEngineSequence()
    {
        isEngineReady = false;

        if (engineStartSFX != null && audioSource != null)
        {
            audioSource.clip = engineStartSFX;
            audioSource.loop = false;
            audioSource.Play();
            yield return new WaitForSeconds(engineStartSFX.length);
        }

        isEngineReady = true;

        if (engineIdleSFX != null && audioSource != null)
        {
            audioSource.clip = engineIdleSFX;
            audioSource.loop = true;
            audioSource.Play();
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

        if (!isEngineReady)
        {
            if (trajectoryLine != null) trajectoryLine.enabled = false;
            return;
        }

        MoveChassis();
        RotateTurret();
        UpdateTrajectory();
        UpdateReloadUI();
        HandleEngineAudio();
    }

    void HandleEngineAudio()
    {
        if (audioSource == null) return;

        if (isMoving)
        {
            if (engineDrivingSFX != null && audioSource.clip != engineDrivingSFX)
            {
                audioSource.clip = engineDrivingSFX;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
        else
        {
            if (engineIdleSFX != null && audioSource.clip != engineIdleSFX)
            {
                audioSource.clip = engineIdleSFX;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
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

        if (reloadOverlayImage == null)
        {
            GameObject overlayObj = GameObject.Find("ReloadOverlay");
            if (overlayObj != null)
            {
                reloadOverlayImage = overlayObj.GetComponent<Image>();
            }
        }

        if (muzzleFlash3D == null && cannonMuzzle != null)
        {
            muzzleFlash3D = cannonMuzzle.GetComponentInChildren<MuzzleFlash3D>(true);
        }
    }

    void MoveChassis()
    {
        if (!canMove || leftJoystick == null)
        {
            isMoving = false;
            return;
        }

        float moveX = leftJoystick.Horizontal;
        float moveZ = leftJoystick.Vertical;
        Vector3 inputDir = new Vector3(moveX, 0f, moveZ);

        isMoving = inputDir.magnitude > 0.05f;

        if (isMoving)
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
        if (!canMove || rightJoystick == null || torretaPivot == null) return;

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
        if (!canMove || trajectoryLine == null || cannonMuzzle == null || rightJoystick == null)
        {
            if (trajectoryLine != null) trajectoryLine.enabled = false;
            return;
        }

        float turretX = rightJoystick.Horizontal;
        float turretZ = rightJoystick.Vertical;
        Vector3 inputDir = new Vector3(turretX, 0f, turretZ);

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

            float progress = 1f - (timePassed / reloadTime);
            reloadOverlayImage.fillAmount = Mathf.Clamp01(progress);
        }
        else
        {
            reloadOverlayImage.fillAmount = 0f;
            reloadOverlayImage.enabled = false;
        }
    }

    public void ActivateNukeMode()
    {
        doubleDamageShotsLeft = 2;
    }

    void Shoot()
    {
        if (!canMove) return;
        if (!isEngineReady) return;
        if (Time.time < nextFireTime) return;
        if (cannonMuzzle == null || bulletPrefab == null) return;

        lastFireTime = Time.time;
        nextFireTime = Time.time + reloadTime;

        if (trajectoryLine != null)
            trajectoryLine.enabled = false;

        Vector3 spawnPosition = cannonMuzzle.position + (cannonMuzzle.forward * 0.2f);
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, cannonMuzzle.rotation);

        Collider bulletCollider = bullet.GetComponent<Collider>();
        Collider[] tankColliders = GetComponentsInChildren<Collider>();
        if (bulletCollider != null)
        {
            foreach (Collider col in tankColliders)
            {
                Physics.IgnoreCollision(bulletCollider, col);
            }
        }

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(cannonMuzzle.forward * bulletForce, ForceMode.Impulse);
        }

        bool isDoubleDamage = false;
        if (doubleDamageShotsLeft > 0)
        {
            isDoubleDamage = true;
            doubleDamageShotsLeft--;
        }

        TankBullet bulletScript = bullet.GetComponent<TankBullet>();
        if (bulletScript != null)
        {
            bulletScript.SetShooter(gameObject);
            bulletScript.SetGroundYLevel(transform.position.y - 0.03f);

            if (isDoubleDamage)
            {
                bulletScript.EnableDoubleDamage();
            }
        }

        if (muzzleFlash3D != null) muzzleFlash3D.TriggerFlash();
        if (muzzleFlashVFX != null) muzzleFlashVFX.Play();

        if (audioSource != null && shootSFX != null)
        {
            audioSource.PlayOneShot(shootSFX);
        }
    }
}