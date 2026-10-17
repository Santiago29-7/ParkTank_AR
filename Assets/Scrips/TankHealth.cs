using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TankHealth : MonoBehaviour
{
    [Header("Configuración de Salud")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Configuración de UI")]
    public Slider healthSlider;
    public TMP_Text healthText;

    [Header("Configuración de Blindaje")]
    public bool hasShield = false;
    public GameObject shieldVFX;

    [Header("Efectos de Muerte y Explosión")]
    public GameObject deathVFXPrefab;    // Prefab 'Impacto-proyectil'
    public AudioClip deathSFX;           // Sonido de explosión
    public float deathVFXScale = 0.35f;  // Escala de la explosión
    public Color burntColor = new Color(0.15f, 0.15f, 0.15f); // Color metal quemado
    public float wreckDuration = 6.0f;   // Tiempo que permanecen los restos

    [Header("Ajustes de Torreta Voladora")]
    public float turretLaunchForce = 2.2f;       // Fuerza vertical (altura del salto)
    public float turretHorizontalSpread = 1.0f;  // Fuerza horizontal (desplazamiento)

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;

        // Buscar Slider en los objetos hijos (para enemigos)
        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>();
        }

        // Si es el Jugador, buscar la barra en la interfaz global
        if (healthSlider == null && (CompareTag("Player") || gameObject.name.Contains("Player")))
        {
            GameObject sliderObj = GameObject.Find("HealthBarSlider");
            if (sliderObj != null)
            {
                healthSlider = sliderObj.GetComponent<Slider>();
            }
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthText == null && (CompareTag("Player") || gameObject.name.Contains("Player")))
        {
            GameObject textObj = GameObject.Find("HealthTextNumber");
            if (textObj != null)
            {
                healthText = textObj.GetComponent<TMP_Text>();
            }
        }

        FindShieldReference();

        if (shieldVFX != null)
        {
            shieldVFX.SetActive(hasShield);
        }

        UpdateUI();
    }

    void FindShieldReference()
    {
        if (shieldVFX == null)
        {
            Transform foundShield = transform.Find("escudo-hexagonal");
            if (foundShield != null)
            {
                shieldVFX = foundShield.gameObject;
            }
        }
    }

    public void ActivateShield()
    {
        FindShieldReference();
        hasShield = true;

        if (shieldVFX != null)
        {
            shieldVFX.SetActive(true);
            Debug.Log("¡Blindaje activado! Escudo encendido visualmente.");
        }
    }

    public void HealPercent(float percentage)
    {
        if (isDead) return;
        float healAmount = maxHealth * percentage;
        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
        UpdateUI();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        if (hasShield)
        {
            hasShield = false;
            if (shieldVFX != null)
            {
                shieldVFX.SetActive(false);
            }
            Debug.Log("¡El blindaje absorbió el impacto! Daño evitado.");
            return;
        }

        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        UpdateUI();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void UpdateUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = Mathf.RoundToInt(currentHealth).ToString();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[TankHealth] {gameObject.name} destruido.");

        // DETENER TODOS LOS SONIDOS DEL TANQUE (Motor, movimiento, etc.)
        AudioSource[] activeAudioSources = GetComponentsInChildren<AudioSource>();
        foreach (AudioSource audio in activeAudioSources)
        {
            audio.Stop();
        }

        // 1. Instanciar explosión
        if (deathVFXPrefab != null)
        {
            GameObject explosion = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            explosion.transform.localScale = Vector3.one * deathVFXScale;
            Destroy(explosion, 2.5f);
        }

        // 2. Reproducir audio de explosión
        if (deathSFX != null)
        {
            AudioSource.PlayClipAtPoint(deathSFX, transform.position, 1.0f);
        }

        // 3. Desactivar scripts de movimiento, controles y colisiones
        EnemyTankAI aiScript = GetComponent<EnemyTankAI>();
        if (aiScript != null) aiScript.enabled = false;

        TankController playerScript = GetComponent<TankController>();
        if (playerScript != null) playerScript.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Ocultar barra de vida flotante
        Canvas healthCanvas = GetComponentInChildren<Canvas>();
        if (healthCanvas != null) healthCanvas.gameObject.SetActive(false);

        // --- SI ES EL JUGADOR: Disparar la pantalla y sonido de Game Over ---
        if (CompareTag("Player") || gameObject.name.Contains("Player") || playerScript != null)
        {
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.TriggerGameOver();
            }
        }

        // 4. Cambiar el color de todas las mallas a metal quemado
        ApplyBurntMetalEffect();

        // 5. Desacoplar torreta y lanzar
        StartCoroutine(DetachTurretAndCleanup());
    }

    void ApplyBurntMetalEffect()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            if (rend is CanvasRenderer) continue;

            foreach (Material mat in rend.materials)
            {
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", burntColor);
                }
                else if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", burntColor);
                }
            }
        }
    }

    IEnumerator DetachTurretAndCleanup()
    {
        float groundY = transform.position.y;

        Transform turret = transform.Find("Torreta");
        if (turret == null)
        {
            foreach (Transform child in transform)
            {
                if (child.name.ToLower().Contains("torreta") || child.name.ToLower().Contains("turret"))
                {
                    turret = child;
                    break;
                }
            }
        }

        if (turret != null)
        {
            turret.parent = null; // Separar del chasis

            Rigidbody turretRb = turret.gameObject.GetComponent<Rigidbody>();
            if (turretRb == null)
            {
                turretRb = turret.gameObject.AddComponent<Rigidbody>();
            }

            turretRb.isKinematic = false;
            turretRb.useGravity = true;

            // Impulso vertical y horizontal
            Vector3 forceDirection = Vector3.up * turretLaunchForce + Random.insideUnitSphere * turretHorizontalSpread;
            turretRb.AddForce(forceDirection, ForceMode.Impulse);
            turretRb.AddTorque(Random.insideUnitSphere * 8f, ForceMode.Impulse);

            StartCoroutine(ClampTurretToGround(turret, turretRb, groundY));
        }

        // Esperar el tiempo configurado en la mesa
        yield return new WaitForSeconds(wreckDuration);

        // Desvanecimiento / Encogimiento progresivo simultáneo
        Vector3 initialScale = transform.localScale;
        Vector3 turretInitialScale = (turret != null) ? turret.localScale : Vector3.zero;

        float elapsedTime = 0f;
        float duration = 1.2f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // Encoger el chasis
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, progress);

            // Encoger la torreta al mismo tiempo
            if (turret != null)
            {
                turret.localScale = Vector3.Lerp(turretInitialScale, Vector3.zero, progress);
            }

            yield return null;
        }

        if (turret != null)
        {
            Destroy(turret.gameObject);
        }

        Destroy(gameObject);
    }

    IEnumerator ClampTurretToGround(Transform turret, Rigidbody turretRb, float groundY)
    {
        float targetGroundY = groundY + 0.01f;

        yield return new WaitForSeconds(0.25f);

        while (turret != null)
        {
            if (turret.position.y <= targetGroundY)
            {
                Vector3 fixedPos = turret.position;
                fixedPos.y = targetGroundY;
                turret.position = fixedPos;

                if (turretRb != null)
                {
                    turretRb.linearVelocity = Vector3.zero;
                    turretRb.angularVelocity = Vector3.zero;
                    turretRb.isKinematic = true;
                }
                break;
            }
            yield return null;
        }
    }
}