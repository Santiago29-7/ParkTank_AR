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
    public TMP_Text damageText; // Referencia al texto flotante de daño

    [Header("Configuración de Blindaje")]
    public bool hasShield = false;
    public GameObject shieldVFX;
    private Coroutine shieldCoroutine;

    [Header("Efectos de Muerte y Explosión")]
    public GameObject deathVFXPrefab;
    public AudioClip deathSFX;
    public float deathVFXScale = 0.35f;
    public Color burntColor = new Color(0.15f, 0.15f, 0.15f);
    public float wreckDuration = 6.0f;

    [Header("Ajustes de Torreta Voladora")]
    public float turretLaunchForce = 2.2f;
    public float turretHorizontalSpread = 1.0f;

    private bool isDead = false;
    private Coroutine damageTextCoroutine;
    private Vector3 originalDamageTextPos;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthSlider == null) healthSlider = GetComponentInChildren<Slider>();
        if (healthSlider == null && (CompareTag("Player") || gameObject.name.Contains("Player")))
        {
            GameObject sliderObj = GameObject.Find("HealthBarSlider");
            if (sliderObj != null) healthSlider = sliderObj.GetComponent<Slider>();
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthText == null && (CompareTag("Player") || gameObject.name.Contains("Player")))
        {
            GameObject textObj = GameObject.Find("HealthTextNumber");
            if (textObj != null) healthText = textObj.GetComponent<TMP_Text>();
        }

        // Búsqueda e inicialización del DamageText
        if (damageText == null)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text t in texts)
            {
                if (t.gameObject.name.ToLower().Contains("damage"))
                {
                    damageText = t;
                    break;
                }
            }
        }

        if (damageText != null)
        {
            originalDamageTextPos = damageText.transform.localPosition;
            damageText.gameObject.SetActive(false);
        }

        FindShieldReference();
        if (shieldVFX != null) shieldVFX.SetActive(hasShield);
        UpdateUI();
    }

    void FindShieldReference()
    {
        if (shieldVFX == null)
        {
            Transform foundShield = transform.Find("escudo-hexagonal");
            if (foundShield != null) shieldVFX = foundShield.gameObject;
        }
    }

    // --- SISTEMA DE ESCUDO POR TIEMPO ---
    public void ActivateShield(float duration = 10f)
    {
        FindShieldReference();

        if (shieldCoroutine != null)
        {
            StopCoroutine(shieldCoroutine);
        }

        shieldCoroutine = StartCoroutine(ShieldTimerRoutine(duration));
    }

    private IEnumerator ShieldTimerRoutine(float duration)
    {
        hasShield = true;
        if (shieldVFX != null) shieldVFX.SetActive(true);

        yield return new WaitForSeconds(duration);

        hasShield = false;
        if (shieldVFX != null) shieldVFX.SetActive(false);
        shieldCoroutine = null;
    }

    // --- SISTEMA DE DAÑO Y MUERTE LOCAL CON ESTADÍSTICAS ---
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        if (hasShield)
        {
            Debug.Log("[TankHealth] ¡Daño bloqueado por el escudo activo!");
            return;
        }

        currentHealth = Mathf.Max(currentHealth - damageAmount, 0f);
        UpdateUI();
        ShowDamageNumber(damageAmount);

        if (currentHealth <= 0f)
        {
            isDead = true;

            // 1. Reproducir explosión y sonidos
            PlayDeathEffects();

            GameManager gm = FindFirstObjectByType<GameManager>();
            EnemyTankAI aiComponent = GetComponent<EnemyTankAI>();
            TankController playerComponent = GetComponent<TankController>();

            // 2. Si se destruye un TANQUE ENEMIGO (IA)
            if (aiComponent != null)
            {
                DatabaseManager.AddTankDestroyed(); // Incrementa tanques destruidos en estadísticas

                if (gm != null)
                {
                    gm.AddKill();
                }
            }
            // 3. Si se destruye el TANQUE DEL JUGADOR
            else if (playerComponent != null)
            {
                DatabaseManager.AddPlayerElimination(); // Incrementa eliminaciones sufridas
                DatabaseManager.AddGameLost();          // Incrementa partidas perdidas

                if (gm != null)
                {
                    gm.TriggerGameOver();
                }
            }
        }
    }

    private void ShowDamageNumber(float amount)
    {
        if (damageText == null) return;

        if (damageTextCoroutine != null)
        {
            StopCoroutine(damageTextCoroutine);
        }

        damageTextCoroutine = StartCoroutine(ShowDamageRoutine(amount));
    }

    private IEnumerator ShowDamageRoutine(float amount)
    {
        damageText.transform.localPosition = originalDamageTextPos;
        damageText.text = "-" + Mathf.RoundToInt(amount).ToString();

        damageText.color = amount > 20f ? new Color(1f, 0.5f, 0f, 1f) : new Color(1f, 0.2f, 0.2f, 1f);
        damageText.gameObject.SetActive(true);

        float duration = 0.85f;
        float elapsed = 0f;

        Vector3 startPos = originalDamageTextPos;
        Vector3 targetPos = startPos + new Vector3(0f, 18f, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            damageText.transform.localPosition = Vector3.Lerp(startPos, targetPos, progress);

            Color c = damageText.color;
            c.a = Mathf.Lerp(1f, 0f, progress);
            damageText.color = c;

            yield return null;
        }

        damageText.gameObject.SetActive(false);
        damageText.transform.localPosition = originalDamageTextPos;
    }

    void PlayDeathEffects()
    {
        AudioSource[] activeAudioSources = GetComponentsInChildren<AudioSource>();
        foreach (AudioSource audio in activeAudioSources) audio.Stop();

        if (deathVFXPrefab != null)
        {
            GameObject explosion = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            explosion.transform.localScale = Vector3.one * deathVFXScale;
            Destroy(explosion, 2.5f);
        }

        if (deathSFX != null)
        {
            AudioSource.PlayClipAtPoint(deathSFX, transform.position, 1.0f);
        }

        EnemyTankAI aiScript = GetComponent<EnemyTankAI>();
        if (aiScript != null) aiScript.enabled = false;

        TankController playerScript = GetComponent<TankController>();
        if (playerScript != null) playerScript.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Canvas healthCanvas = GetComponentInChildren<Canvas>();
        if (healthCanvas != null) healthCanvas.gameObject.SetActive(false);

        ApplyBurntMetalEffect();
        StartCoroutine(DetachTurretAndCleanup());
    }

    public void HealPercent(float percentage)
    {
        if (isDead) return;
        float healAmount = maxHealth * percentage;
        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
        UpdateUI();
    }

    public void HealAmount(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateUI();
    }

    void UpdateUI()
    {
        if (healthSlider != null) healthSlider.value = currentHealth;
        if (healthText != null) healthText.text = Mathf.RoundToInt(currentHealth).ToString();
    }

    void ApplyBurntMetalEffect()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            if (rend is CanvasRenderer) continue;
            foreach (Material mat in rend.materials)
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", burntColor);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", burntColor);
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
            turret.parent = null;
            Rigidbody turretRb = turret.gameObject.GetComponent<Rigidbody>();
            if (turretRb == null) turretRb = turret.gameObject.AddComponent<Rigidbody>();

            turretRb.isKinematic = false;
            turretRb.useGravity = true;

            Vector3 forceDirection = Vector3.up * turretLaunchForce + Random.insideUnitSphere * turretHorizontalSpread;
            turretRb.AddForce(forceDirection, ForceMode.Impulse);
            turretRb.AddTorque(Random.insideUnitSphere * 8f, ForceMode.Impulse);

            StartCoroutine(ClampTurretToGround(turret, turretRb, groundY));
        }

        yield return new WaitForSeconds(wreckDuration);

        Vector3 initialScale = transform.localScale;
        Vector3 turretInitialScale = (turret != null) ? turret.localScale : Vector3.zero;
        float elapsedTime = 0f;
        float duration = 1.2f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, progress);
            if (turret != null) turret.localScale = Vector3.Lerp(turretInitialScale, Vector3.zero, progress);

            yield return null;
        }

        if (turret != null) Destroy(turret.gameObject);

        // Destrucción local del GameObject
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