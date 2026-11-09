using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestiona los puntos de vida (HP), la barra de salud en UI, los números flotantes de daño, 
/// la protección por escudo invulnerable, los métodos de curación y el flujo completo de destrucción 
/// del tanque (desprendimiento de torreta por físicas, efecto visual de metal quemado y limpieza de restos).
/// </summary>
public class TankHealth : MonoBehaviour
{
    // ==========================================
    // CONFIGURACIÓN DE SALUD Y UI
    // ==========================================

    [Header("Configuración de Salud")]
    [Tooltip("Salud máxima total del tanque")]
    public float maxHealth = 100f;

    [Tooltip("Salud actual del tanque en tiempo real")]
    public float currentHealth;

    [Header("Configuración de UI")]
    [Tooltip("Slider de la interfaz que representa la barra de vida")]
    public Slider healthSlider;

    [Tooltip("Texto MeshPro que despliega el valor numérico de salud restante")]
    public TMP_Text healthText;

    [Tooltip("Texto emergente flotante que muestra la cantidad de daño recibido")]
    public TMP_Text damageText;

    // ==========================================
    // CONFIGURACIÓN DE BLINDAJE Y ESCUDO
    // ==========================================

    [Header("Configuración de Blindaje")]
    [Tooltip("Indica si el escudo protector invulnerable se encuentra activo")]
    public bool hasShield = false;

    [Tooltip("Objeto 3D o sistema de partículas (VFX) del escudo hexagonal")]
    public GameObject shieldVFX;

    private Coroutine shieldCoroutine; // Referencia al temporizador del escudo activo

    // ==========================================
    // EFECTOS DE MUERTE, DESTRUCCIÓN Y CHATARRA
    // ==========================================

    [Header("Efectos de Muerte y Explosión")]
    [Tooltip("Prefab del efecto de explosión final al ser destruido")]
    public GameObject deathVFXPrefab;

    [Tooltip("Efecto de audio (SFX) ejecutado al destruirse el tanque")]
    public AudioClip deathSFX;

    [Tooltip("Escala personalizada del efecto visual de explosión")]
    public float deathVFXScale = 0.35f;

    [Tooltip("Color de tinte para simular metal chamuscado/quemado tras la explosión")]
    public Color burntColor = new Color(0.15f, 0.15f, 0.15f);

    [Tooltip("Tiempo en segundos que permanecen los restos (chatarra) antes de encogerse y desaparecer")]
    public float wreckDuration = 6.0f;

    [Header("Ajustes de Torreta Voladora")]
    [Tooltip("Fuerza del impulso vertical aplicado a la torreta al desprenderse por la explosión")]
    public float turretLaunchForce = 2.2f;

    [Tooltip("Dispersion horizontal aleatoria aplicada al desprendimiento de la torreta")]
    public float turretHorizontalSpread = 1.0f;

    // ==========================================
    // ESTADO INTERNO Y CANDADOS DE SEGURIDAD
    // ==========================================

    private bool isDead = false; // Candado de seguridad para prevenir múltiples ejecuciones de destrucción o llamados a BD
    private Coroutine damageTextCoroutine; // Referencia a la animación activa del número de daño
    private Vector3 originalDamageTextPos; // Posición local original del texto emergente de daño

    // ==========================================
    // INICIALIZACIÓN Y AUTO-VÍNCULO DE UI
    // ==========================================

    void Start()
    {
        currentHealth = maxHealth;

        // Auto-detectar Slider de salud en los componentes hijos o en la UI global del Jugador
        if (healthSlider == null) healthSlider = GetComponentInChildren<Slider>();
        if (healthSlider == null && (CompareTag("Player") || gameObject.name.Contains("Player")))
        {
            GameObject sliderObj = GameObject.Find("HealthBarSlider");
            if (sliderObj != null) healthSlider = sliderObj.GetComponent<Slider>();
        }

        // Configuración inicial del valor máximo y actual del Slider
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        // Auto-detectar texto numérico de vida para el Jugador
        if (healthText == null && (CompareTag("Player") || gameObject.name.Contains("Player")))
        {
            GameObject textObj = GameObject.Find("HealthTextNumber");
            if (textObj != null) healthText = textObj.GetComponent<TMP_Text>();
        }

        // Auto-detectar componente de texto flotante de daño en la jerarquía
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

        // Guarda la posición base del texto emergente de daño y lo oculta inicialmente
        if (damageText != null)
        {
            originalDamageTextPos = damageText.transform.localPosition;
            damageText.gameObject.SetActive(false);
        }

        // Busca e inicializa el estado visual del escudo protector
        FindShieldReference();
        if (shieldVFX != null) shieldVFX.SetActive(hasShield);

        UpdateUI();
    }

    /// <summary>
    /// Busca el objeto 3D del escudo hexagonal en los hijos si no fue asignado en el Inspector.
    /// </summary>
    void FindShieldReference()
    {
        if (shieldVFX == null)
        {
            Transform foundShield = transform.Find("escudo-hexagonal");
            if (foundShield != null) shieldVFX = foundShield.gameObject;
        }
    }

    // ==========================================
    // GESTIÓN DEL ESCUDO TEMPORAL
    // ==========================================

    /// <summary>
    /// Activa el escudo protector invulnerable durante un tiempo determinado.
    /// </summary>
    /// <param name="duration">Tiempo en segundos de duración de la invulnerabilidad.</param>
    public void ActivateShield(float duration = 10f)
    {
        FindShieldReference();

        if (shieldCoroutine != null)
        {
            StopCoroutine(shieldCoroutine);
        }

        shieldCoroutine = StartCoroutine(ShieldTimerRoutine(duration));
    }

    /// <summary>
    /// Corrutina que mantiene el escudo activo y lo apaga al transcurrir el tiempo especificado.
    /// </summary>
    private IEnumerator ShieldTimerRoutine(float duration)
    {
        hasShield = true;
        if (shieldVFX != null) shieldVFX.SetActive(true);

        yield return new WaitForSeconds(duration);

        hasShield = false;
        if (shieldVFX != null) shieldVFX.SetActive(false);
        shieldCoroutine = null;
    }

    // ==========================================
    // PROCESAMIENTO DE DAÑO Y MUERTE
    // ==========================================

    /// <summary>
    /// Aplica una cantidad de daño al tanque. Si el escudo está activo, bloquea el impacto.
    /// Si la vida llega a cero, activa el flujo de muerte y actualiza las estadísticas/GameManager.
    /// </summary>
    /// <param name="damageAmount">Cantidad de puntos de vida a restar.</param>
    public void TakeDamage(float damageAmount)
    {
        // 1. BLOQUEO DE SEGURIDAD: Ignora llamadas adicionales si el tanque ya fue destruido
        if (isDead) return;

        // Si el escudo protector está activo, absorbe por completo el daño recibido
        if (hasShield)
        {
            Debug.Log("[TankHealth] ¡Daño bloqueado por el escudo activo!");
            return;
        }

        // Resta la salud asegurando que no descienda de cero
        currentHealth = Mathf.Max(currentHealth - damageAmount, 0f);
        UpdateUI();
        ShowDamageNumber(damageAmount);

        // Verificación de condición de muerte
        if (currentHealth <= 0f)
        {
            // 2. CANDADO DE ESTADO: Evita duplicidad de registros en la base de datos o lógica de fin de juego
            isDead = true;

            PlayDeathEffects();

            GameManager gm = FindFirstObjectByType<GameManager>();
            EnemyTankAI aiComponent = GetComponent<EnemyTankAI>();
            TankController playerComponent = GetComponent<TankController>();

            // Si quien muere es un tanque enemigo
            if (aiComponent != null)
            {
                if (gm != null)
                {
                    gm.AddKill(); // Registra la baja en el administrador de partida
                }
            }
            // Si quien muere es el tanque del jugador
            else if (playerComponent != null)
            {
                DatabaseManager.AddPlayerElimination(); // Incrementa contador global de eliminaciones
                DatabaseManager.AddGameLost();           // Registra la derrota en PlayerPrefs

                if (gm != null)
                {
                    gm.TriggerGameOver(); // Dispara la pantalla de Game Over
                }
            }
        }
    }

    // ==========================================
    // ANIMACIÓN DE TEXTO EMERGENTE DE DAÑO
    // ==========================================

    /// <summary>
    /// Inicia la visualización del texto flotante de daño.
    /// </summary>
    private void ShowDamageNumber(float amount)
    {
        if (damageText == null) return;

        if (damageTextCoroutine != null)
        {
            StopCoroutine(damageTextCoroutine);
        }

        damageTextCoroutine = StartCoroutine(ShowDamageRoutine(amount));
    }

    /// <summary>
    /// Corrutina que anima el texto flotante de daño elevándolo y desvaneciéndolo con un color según la gravedad del impacto.
    /// </summary>
    private IEnumerator ShowDamageRoutine(float amount)
    {
        damageText.transform.localPosition = originalDamageTextPos;
        damageText.text = "-" + Mathf.RoundToInt(amount).ToString();

        // Color naranja para golpes fuertes (>20 DMG) o rojo para impactos estándar
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

            // Interpola posición vertical e intensidad del canal Alpha
            damageText.transform.localPosition = Vector3.Lerp(startPos, targetPos, progress);

            Color c = damageText.color;
            c.a = Mathf.Lerp(1f, 0f, progress);
            damageText.color = c;

            yield return null;
        }

        damageText.gameObject.SetActive(false);
        damageText.transform.localPosition = originalDamageTextPos;
    }

    // ==========================================
    // EFECTOS VISUALES Y FÍSICOS DE MUERTE
    // ==========================================

    /// <summary>
    /// Detiene audios, deshabilita componentes de IA/Controlador/Colliders,
    /// crea la explosión de muerte, aplica el color de metal chamuscado e inicia la eyección de la torreta.
    /// </summary>
    void PlayDeathEffects()
    {
        // Detiene sonidos de motor u otros audios activos en el tanque
        AudioSource[] activeAudioSources = GetComponentsInChildren<AudioSource>();
        foreach (AudioSource audio in activeAudioSources) audio.Stop();

        // Instancia el efecto de partículas de explosión en 3D
        if (deathVFXPrefab != null)
        {
            GameObject explosion = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            explosion.transform.localScale = Vector3.one * deathVFXScale;
            Destroy(explosion, 2.5f);
        }

        // Reproduce el efecto de audio de explosión
        if (deathSFX != null)
        {
            AudioSource.PlayClipAtPoint(deathSFX, transform.position, 1.0f);
        }

        // Desactiva los componentes de control e inteligencia artificial
        EnemyTankAI aiScript = GetComponent<EnemyTankAI>();
        if (aiScript != null) aiScript.enabled = false;

        TankController playerScript = GetComponent<TankController>();
        if (playerScript != null) playerScript.enabled = false;

        // Desactiva colliders principales para evitar interferencias
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Oculta la barra de vida sobre el tanque
        Canvas healthCanvas = GetComponentInChildren<Canvas>();
        if (healthCanvas != null) healthCanvas.gameObject.SetActive(false);

        // Cambia el material visual a tono metálico chamuscado/quemado
        ApplyBurntMetalEffect();

        // Inicia el desprendimiento físico de la torreta y posterior encogimiento/destrucción
        StartCoroutine(DetachTurretAndCleanup());
    }

    // ==========================================
    // SISTEMAS DE CURACIÓN
    // ==========================================

    /// <summary>
    /// Restaura un porcentaje de la vida máxima del tanque (ej: 0.20f = 20%).
    /// </summary>
    public void HealPercent(float percentage)
    {
        if (isDead) return;
        float healAmount = maxHealth * percentage;
        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
        UpdateUI();
    }

    /// <summary>
    /// Restaura una cantidad fija de puntos de salud.
    /// </summary>
    public void HealAmount(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateUI();
    }

    /// <summary>
    /// Actualiza los componentes de interfaz (Slider y Texto) con la vida actual.
    /// </summary>
    void UpdateUI()
    {
        if (healthSlider != null) healthSlider.value = currentHealth;
        if (healthText != null) healthText.text = Mathf.RoundToInt(currentHealth).ToString();
    }

    // ==========================================
    // EFECTOS VISUALES DE CHATARRA (BURNT METAL)
    // ==========================================

    /// <summary>
    /// Recorre todos los Renderers del modelo 3D y reemplaza el color base por un tinte oscuro chamuscado.
    /// </summary>
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

    // ==========================================
    // DESPRENDIMIENTO DE TORRETA Y LIMPIEZA
    // ==========================================

    /// <summary>
    /// Separa el Transform de la torreta, le asigna físicas de Rigidbody con impulsos y fuerzas de torque,
    /// aguarda el tiempo de chatarra en escena y encoge suavemente ambos restos antes de destruirlos.
    /// </summary>
    IEnumerator DetachTurretAndCleanup()
    {
        float groundY = transform.position.y;
        Transform turret = transform.Find("Torreta");

        // Búsqueda alternativa por nombre si no se encuentra exactamente como "Torreta"
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

        // Si se localizó la torreta, la desvincula del tanque y le aplica físicas
        if (turret != null)
        {
            turret.parent = null; // Desvincula del objeto padre
            Rigidbody turretRb = turret.gameObject.GetComponent<Rigidbody>();
            if (turretRb == null) turretRb = turret.gameObject.AddComponent<Rigidbody>();

            turretRb.isKinematic = false;
            turretRb.useGravity = true;

            // Aplica fuerza de disparo impulsándola hacia arriba con una leve desviación horizontal aleatoria
            Vector3 forceDirection = Vector3.up * turretLaunchForce + Random.insideUnitSphere * turretHorizontalSpread;
            turretRb.AddForce(forceDirection, ForceMode.Impulse);
            turretRb.AddTorque(Random.insideUnitSphere * 8f, ForceMode.Impulse);

            // Inicia la verificación para estabilizar la torreta al tocar la superficie AR
            StartCoroutine(ClampTurretToGround(turret, turretRb, groundY));
        }

        // Aguarda el tiempo configurado para mostrar los restos chamuscados en el escenario
        yield return new WaitForSeconds(wreckDuration);

        // Animación de encogimiento progresivo (Lerp de 1.0 a 0.0)
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

        // Limpieza final de la escena
        if (turret != null) Destroy(turret.gameObject);
        Destroy(gameObject);
    }

    /// <summary>
    /// Congela el movimiento físico de la torreta desprendida una vez que cae al nivel del suelo virtual del plano AR.
    /// </summary>
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