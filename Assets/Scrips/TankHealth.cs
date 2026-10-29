using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class TankHealth : MonoBehaviourPun
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
        if (!PhotonNetwork.IsConnected || photonView == null || photonView.ViewID == 0)
        {
            RPC_ActivateShield(duration);
            return;
        }

        photonView.RPC("RPC_ActivateShield", RpcTarget.All, duration);
    }

    [PunRPC]
    void RPC_ActivateShield(float duration)
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

    // --- SISTEMA DE DAÑO CORREGIDO ---
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        int shooterActorNumber = (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
            ? PhotonNetwork.LocalPlayer.ActorNumber
            : -1;

        // Si el tanque es de la IA (ViewID <= 0) o no hay red, le aplica el daño directamente a ESTE objeto
        if (!PhotonNetwork.IsConnected || photonView == null || photonView.ViewID <= 0)
        {
            RPC_ApplyDamageAndCheckKill(amount, shooterActorNumber);
            return;
        }

        // Si es un jugador real en red, sincroniza el daño a través de Photon
        photonView.RPC("RPC_ApplyDamageAndCheckKill", RpcTarget.All, amount, shooterActorNumber);
    }

    [PunRPC]
    void RPC_ApplyDamageAndCheckKill(float amount, int shooterActorNumber)
    {
        if (isDead) return;

        if (hasShield)
        {
            Debug.Log("[TankHealth] ¡Daño bloqueado por el escudo activo!");
            return;
        }

        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        UpdateUI();

        // Mostrar texto flotante de daño recibido
        ShowDamageNumber(amount);

        if (currentHealth <= 0f)
        {
            isDead = true;

            // 1. Efectos de explosión
            PlayDeathEffects();

            // 2. Determinar si fuiste tú quien realizó el disparo letal
            bool isLocalShooter = !PhotonNetwork.IsConnected ||
                                  (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.ActorNumber == shooterActorNumber);

            if (isLocalShooter)
            {
                GameManager gm = FindFirstObjectByType<GameManager>();
                if (gm != null) gm.AddKill();
            }

            // 3. Pantalla de Game Over si este era tu tanque
            TankController playerScript = GetComponent<TankController>();
            if (playerScript != null && (photonView == null || photonView.IsMine))
            {
                GameManager gm = FindFirstObjectByType<GameManager>();
                if (gm != null) gm.TriggerGameOver();
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

        // Color naranja/amarillo si el impacto es fuerte (>20), o rojo para estándar
        damageText.color = amount > 20f ? new Color(1f, 0.5f, 0f, 1f) : new Color(1f, 0.2f, 0.2f, 1f);
        damageText.gameObject.SetActive(true);

        float duration = 0.85f;
        float elapsed = 0f;

        Vector3 startPos = originalDamageTextPos;
        Vector3 targetPos = startPos + new Vector3(0f, 18f, 0f); // Elevación gradual en Canvas

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

        if (!PhotonNetwork.IsConnected || photonView == null || photonView.ViewID == 0)
        {
            RPC_Heal(percentage);
            return;
        }

        if (photonView.IsMine)
        {
            photonView.RPC("RPC_Heal", RpcTarget.All, percentage);
        }
    }

    [PunRPC]
    void RPC_Heal(float percentage)
    {
        float healAmount = maxHealth * percentage;
        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
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

        if (PhotonNetwork.IsConnected && photonView != null && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else if (!PhotonNetwork.IsConnected || photonView == null || photonView.ViewID == 0)
        {
            Destroy(gameObject);
        }
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