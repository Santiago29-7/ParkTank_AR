using UnityEngine;

public class TankBullet : MonoBehaviour
{
    [Header("Configuración de Daño")]
    public float damage = 15f; // Cantidad de vida que resta al impactar

    [Header("Efectos de Impacto")]
    public GameObject explosionVFXPrefab;
    public AudioClip impactSFX;
    public float maxLifetime = 5f;

    [Header("Detección de Suelo Virtual (AR)")]
    public bool useVirtualGround = true;
    public float groundYLevel = -0.1f;

    private bool hasImpacted = false;
    private float spawnTime;
    private GameObject shooterOwner; // Referencia de quién disparó la bala

    void Start()
    {
        spawnTime = Time.time;
        Destroy(gameObject, maxLifetime);

        // Ignorar colisión con la cámara principal
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

    // Asigna el dueño del disparo e ignora sus colisionadores para evitar autodestrucción
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

        // Ignorar si colisiona accidentalmente con quien la disparó
        if (shooterOwner != null && (collision.gameObject == shooterOwner || collision.transform.IsChildOf(shooterOwner.transform)))
        {
            return;
        }

        Debug.Log($"[TankBullet] Chocó con: {collision.gameObject.name} en Y: {collision.contacts[0].point.y}");

        // Buscar componente de salud (en el objeto o en cualquiera de sus padres)
        TankHealth health = collision.gameObject.GetComponentInParent<TankHealth>();

        // 1. Si golpea a cualquier tanque con salud (Jugador o Enemigo), aplica el daño
        if (health != null)
        {
            health.TakeDamage(damage);
            Debug.Log($"[TankBullet] ¡IMPACTO DIRECTO A {collision.gameObject.name}! Salud restante: {health.currentHealth}");

            ContactPoint hit = collision.contacts[0];
            SpawnImpact(hit.point, Quaternion.LookRotation(hit.normal));
            return;
        }

        // 2. Si no tiene salud y la colisión es en el aire, ignorar objetos de adorno
        float contactY = collision.contacts[0].point.y;
        if (useVirtualGround && contactY > (groundYLevel + 0.3f) && !collision.gameObject.CompareTag("Enemy") && !collision.gameObject.CompareTag("Player"))
        {
            Debug.LogWarning($"[TankBullet] Colisión en aire ignorada sobre objeto: {collision.gameObject.name}");
            return;
        }

        // 3. Impacto contra suelo o paredes
        ContactPoint contact = collision.contacts[0];
        Quaternion hitRotation = Quaternion.LookRotation(contact.normal);

        if (explosionVFXPrefab != null)
        {
            hitRotation *= explosionVFXPrefab.transform.rotation;
        }

        SpawnImpact(contact.point, hitRotation);
    }

    private void TriggerFallbackImpact()
    {
        Vector3 impactPosition = new Vector3(transform.position.x, groundYLevel, transform.position.z);
        Quaternion hitRotation = Quaternion.LookRotation(Vector3.up);

        if (explosionVFXPrefab != null)
        {
            hitRotation *= explosionVFXPrefab.transform.rotation;
        }

        SpawnImpact(impactPosition, hitRotation);
    }

    private void SpawnImpact(Vector3 position, Quaternion rotation)
    {
        hasImpacted = true;

        if (explosionVFXPrefab != null)
        {
            Instantiate(explosionVFXPrefab, position, rotation);
        }

        if (impactSFX != null)
        {
            AudioSource.PlayClipAtPoint(impactSFX, position, 1.0f);
        }

        Destroy(gameObject);
    }
}