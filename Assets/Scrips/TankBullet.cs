using UnityEngine;

public class TankBullet : MonoBehaviour
{
    [Header("Efectos de Impacto")]
    public GameObject explosionVFXPrefab;
    public AudioClip impactSFX;
    public float maxLifetime = 5f;

    [Header("Detección de Suelo Virtual (AR)")]
    public bool useVirtualGround = true;
    public float groundYLevel = -0.1f;

    private bool hasImpacted = false;
    private float spawnTime;

    void Start()
    {
        spawnTime = Time.time;
        Destroy(gameObject, maxLifetime);

        // Ignorar colisión con la cámara principal si esta tiene collider
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

    void Update()
    {
        if (hasImpacted) return;

        // Si la bala cae por debajo de la altura del piso virtual (con un pequeño margen de tiempo)
        if (useVirtualGround && (Time.time - spawnTime) > 0.05f && transform.position.y <= groundYLevel)
        {
            TriggerFallbackImpact();
        }
    }

    public void SetGroundYLevel(float tankYPosition)
    {
        groundYLevel = tankYPosition;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasImpacted) return;

        // 1. Mostrar en consola con qué objeto chocó realmente la bala
        Debug.Log($"[TankBullet] Chocó con: {collision.gameObject.name} en posición Y: {collision.contacts[0].point.y}");

        // 2. Si el choque ocurre muy por encima del nivel del tanque (y no es un enemigo), ignorar la colisión
        float contactY = collision.contacts[0].point.y;
        if (contactY > (groundYLevel + 0.3f) && !collision.gameObject.CompareTag("Enemy"))
        {
            Debug.LogWarning($"[TankBullet] Colisión en aire ignorada sobre objeto: {collision.gameObject.name}");
            return;
        }

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
        // Fuerza la posición Y exacta del piso virtual donde está el tanque
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