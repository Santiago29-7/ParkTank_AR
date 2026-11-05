using UnityEngine;

public class EnemyTankAI : MonoBehaviour
{
    [Header("Referencias de Torreta y Disparo")]
    public Transform turretTransform;
    public Transform muzzlePoint;
    public GameObject bulletPrefab;
    public GameObject muzzleFlashPrefab;

    [Header("Configuración de Ataque")]
    public float detectionRange = 3.0f;
    public float turretRotationSpeed = 8f;
    public float fireRate = 2.5f;
    public float bulletSpeed = 4f;
    public float bulletDamage = 15f;

    [Header("Movimiento Táctico Jugador")]
    public float moveSpeed = 0.18f;
    public float bodyRotationSpeed = 5f;
    public float minCombatDistance = 0.45f;
    public float maxCombatDistance = 0.90f;
    public float changePositionInterval = 2.5f;

    [Header("Detección de Choques / Obstáculos")]
    public float obstacleCheckDistance = 0.18f;

    private Transform playerTarget;
    private Vector3 currentMoveTarget;
    private float nextFireTime;
    private float nextMoveChangeTime;
    private bool isPausingTactically = false;
    private float pauseEndTime = 0f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
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
            // 1. Apuntar torreta hacia el jugador
            AimTurretAtPlayer();

            // 2. Gestionar movimiento local táctico (strafing, giros, pausas)
            HandleDynamicMovement(distanceToPlayer);

            // 3. Disparar según la cadencia
            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    void FindPlayer()
    {
        TankController controller = FindFirstObjectByType<TankController>();
        if (controller != null)
        {
            playerTarget = controller.transform;
            PickNewMoveTarget(Vector3.Distance(transform.position, playerTarget.position));
        }
    }

    void HandleDynamicMovement(float distanceToPlayer)
    {
        // Manejo de pausas tácticas de tiro
        if (isPausingTactically)
        {
            if (Time.time >= pauseEndTime)
            {
                isPausingTactically = false;
                PickNewMoveTarget(distanceToPlayer);
            }
            return;
        }

        // Si detecta obstáculos o vence el temporizador de posición
        if (IsObstacleAhead())
        {
            PickNewMoveTarget(distanceToPlayer);
            nextMoveChangeTime = Time.time + changePositionInterval;
        }
        else if (Time.time >= nextMoveChangeTime || Vector3.Distance(transform.position, currentMoveTarget) < 0.08f)
        {
            // Probabilidad de pausas cortas
            if (Random.value < 0.20f)
            {
                isPausingTactically = true;
                pauseEndTime = Time.time + Random.Range(0.8f, 1.5f);
                return;
            }

            PickNewMoveTarget(distanceToPlayer);
            nextMoveChangeTime = Time.time + Random.Range(changePositionInterval - 0.8f, changePositionInterval + 1.2f);
        }

        // Desplazamiento hacia la posición seleccionada
        Vector3 moveDirection = (currentMoveTarget - transform.position);
        moveDirection.y = 0;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, bodyRotationSpeed * Time.deltaTime);

            Vector3 targetPos = Vector3.MoveTowards(transform.position, currentMoveTarget, moveSpeed * Time.deltaTime);

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

    void PickNewMoveTarget(float distanceToPlayer)
    {
        if (playerTarget == null) return;

        Vector3 toPlayer = (playerTarget.position - transform.position).normalized;
        Vector3 perpendicularLeft = Vector3.Cross(toPlayer, Vector3.up);
        Vector3 perpendicularRight = -perpendicularLeft;

        // Retroceder si el jugador está demasiado cerca
        if (distanceToPlayer < minCombatDistance)
        {
            Vector3 retreatDir = (-toPlayer + (Random.value > 0.5f ? perpendicularLeft : perpendicularRight) * 0.5f).normalized;
            currentMoveTarget = transform.position + retreatDir * Random.Range(0.35f, 0.65f);
            return;
        }

        // Patrones de movimiento variados (Flanqueo, Strafe, Reubicación)
        int behaviorChoice = Random.Range(0, 4);

        switch (behaviorChoice)
        {
            case 0:
                currentMoveTarget = transform.position + perpendicularLeft * Random.Range(0.3f, 0.7f);
                break;

            case 1:
                currentMoveTarget = transform.position + perpendicularRight * Random.Range(0.3f, 0.7f);
                break;

            case 2:
                Vector3 diagonalDir = (perpendicularLeft * (Random.value > 0.5f ? 1f : -1f) + toPlayer * Random.Range(-0.4f, 0.4f)).normalized;
                currentMoveTarget = transform.position + diagonalDir * Random.Range(0.4f, 0.8f);
                break;

            case 3:
            default:
                Vector2 randomCircle = Random.insideUnitCircle.normalized;
                float randomDistance = Random.Range(minCombatDistance, maxCombatDistance);
                currentMoveTarget = new Vector3(
                    playerTarget.position.x + (randomCircle.x * randomDistance),
                    transform.position.y,
                    playerTarget.position.z + (randomCircle.y * randomDistance)
                );
                break;
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

        GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);

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