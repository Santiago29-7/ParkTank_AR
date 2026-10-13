using UnityEngine;

public class TankBullet : MonoBehaviour
{
    [Header("Efectos de Impacto")]
    public GameObject explosionVFXPrefab; // Prefab de partículas de explosión
    public AudioClip impactSFX;          // Sonido de impacto/explosión
    public float maxLifetime = 5f;       // Tiempo límite de vida si no choca con nada

    private bool hasImpacted = false;

    void Start()
    {
        // Destrucción por defecto en caso de que la bala salga volando al infinito
        Destroy(gameObject, maxLifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Evita que el impacto se ejecute múltiples veces si toca varias superficies a la vez
        if (hasImpacted) return;
        hasImpacted = true;

        // Instanciar efecto de explosión en la posición del choque
        if (explosionVFXPrefab != null)
        {
            GameObject explosion = Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, 2.5f);
        }

        // Reproducir sonido en la posición 3D del impacto
        if (impactSFX != null)
        {
            AudioSource.PlayClipAtPoint(impactSFX, transform.position, 1.0f);
        }

        // Destruir el proyectil inmediatamente al chocar
        Destroy(gameObject);
    }
}