using UnityEngine;

public class EnemyTankAI : MonoBehaviour
{
    [Header("Referencias de Torreta y Disparo")]
    public Transform turretTransform;
    public Transform muzzlePoint;
    public GameObject bulletPrefab;
    public GameObject muzzleFlashPrefab;

    [Header("Configuración de Ataque")]
    public float detectionRange = 15f;
    public float turretRotationSpeed = 8f;
    public float fireRate = 3f;
    public float bulletSpeed = 6f; // Velocidad de la bala hacia el jugador

    private Transform playerTarget;
    private float nextFireTime;

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
            AimAtPlayer();

            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    void FindPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTarget = player.transform;
            return;
        }

        TankController controller = FindObjectOfType<TankController>();
        if (controller != null)
        {
            playerTarget = controller.transform;
        }
    }

    void AimAtPlayer()
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

        // Crear la bala
        GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);

        // Mantener la escala adecuada para AR (0.08)
        bullet.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);

        // Darle impulso hacia el frente para que avance
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = muzzlePoint.forward * bulletSpeed;
        }

        Destroy(bullet, 3f);

        // Fogonazo del cañón
        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
            flash.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            Destroy(flash, 0.3f);
        }
    }
}