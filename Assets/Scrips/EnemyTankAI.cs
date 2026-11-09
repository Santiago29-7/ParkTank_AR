using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona la Inteligencia Artificial del tanque enemigo en Realidad Aumentada.
/// Controla la rotación independiente de la torreta, el movimiento táctico mediante máquinas de estado,
/// la evasión proactiva de obstáculos y el sistema de disparo desincronizado.
/// </summary>
public class EnemyTankAI : MonoBehaviour
{
    // ==========================================
    // REFERENCIAS DE TORRETA Y DISPARO
    // ==========================================

    [Header("Referencias de Torreta y Disparo")]
    [Tooltip("Pivote de la torreta del tanque que girará hacia el jugador")]
    public Transform turretTransform;

    [Tooltip("Punto exacto en la punta del cañón donde se instancian las balas")]
    public Transform muzzlePoint;

    [Tooltip("Prefab del proyectil que disparará este tanque enemigo")]
    public GameObject bulletPrefab;

    [Tooltip("Efecto visual (VFX) de chispazo/fogonazo al disparar")]
    public GameObject muzzleFlashPrefab;

    // ==========================================
    // PARÁMETROS DE ATAQUE Y DAÑO
    // ==========================================

    [Header("Configuración de Ataque")]
    [Tooltip("Distancia máxima a la que el enemigo detecta y empieza a atacar al jugador")]
    public float detectionRange = 4.0f;

    [Tooltip("Velocidad de suavizado con la que la torreta apunta al objetivo")]
    public float turretRotationSpeed = 8f;

    [Tooltip("Cadencia de disparo base en segundos")]
    public float fireRate = 2.5f;

    [Tooltip("Velocidad de impulso del proyectil enemigo")]
    public float bulletSpeed = 4f;

    [Tooltip("Daño infligido por cada impacto de bala")]
    public float bulletDamage = 15f;

    // ==========================================
    // PARÁMETROS DE MOVIMIENTO TÁCTICO
    // ==========================================

    [Header("Movimiento Táctico Jugador")]
    [Tooltip("Velocidad de desplazamiento del tanque por el terreno")]
    public float moveSpeed = 0.85f;

    [Tooltip("Velocidad con la que el chasis gira hacia la dirección de movimiento")]
    public float bodyRotationSpeed = 8f;

    [Tooltip("Distancia mínima al jugador antes de obligar al enemigo a retroceder")]
    public float minCombatDistance = 0.70f;

    [Tooltip("Distancia máxima a la que el enemigo considerará acercarse al jugador")]
    public float maxCombatDistance = 1.50f;

    // ==========================================
    // DETECCIÓN DE OBSTÁCULOS
    // ==========================================

    [Header("Detección de Choques / Obstáculos")]
    [Tooltip("Distancia del SphereCast para detectar choques frontales con coberturas u otros tanques")]
    public float obstacleCheckDistance = 0.18f;

    // ==========================================
    // MÁQUINA DE ESTADOS Y VARIABLES INTERNAS
    // ==========================================

    /// <summary>
    /// Estados tácticos de movimiento de la IA:
    /// Approaching: Se acerca en zig-zag.
    /// Retreating: Se aleja del jugador.
    /// Orbiting: Rodea al jugador lateralmente.
    /// Wandering: Movimiento semi-aleatorio de patrulla.
    /// </summary>
    private enum MovementState { Approaching, Retreating, Orbiting, Wandering }
    private MovementState currentState;

    private float nextStateChangeTime; // Marca de tiempo en segundos para evaluar un nuevo estado de movimiento
    private Transform playerTarget;    // Referencia directa al Transform del tanque del jugador
    private float nextFireTime;        // Marca de tiempo para el siguiente disparo con variación (jitter)
    private int strafeDirection = 1;  // Dirección lateral de la ráfaga (1 = Derecha, -1 = Izquierda)
    private Rigidbody rb;              // Referencia al componente de física para mover el chasis sin atravesar muros

    // ==========================================
    // INICIALIZACIÓN
    // ==========================================

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Asigna un estado de movimiento inicial por un período de 2 segundos
        ChooseNewMovementState(2.0f);

        // DESFASE DE DISPARO INICIAL:
        // Evita que múltiples enemigos instanciados al mismo tiempo disparen sincronizadamente
        nextFireTime = Time.time + Random.Range(0.5f, fireRate);
    }

    // ==========================================
    // BUCLE PRINCIPAL DE ACTUALIZACIÓN
    // ==========================================

    void Update()
    {
        // Si aún no ha encontrado al jugador en la escena, ejecuta la búsqueda
        if (playerTarget == null)
        {
            FindPlayer();
            return;
        }

        // Calcula la distancia actual entre este enemigo y el jugador
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // Si el jugador entra dentro del rango de detección activa la IA
        if (distanceToPlayer <= detectionRange)
        {
            // 1. Apunta la torreta de manera independiente hacia la posición del jugador
            AimTurretAtPlayer();

            // 2. Ejecuta el sistema de desplazamiento dinámico según el estado actual
            HandleDynamicMovement(distanceToPlayer);

            // 3. Ejecuta la lógica de disparo desincronizado
            if (Time.time >= nextFireTime)
            {
                Shoot();

                // JITTER/VARIACIÓN EN EL TIEMPO DE RECARGA:
                // Agrega una pequeña variación aleatoria de +/- 0.4s para desincronizar los ráfagos de disparos
                float randomJitter = Random.Range(-0.4f, 0.4f);
                nextFireTime = Time.time + Mathf.Max(1.0f, fireRate + randomJitter);
            }
        }
    }

    // ==========================================
    // BÚSQUEDA Y SEGUIMIENTO DEL JUGADOR
    // ==========================================

    /// <summary>
    /// Localiza el objeto del jugador en la escena activa mediante la referencia de TankController.
    /// </summary>
    void FindPlayer()
    {
        TankController controller = FindFirstObjectByType<TankController>();
        if (controller != null)
        {
            playerTarget = controller.transform;
        }
    }

    // ==========================================
    // SISTEMA DE NAVEGACIÓN Y MOVIMIENTO TÁCTICO
    // ==========================================

    /// <summary>
    /// Procesa el vector de movimiento del chasis en función de la máquina de estados y las colisiones.
    /// </summary>
    /// <param name="distanceToPlayer">Distancia escalar actual hasta el jugador.</param>
    void HandleDynamicMovement(float distanceToPlayer)
    {
        // Cambia de táctica si expiró el temporizador o si detecta un obstáculo directo al frente
        if (Time.time >= nextStateChangeTime || IsObstacleAhead())
        {
            ChooseNewMovementState(distanceToPlayer);
        }

        // Calcula el vector de dirección horizontal hacia el jugador descartando diferencias de altura en Y
        Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
        directionToPlayer.y = 0;

        // Vector perpendicular para el desplazamiento lateral (strafing)
        Vector3 strafeVector = Vector3.Cross(directionToPlayer, Vector3.up) * strafeDirection;
        Vector3 moveDirection = Vector3.zero;

        // Comportamiento del vector según el estado actual de la IA
        switch (currentState)
        {
            case MovementState.Approaching:
                // Avance frontal combinado con desplazamiento diagonal
                moveDirection = directionToPlayer + (strafeVector * 0.4f);

                // Si sobrepasa la distancia mínima, fuerza un cambio de estado para retroceder
                if (distanceToPlayer < minCombatDistance) ChooseNewMovementState(distanceToPlayer);
                break;

            case MovementState.Retreating:
                // Retroceso alejándose del jugador con ligero desplazamiento lateral
                moveDirection = -directionToPlayer + (strafeVector * 0.5f);
                break;

            case MovementState.Orbiting:
                // Circunda al jugador manteniendo la distancia focal
                moveDirection = strafeVector;
                break;

            case MovementState.Wandering:
                // Movimiento semi-aleatorio mezclando avance/retroceso con ráfagas laterales
                float randomForward = Random.value > 0.5f ? 0.5f : -0.5f;
                moveDirection = strafeVector + (directionToPlayer * randomForward);
                break;
        }

        moveDirection.Normalize();

        // Si el vector de movimiento tiene magnitud suficiente, rota el chasis y traslada la posición física
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            // Rotación suave del chasis hacia la dirección de marcha
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, bodyRotationSpeed * Time.deltaTime);

            Vector3 targetPos = transform.position + moveDirection * moveSpeed * Time.deltaTime;

            // Traslación por físicas mediante Rigidbody para evitar atravesar paredes o coberturas
            if (rb != null && !rb.isKinematic)
            {
                rb.MovePosition(targetPos);
            }
            else
            {
                transform.position = targetPos;
            }
        }
    }

    /// <summary>
    /// Selecciona de manera probabilística un nuevo estado de movimiento basándose en la distancia actual al jugador.
    /// </summary>
    /// <param name="distanceToPlayer">Distancia medida hacia el objetivo.</param>
    void ChooseNewMovementState(float distanceToPlayer)
    {
        strafeDirection = Random.value > 0.5f ? 1 : -1; // Elige aleatoriamente si rodear por la izquierda o derecha
        nextStateChangeTime = Time.time + Random.Range(2.0f, 4.0f); // Mantiene el estado seleccionado entre 2 y 4 segundos

        if (distanceToPlayer < minCombatDistance)
        {
            // Prioridad absoluta: Si está excesivamente cerca, fuerza el retroceso
            currentState = MovementState.Retreating;
        }
        else if (distanceToPlayer > maxCombatDistance)
        {
            // Si está lejos, decide si aproximarse o vagar por la zona
            currentState = Random.value > 0.6f ? MovementState.Approaching : MovementState.Wandering;
        }
        else
        {
            // Zona media de combate: Alterna entre orbitar, patrullar o retroceder para ser impredecible
            float rand = Random.value;
            if (rand < 0.33f) currentState = MovementState.Orbiting;
            else if (rand < 0.66f) currentState = MovementState.Wandering;
            else currentState = MovementState.Retreating;
        }
    }

    // ==========================================
    // SENSORIA Y DETECCIÓN DE OBSTÁCULOS
    // ==========================================

    /// <summary>
    /// Realiza un barrido esférico (SphereCast) al frente del chasis para detectar coberturas u otros tanques en la trayectoria.
    /// </summary>
    /// <returns>True si detecta un obstáculo directo en su camino.</returns>
    bool IsObstacleAhead()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.05f; // Eleva ligeramente el origen del rayo sobre el suelo

        if (Physics.SphereCast(origin, 0.06f, transform.forward, out hit, obstacleCheckDistance))
        {
            // Filtra que el choque no sea contra sí mismo y que corresponda a otro enemigo, jugador o estructura con salud
            if (hit.collider.gameObject != gameObject &&
               (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<TankHealth>() != null))
            {
                return true;
            }
        }
        return false;
    }

    // ==========================================
    // APUNTADO DE TORRETA
    // ==========================================

    /// <summary>
    /// Rota la torreta de forma independiente al chasis para mantener fija la mira sobre el jugador.
    /// </summary>
    void AimTurretAtPlayer()
    {
        if (turretTransform == null || playerTarget == null) return;

        Vector3 targetDirection = playerTarget.position - turretTransform.position;
        targetDirection.y = 0; // Garantiza rotación pura en el eje horizontal Y

        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            turretTransform.rotation = Quaternion.Slerp(turretTransform.rotation, targetRotation, turretRotationSpeed * Time.deltaTime);
        }
    }

    // ==========================================
    // EJECUCIÓN DE DISPARO Y EFECTOS
    // ==========================================

    /// <summary>
    /// Instancia el proyectil enemigo, ignora colisiones propias, aplica velocidad física y reproduce el fogonazo de disparo.
    /// </summary>
    void Shoot()
    {
        if (bulletPrefab == null || muzzlePoint == null) return;

        // Calcula la posición de spawn proyectada al frente del cañón
        Vector3 spawnPosition = muzzlePoint.position + (muzzlePoint.forward * 0.15f);
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, muzzlePoint.rotation);

        // Omite la colisión entre el proyectil instanciado y los colliders del propio tanque disparador
        Collider bulletCollider = bullet.GetComponent<Collider>();
        Collider[] tankColliders = GetComponentsInChildren<Collider>();
        if (bulletCollider != null)
        {
            foreach (Collider col in tankColliders)
            {
                Physics.IgnoreCollision(bulletCollider, col);
            }
        }

        // Configura el script interno de la bala (daño, origen y altura de referencia)
        TankBullet bulletScript = bullet.GetComponent<TankBullet>();
        if (bulletScript != null)
        {
            bulletScript.damage = bulletDamage;
            bulletScript.SetShooter(gameObject);
            bulletScript.SetGroundYLevel(transform.position.y);
        }

        // Escala el tamaño del proyectil a la proporción adecuada para Realidad Aumentada
        bullet.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);

        // Asigna la velocidad constante al Rigidbody de la bala
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            bulletRb.isKinematic = false;
            bulletRb.useGravity = false;
            bulletRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            bulletRb.linearVelocity = muzzlePoint.forward * bulletSpeed;
        }

        // Destruye automáticamente el proyectil a los 4 segundos si no impactó contra nada
        Destroy(bullet, 4f);

        // Instancia y escala el efecto visual del fogonazo del cañón
        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
            flash.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            Destroy(flash, 0.3f);
        }
    }
}