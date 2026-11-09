using System.Collections;
using UnityEngine;

/// <summary>
/// Controla la física, detección de colisiones, cálculo de daño y efectos visuales/sonoros del proyectil del tanque (TankBullet).
/// Administra el ignorado de colisiones con el emisor y la cámara AR, detecta límites de suelo virtual (fallback),
/// aplica multiplicadores de daño (Power-Up Nuke) y previene el fuego amigo entre tanques enemigos.
/// </summary>
public class TankBullet : MonoBehaviour
{
    // ==========================================
    // CONFIGURACIÓN DE DAÑO Y MULTIPLICADORES
    // ==========================================

    [Header("Configuración de Daño")]
    [Tooltip("Daño base infligido al impactar contra un tanque objetivo")]
    public float damage = 15f;

    private float damageMultiplier = 1f; // Multiplicador de daño (aumenta a 2x al recoger el Power-Up Nuke)

    // ==========================================
    // EFECTOS VISUALES Y SONOROS
    // ==========================================

    [Header("Efectos de Impacto")]
    [Tooltip("Prefab del efecto visual de explosión / impacto 3D")]
    public GameObject explosionVFXPrefab;

    [Tooltip("Efecto de sonido (SFX) reproducido al colisionar")]
    public AudioClip impactSFX;

    [Tooltip("Tiempo máximo de vida en segundos antes de autodestruirse si no impacta con nada")]
    public float maxLifetime = 5f;

    // ==========================================
    // DETECCIÓN DE SUELO VIRTUAL (AR)
    // ==========================================

    [Header("Detección de Suelo Virtual (AR)")]
    [Tooltip("Habilita la detección de límite inferior en el eje Y para proyectiles que caen del plano AR")]
    public bool useVirtualGround = true;

    [Tooltip("Nivel Y absoluto en el espacio donde el proyectil explotará automáticamente si cae")]
    public float groundYLevel = -0.1f;

    // ==========================================
    // ESTADO INTERNO Y REFERENCIAS
    // ==========================================

    private bool hasImpacted = false;      // Bandera para evitar múltiples procesamientos de impacto por frame
    private float spawnTime;              // Marca de tiempo en segundos en que la bala fue instanciada
    private GameObject shooterOwner;      // Referencia al tanque emisor que disparó el proyectil

    // ==========================================
    // INICIALIZACIÓN
    // ==========================================

    void Start()
    {
        spawnTime = Time.time;

        // Programa la autodestrucción preventiva para evitar fugas de memoria
        Destroy(gameObject, maxLifetime);

        // Ignora colisiones entre el proyectil y la cámara principal (AR Camera)
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

    // ==========================================
    // MÉTODOS DE CONFIGURACIÓN EXTERNA
    // ==========================================

    /// <summary>
    /// Asigna la referencia del tanque emisor e ignora físicamente la colisión con todos sus colliders.
    /// </summary>
    /// <param name="owner">GameObject del tanque que disparó este proyectil.</param>
    public void SetShooter(GameObject owner)
    {
        shooterOwner = owner;
        Collider bulletCollider = GetComponent<Collider>();

        if (bulletCollider != null && owner != null)
        {
            // Ignora colisiones con la estructura y orugas del propio tanque que disparó
            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
            foreach (Collider col in ownerColliders)
            {
                Physics.IgnoreCollision(bulletCollider, col);
            }
        }
    }

    /// <summary>
    /// Ajusta la altura del suelo virtual en función de la posición Y del tanque en la superficie AR.
    /// </summary>
    public void SetGroundYLevel(float tankYPosition)
    {
        groundYLevel = tankYPosition;
    }

    /// <summary>
    /// Duplica el daño del proyectil (2x) e incrementa su escala visual en un 50% (Modo Nuke Épico).
    /// </summary>
    public void EnableDoubleDamage()
    {
        damageMultiplier = 2f;
        transform.localScale *= 1.5f;
    }

    // ==========================================
    // DETECCIÓN DE SUELO VIRTUAL EN TIEMPO REAL
    // ==========================================

    void Update()
    {
        if (hasImpacted) return;

        // Comprueba si la bala cruzó por debajo del límite de suelo virtual tras un breve margen de seguridad (0.05s)
        if (useVirtualGround && (Time.time - spawnTime) > 0.05f && transform.position.y <= groundYLevel)
        {
            TriggerFallbackImpact();
        }
    }

    // ==========================================
    // PROCESAMIENTO DE COLISIONES FÍSICAS
    // ==========================================

    private void OnCollisionEnter(Collision collision)
    {
        if (hasImpacted) return;

        // 1. Ignorar colisiones con Triggers invisibles (UI 3D, zonas de detección de IA, etc.)
        if (collision.collider.isTrigger) return;

        // 2. Ignorar si la colisión se produce accidentalmente con el propio tanque emisor o sus hijos
        if (shooterOwner != null && (collision.gameObject == shooterOwner || collision.transform.IsChildOf(shooterOwner.transform)))
        {
            return;
        }

        // 3. Buscar el componente TankHealth en el objeto colisionado o en sus padres de jerarquía
        TankHealth health = collision.gameObject.GetComponentInParent<TankHealth>();

        if (health != null)
        {
            // Validación adicional para prevenir autofuego si la referencia es el propio emisor
            if (shooterOwner != null && health.gameObject == shooterOwner) return;

            // PREVENCIÓN DE FUEGO AMIGO ENTRE ENEMIGOS:
            // Si el emisor es un enemigo y el objetivo también es un enemigo, no se aplica daño
            if (shooterOwner != null && shooterOwner.CompareTag("Enemy") && health.gameObject.CompareTag("Enemy"))
            {
                ContactPoint hitFriendly = collision.contacts[0];
                SpawnImpact(hitFriendly.point, Quaternion.LookRotation(hitFriendly.normal));
                return;
            }

            // Aplicación del daño total (daño base * multiplicador)
            float finalDamage = damage * damageMultiplier;
            health.TakeDamage(finalDamage);

            // Generación de impacto visual en el punto exacto de contacto
            ContactPoint hit = collision.contacts[0];
            SpawnImpact(hit.point, Quaternion.LookRotation(hit.normal));
            return;
        }

        // 4. Si chocó contra un obstáculo del escenario o el plano AR, genera el impacto visual/sonoro
        ContactPoint contact = collision.contacts[0];
        Quaternion hitRotation = Quaternion.LookRotation(contact.normal);
        if (explosionVFXPrefab != null) hitRotation *= explosionVFXPrefab.transform.rotation;

        SpawnImpact(contact.point, hitRotation);
    }

    // ==========================================
    // GENERACIÓN DE IMPACTO Y LIMPIEZA
    // ==========================================

    /// <summary>
    /// Genera la explosión de respaldo en el nivel de suelo virtual si la bala escapa de los colliders.
    /// </summary>
    private void TriggerFallbackImpact()
    {
        Vector3 impactPosition = new Vector3(transform.position.x, groundYLevel, transform.position.z);
        Quaternion hitRotation = Quaternion.LookRotation(Vector3.up);

        if (explosionVFXPrefab != null) hitRotation *= explosionVFXPrefab.transform.rotation;
        SpawnImpact(impactPosition, hitRotation);
    }

    /// <summary>
    /// Instancia los efectos de partículas (VFX), reproduce el sonido de explosión (SFX) y destruye la bala.
    /// </summary>
    /// <param name="position">Punto 3D del impacto.</param>
    /// <param name="rotation">Orientación del efecto visual.</param>
    private void SpawnImpact(Vector3 position, Quaternion rotation)
    {
        hasImpacted = true;

        if (explosionVFXPrefab != null) Instantiate(explosionVFXPrefab, position, rotation);
        if (impactSFX != null) AudioSource.PlayClipAtPoint(impactSFX, position, 1.0f);

        Destroy(gameObject);
    }
}