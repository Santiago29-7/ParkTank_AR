using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla la lógica principal del tanque del jugador en Realidad Aumentada.
/// Administra el movimiento del chasis guiado por cámara, la rotación independiente de la torreta,
/// el cálculo de la línea de trayectoria balística (LineRenderer), el sistema de disparo y recarga,
/// los efectos de sonido del motor (encendido, reposo, marcha) y los modificadores de daño (Power-Up Nuke).
/// </summary>
public class TankController : MonoBehaviour
{
    // ==========================================
    // CONTROLES Y JOYSTICKS TÁCTILES
    // ==========================================

    [Header("Referencias de Joysticks")]
    [Tooltip("Joystick izquierdo dedicado al desplazamiento del chasis del tanque")]
    public SimpleJoystick leftJoystick;

    [Tooltip("Joystick derecho dedicado al apuntado de la torreta y disparo")]
    public SimpleJoystick rightJoystick;

    // ==========================================
    // ESTRUCTURA Y COMPONENTES DEL TANQUE
    // ==========================================

    [Header("Referencias del Tanque")]
    [Tooltip("Transform del pivote de la torreta para rotación independiente del cuerpo")]
    public Transform torretaPivot;

    // ==========================================
    // BALÍSTICA Y MECÁNICA DE DISPARO
    // ==========================================

    [Header("Mecánica de Disparo")]
    [Tooltip("Prefab del proyectil que será instanciado al disparar (TankBullet)")]
    public GameObject bulletPrefab;

    [Tooltip("Punto de salida (boca del cañón) desde donde nacen los proyectiles")]
    public Transform cannonMuzzle;

    [Tooltip("Fuerza de impulso aplicada al proyectil al disparar")]
    public float bulletForce = 6f;

    [Tooltip("Tiempo de recarga entre disparos en segundos")]
    public float reloadTime = 6.0f;

    private float nextFireTime = 0f;       // Marca de tiempo que autoriza el siguiente disparo
    private float lastFireTime = -999f;    // Marca de tiempo del último disparo realizado

    [Header("Mecánica Caja Amarilla (Doble Daño)")]
    private int doubleDamageShotsLeft = 0; // Contador de disparos potenciados restantes (Modo Nuke Épico)

    // ==========================================
    // INTERFAZ DE USUARIO (UI)
    // ==========================================

    [Header("UI de Recarga")]
    [Tooltip("Imagen UI tipo Filled que visualiza la cuenta regresiva de recarga en el botón")]
    public Image reloadOverlayImage;

    // ==========================================
    // EFECTOS VISUALES Y AUDITIVOS (VFX / SFX)
    // ==========================================

    [Header("Efectos y Sonidos (VFX / SFX)")]
    [Tooltip("Componente de fogonazo 3D para el cañón")]
    public MuzzleFlash3D muzzleFlash3D;

    [Tooltip("Sistema de partículas de destello (Muzzle Flash) opcional")]
    public ParticleSystem muzzleFlashVFX;

    [Tooltip("AudioSource principal del tanque para reproducir motor y disparos")]
    public AudioSource audioSource;

    [Tooltip("Efecto de sonido de disparo")]
    public AudioClip shootSFX;

    [Header("Sonidos de Motor")]
    [Tooltip("AudioClip del encendido inicial del motor")]
    public AudioClip engineStartSFX;

    [Tooltip("AudioClip del motor encendido en reposo (Idle)")]
    public AudioClip engineIdleSFX;

    [Tooltip("AudioClip del motor acelerando durante el movimiento")]
    public AudioClip engineDrivingSFX;

    // ==========================================
    // PREDICCIÓN DE TRAYECTORIA
    // ==========================================

    [Header("Guía de Trayectoria")]
    [Tooltip("LineRenderer para proyectar la parábola del disparo antes de lanzar")]
    public LineRenderer trajectoryLine;

    [Tooltip("Cantidad de puntos de muestreo para calcular la curva parabólica")]
    public int trajectoryPoints = 30;

    [Tooltip("Intervalo de tiempo entre cada punto de cálculo de la parábola")]
    public float timeBetweenPoints = 0.05f;

    // ==========================================
    // PARÁMETROS DE MOVIMIENTO Y FÍSICAS
    // ==========================================

    [Header("Parámetros de Movimiento")]
    [Tooltip("Velocidad de desplazamiento lineal del tanque en metros por segundo")]
    public float moveSpeed = 0.4f;

    [Tooltip("Velocidad de rotación del chasis hacia la dirección de movimiento")]
    public float turnSpeed = 2f;

    [Tooltip("Velocidad de giro de la torreta hacia la dirección del joystick derecho")]
    public float turretRotateSpeed = 40f;

    // ==========================================
    // ESTADO INTERNO
    // ==========================================

    private bool isEngineReady = false; // Indica si la secuencia de encendido de motor ya finalizó
    private bool isMoving = false;      // Determina si el chasis se está desplazando activamente
    private bool canMove = true;        // Control de inmovilización (usado por sistemas de reparación o stun)
    private Camera mainCamera;          // Referencia a la cámara principal de la escena AR

    // ==========================================
    // INICIALIZACIÓN
    // ==========================================

    void Start()
    {
        mainCamera = Camera.main;

        // Auto-conecta las referencias de joysticks e interfaz táctil si no están asignadas
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

        // Inicia la secuencia auditiva de arranque del motor
        StartCoroutine(StartEngineSequence());
    }

    // ==========================================
    // CONTROL DE ESTADO DE MOVIMIENTO Y MOTOR
    // ==========================================

    /// <summary>
    /// Habilita o inhabilita la capacidad de movimiento y apuntado del tanque (ej. durante reparaciones).
    /// </summary>
    public void SetCanMove(bool state)
    {
        canMove = state;
        if (!canMove)
        {
            isMoving = false;
            if (trajectoryLine != null) trajectoryLine.enabled = false;
        }
    }

    /// <summary>
    /// Propiedad pública que devuelve si el tanque tiene permitido moverse.
    /// </summary>
    public bool CanMove => canMove;

    /// <summary>
    /// Corrutina que reproduce el sonido de arranque del motor antes de pasar al loop de reposo (Idle).
    /// </summary>
    IEnumerator StartEngineSequence()
    {
        isEngineReady = false;

        // 1. Reproduce el sonido de encendido inicial (Engine Start)
        if (engineStartSFX != null && audioSource != null)
        {
            audioSource.clip = engineStartSFX;
            audioSource.loop = false;
            audioSource.Play();
            yield return new WaitForSeconds(engineStartSFX.length);
        }

        isEngineReady = true;

        // 2. Transiciona al sonido en bucle de motor encendido (Engine Idle)
        if (engineIdleSFX != null && audioSource != null)
        {
            audioSource.clip = engineIdleSFX;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void OnDisable()
    {
        // Desvincula el evento de disparo al desactivar el componente para evitar fugas de memoria
        if (rightJoystick != null)
        {
            rightJoystick.OnJoystickReleased -= Shoot;
        }
    }

    // ==========================================
    // BUCLE PRINCIPAL DE ACTUALIZACIÓN
    // ==========================================

    void Update()
    {
        // Reintenta auto-conectar componentes en caso de ser instanciado dinámicamente
        if (leftJoystick == null || rightJoystick == null || reloadOverlayImage == null || muzzleFlash3D == null)
        {
            AutoConnectJoysticks();
        }

        // Si el motor no ha terminado de encender, no permite interactuar ni dibujar trayectorias
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

    // ==========================================
    // GESTIÓN DE AUDIO DEL MOTOR
    // ==========================================

    /// <summary>
    /// Cambia dinámicamente la pista de audio del motor entre estado en reposo (Idle) y aceleración (Driving).
    /// </summary>
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

    // ==========================================
    // AUTO-CONEXIÓN DE ELEMENTOS DE INTERFAZ
    // ==========================================

    /// <summary>
    /// Busca dinámicamente los Joysticks y elementos de la UI en la jerarquía activa si no fueron asignados.
    /// </summary>
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

    // ==========================================
    // MOVIMIENTO DEL CHASIS
    // ==========================================

    /// <summary>
    /// Desplaza y rota el chasis del tanque en relación con la orientación de la cámara AR.
    /// Utiliza Rigidbody.MovePosition para garantizar que se respeten los obstáculos físicos del entorno.
    /// </summary>
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
            // Calcula los vectores al frente y a la derecha de la cámara descartando la inclinación vertical (eje Y)
            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            // Dirección final de movimiento adaptada a la vista del usuario
            Vector3 moveDirection = camForward * moveZ + camRight * moveX;

            // Desplazamiento basado en físicas
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.deltaTime);
            }
            else
            {
                transform.position += moveDirection * moveSpeed * Time.deltaTime;
            }

            // Orientación progresiva del chasis hacia la dirección recorrida
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }
    }

    // ==========================================
    // ROTACIÓN DE LA TORRETA
    // ==========================================

    /// <summary>
    /// Rota suavemente la torreta hacia la dirección indicada por el Joystick derecho en relación a la cámara.
    /// </summary>
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

    // ==========================================
    // PREDICCIÓN Y DIBUJO DE TRAYECTORIA BALÍSTICA
    // ==========================================

    /// <summary>
    /// Activa la visualización de la parábola de disparo si el joystick derecho está desplazado y la recarga está lista.
    /// </summary>
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

    /// <summary>
    /// Simula la ecuación de movimiento parabólico considerando la masa del proyectil, la fuerza y la gravedad.
    /// Detiene el trazado prematuramente si detecta una colisión física mediante Raycast.
    /// </summary>
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
            // Ecuación de cinemática: p = p0 + v0*t + 0.5*g*t^2
            Vector3 point = startPosition + startVelocity * time + 0.5f * Physics.gravity * (time * time);

            if (i > 0)
            {
                Vector3 previousPoint = trajectoryLine.GetPosition(i - 1);
                Vector3 direction = point - previousPoint;
                float distance = direction.magnitude;

                // Si un obstáculo intercepta la trayectoria proyectada, corta la línea en ese punto exacto
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

    // ==========================================
    // INTERFAZ DE TIEMPO DE RECARGA
    // ==========================================

    /// <summary>
    /// Actualiza la barra circular de enfriamiento/recarga (Reload Overlay) en la interfaz táctil.
    /// </summary>
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

    // ==========================================
    // POTENCIADORES Y DISPARO
    // ==========================================

    /// <summary>
    /// Otorga 2 disparos con daño duplicado al recolectar un Power-Up Épico / Nuke.
    /// </summary>
    public void ActivateNukeMode()
    {
        doubleDamageShotsLeft = 2;
    }

    /// <summary>
    /// Se ejecuta al soltar el joystick de apuntado. Instancia la bala, le aplica fuerza de impulso,
    /// ignora colisiones con el propio tanque, configura modismos de daño y dispara efectos de fogonazo y audio.
    /// </summary>
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

        // Instanciación del proyectil ligeramente frente a la boca del cañón
        Vector3 spawnPosition = cannonMuzzle.position + (cannonMuzzle.forward * 0.2f);
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, cannonMuzzle.rotation);

        // Garantiza que la bala no colisione físicamente con la estructura del propio tanque
        Collider bulletCollider = bullet.GetComponent<Collider>();
        Collider[] tankColliders = GetComponentsInChildren<Collider>();
        if (bulletCollider != null)
        {
            foreach (Collider col in tankColliders)
            {
                Physics.IgnoreCollision(bulletCollider, col);
            }
        }

        // Aplicación del impulso físico al proyectil
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(cannonMuzzle.forward * bulletForce, ForceMode.Impulse);
        }

        // Comprobación de disparos potenciados (Nuke)
        bool isDoubleDamage = false;
        if (doubleDamageShotsLeft > 0)
        {
            isDoubleDamage = true;
            doubleDamageShotsLeft--;
        }

        // Configuración de emisor y modo de daño en el script de la bala
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

        // Activación de efectos visuales y audios de disparo
        if (muzzleFlash3D != null) muzzleFlash3D.TriggerFlash();
        if (muzzleFlashVFX != null) muzzleFlashVFX.Play();

        if (audioSource != null && shootSFX != null)
        {
            audioSource.PlayOneShot(shootSFX);
        }
    }
}