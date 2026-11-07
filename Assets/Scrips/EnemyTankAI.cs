using UnityEngine;

public class EnemyTankAI : MonoBehaviour
{
    [Header("Referencias de Torreta y Disparo")]
    public Transform turretTransform;
    public Transform muzzlePoint;
    public GameObject bulletPrefab;
    public GameObject muzzleFlashPrefab;

    [Header("Configuración de Ataque")]
    public float detectionRange = 4.0f;
    public float turretRotationSpeed = 8f;
    public float fireRate = 2.5f;
    public float bulletSpeed = 4f;
    public float bulletDamage = 15f;

    [Header("Movimiento Táctico Jugador")]
    public float moveSpeed = 0.85f;
    public float bodyRotationSpeed = 8f;

    // Distancias para saber qué tan cerca es "demasiado cerca" o "demasiado lejos"
    public float minCombatDistance = 0.70f;
    public float maxCombatDistance = 1.50f;

    [Header("Detección de Choques / Obstáculos")]
    public float obstacleCheckDistance = 0.18f;

    // --- SISTEMA DE ESTADOS ---
    private enum MovementState { Approaching, Retreating, Orbiting, Wandering }
    private MovementState currentState;
    private float nextStateChangeTime;

    private Transform playerTarget;
    private float nextFireTime;
    private int strafeDirection = 1; // 1 = Derecha, -1 = Izquierda
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        ChooseNewMovementState(2.0f); // Estado inicial

        // 1. DESFASE INICIAL: Cada tanque empieza con un tiempo de espera aleatorio para su primer disparo
        nextFireTime = Time.time + Random.Range(0.5f, fireRate);
    }

    void Update()
    {
        if (playerTarget == null)
        {
            FindPlayer();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (distanceToPlayer <= detectionRange)
        {
            // 1. La torreta SIEMPRE mira al jugador, sin importar a dónde vaya el tanque
            AimTurretAtPlayer();

            // 2. Movimiento inteligente (Vagar, acercarse, alejarse)
            HandleDynamicMovement(distanceToPlayer);

            // 3. Disparar desincronizado
            if (Time.time >= nextFireTime)
            {
                Shoot();

                // 2. VARIACIÓN ENTRE DISPAROS: Añade una pequeña variación al tiempo de recarga (+/- 0.4s)
                float randomJitter = Random.Range(-0.4f, 0.4f);
                nextFireTime = Time.time + Mathf.Max(1.0f, fireRate + randomJitter);
            }
        }
    }

    void FindPlayer()
    {
        TankController controller = FindFirstObjectByType<TankController>();
        if (controller != null)
        {
            playerTarget = controller.transform;
        }
    }

    void HandleDynamicMovement(float distanceToPlayer)
    {
        // Cambiar de táctica cada cierto tiempo o si choca con algo
        if (Time.time >= nextStateChangeTime || IsObstacleAhead())
        {
            ChooseNewMovementState(distanceToPlayer);
        }

        Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
        directionToPlayer.y = 0;

        Vector3 strafeVector = Vector3.Cross(directionToPlayer, Vector3.up) * strafeDirection;
        Vector3 moveDirection = Vector3.zero;

        // Comportamiento según el estado actual
        switch (currentState)
        {
            case MovementState.Approaching:
                // Se acerca en zig-zag
                moveDirection = directionToPlayer + (strafeVector * 0.4f);
                // Si se acercó mucho, lo forzamos a retroceder
                if (distanceToPlayer < minCombatDistance) ChooseNewMovementState(distanceToPlayer);
                break;

            case MovementState.Retreating:
                // Huye / toma distancia
                moveDirection = -directionToPlayer + (strafeVector * 0.5f);
                break;

            case MovementState.Orbiting:
                // Da vueltas perfectas alrededor del jugador
                moveDirection = strafeVector;
                break;

            case MovementState.Wandering:
                // Movimiento más libre por el mapa (mitad de lado, mitad alejándose/acercándose al azar)
                float randomForward = Random.value > 0.5f ? 0.5f : -0.5f;
                moveDirection = strafeVector + (directionToPlayer * randomForward);
                break;
        }

        moveDirection.Normalize();

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, bodyRotationSpeed * Time.deltaTime);

            Vector3 targetPos = transform.position + moveDirection * moveSpeed * Time.deltaTime;

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

    void ChooseNewMovementState(float distanceToPlayer)
    {
        strafeDirection = Random.value > 0.5f ? 1 : -1; // Elegir izquierda o derecha
        nextStateChangeTime = Time.time + Random.Range(2.0f, 4.0f); // Mantiene la decisión por 2 a 4 segundos

        if (distanceToPlayer < minCombatDistance)
        {
            // Si está muy cerca, SÍ O SÍ huye
            currentState = MovementState.Retreating;
        }
        else if (distanceToPlayer > maxCombatDistance)
        {
            // Si está muy lejos, a veces se acerca, a veces solo da vueltas por ahí
            currentState = Random.value > 0.6f ? MovementState.Approaching : MovementState.Wandering;
        }
        else
        {
            // Distancia media (zona segura): Decide al azar si alejarse, orbitar o vagar
            float rand = Random.value;
            if (rand < 0.33f) currentState = MovementState.Orbiting;
            else if (rand < 0.66f) currentState = MovementState.Wandering;
            else currentState = MovementState.Retreating; // A veces decide alejarse para no ser predecible
        }
    }

    bool IsObstacleAhead()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.05f;

        if (Physics.SphereCast(origin, 0.06f, transform.forward, out hit, obstacleCheckDistance))
        {
            if (hit.collider.gameObject != gameObject &&
               (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<TankHealth>() != null))
            {
                return true;
            }
        }
        return false;
    }

    void AimTurretAtPlayer()
    {
        if (turretTransform == null || playerTarget == null) return;

        Vector3 targetDirection = playerTarget.position - turretTransform.position;
        targetDirection.y = 0;

        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            turretTransform.rotation = Quaternion.Slerp(turretTransform.rotation, targetRotation, turretRotationSpeed * Time.deltaTime);
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null || muzzlePoint == null) return;

        Vector3 spawnPosition = muzzlePoint.position + (muzzlePoint.forward * 0.15f);
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, muzzlePoint.rotation);

        Collider bulletCollider = bullet.GetComponent<Collider>();
        Collider[] tankColliders = GetComponentsInChildren<Collider>();
        if (bulletCollider != null)
        {
            foreach (Collider col in tankColliders)
            {
                Physics.IgnoreCollision(bulletCollider, col);
            }
        }

        TankBullet bulletScript = bullet.GetComponent<TankBullet>();
        if (bulletScript != null)
        {
            bulletScript.damage = bulletDamage;
            bulletScript.SetShooter(gameObject);
            bulletScript.SetGroundYLevel(transform.position.y);
        }

        bullet.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);

        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            bulletRb.isKinematic = false;
            bulletRb.useGravity = false;
            bulletRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            bulletRb.linearVelocity = muzzlePoint.forward * bulletSpeed;
        }

        Destroy(bullet, 4f);

        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
            flash.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            Destroy(flash, 0.3f);
        }
    }
}