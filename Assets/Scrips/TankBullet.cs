using UnityEngine;
using Photon.Pun;

public class TankBullet : MonoBehaviour
{
    [Header("Configuración de Daño")]
    public float damage = 15f;
    private float damageMultiplier = 1f; // Multiplicador de daño añadido

    [Header("Efectos de Impacto")]
    public GameObject explosionVFXPrefab;
    public AudioClip impactSFX;
    public float maxLifetime = 5f;

    [Header("Detección de Suelo Virtual (AR)")]
    public bool useVirtualGround = true;
    public float groundYLevel = -0.1f;

    private bool hasImpacted = false;
    private float spawnTime;
    private GameObject shooterOwner;

    void Start()
    {
        spawnTime = Time.time;
        Destroy(gameObject, maxLifetime);

        if (Camera.main != null)
        {
            Collider cameraCollider = Camera.main.GetComponent<Collider>();
            Collider bulletCollider = GetComponent<Collider>();
            if (cameraCollider != null && bulletCollider != null)
            {
                Physics.IgnoreCollision(bulletCollider, cameraCollider);
            }
        }
    }

    public void SetShooter(GameObject owner)
    {
        shooterOwner = owner;
        Collider bulletCollider = GetComponent<Collider>();
        if (bulletCollider != null && owner != null)
        {
            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
            foreach (Collider col in ownerColliders)
            {
                Physics.IgnoreCollision(bulletCollider, col);
            }
        }
    }

    public void SetGroundYLevel(float tankYPosition)
    {
        groundYLevel = tankYPosition;
    }

    // --- NUEVA FUNCIÓN PARA ACTIVAR EL DOBLE DAÑO ---
    public void EnableDoubleDamage()
    {
        damageMultiplier = 2f;

        // Aumenta el tamaño visual de la bala un 50% para que se note el poder extra
        transform.localScale *= 1.5f;
    }

    void Update()
    {
        if (hasImpacted) return;
        if (useVirtualGround && (Time.time - spawnTime) > 0.05f && transform.position.y <= groundYLevel)
        {
            TriggerFallbackImpact();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasImpacted) return;

        if (shooterOwner != null && (collision.gameObject == shooterOwner || collision.transform.IsChildOf(shooterOwner.transform)))
        {
            return;
        }

        TankHealth health = collision.gameObject.GetComponentInParent<TankHealth>();

        if (health != null)
        {
            // Se calcula el daño final multiplicando la base por el multiplicador
            float finalDamage = damage * damageMultiplier;
            health.TakeDamage(finalDamage);

            ContactPoint hit = collision.contacts[0];
            SpawnImpact(hit.point, Quaternion.LookRotation(hit.normal));
            return;
        }

        float contactY = collision.contacts[0].point.y;
        if (useVirtualGround && contactY > (groundYLevel + 0.3f) && !collision.gameObject.CompareTag("Enemy") && !collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        ContactPoint contact = collision.contacts[0];
        Quaternion hitRotation = Quaternion.LookRotation(contact.normal);

        if (explosionVFXPrefab != null) hitRotation *= explosionVFXPrefab.transform.rotation;
        SpawnImpact(contact.point, hitRotation);
    }

    private void TriggerFallbackImpact()
    {
        Vector3 impactPosition = new Vector3(transform.position.x, groundYLevel, transform.position.z);
        Quaternion hitRotation = Quaternion.LookRotation(Vector3.up);

        if (explosionVFXPrefab != null) hitRotation *= explosionVFXPrefab.transform.rotation;
        SpawnImpact(impactPosition, hitRotation);
    }

    private void SpawnImpact(Vector3 position, Quaternion rotation)
    {
        hasImpacted = true;
        if (explosionVFXPrefab != null) Instantiate(explosionVFXPrefab, position, rotation);
        if (impactSFX != null) AudioSource.PlayClipAtPoint(impactSFX, position, 1.0f);
        Destroy(gameObject);
    }
}