using System.Collections;
using UnityEngine;

/// <summary>
/// Define los niveles de rareza de las cajas de suministros y potenciadores (Power-Ups).
/// </summary>
public enum PowerUpTier
{
    White_Basic,      // Básico (Reparación de salud)
    Green_Uncommon,   // Poco Común (Escudo temporal)
    Blue_Rare,        // Raro
    Yellow_Epic,      // Épico (Proyectil táctico Nuke x2 daño)
    Red_Legendary     // Legendario
}

/// <summary>
/// Controla la lógica individual de cada caja de Power-Up en la escena AR.
/// Gestiona la rotación continua, la asignación dinámica de materiales según su rareza,
/// la detección de colisiones exclusiva con el jugador, la aplicación de efectos (Salud, Escudo, Nuke)
/// y la animación de desaparición con retroalimentación en UI y audio.
/// </summary>
public class PowerUpItem : MonoBehaviour
{
    // ==========================================
    // CONFIGURACIÓN DE RAREZA Y ROTACIÓN
    // ==========================================

    [Header("Configuración del Power-Up")]
    [Tooltip("Nivel de rareza del potenciador que determina su efecto y color")]
    public PowerUpTier tier = PowerUpTier.White_Basic;

    [Tooltip("Velocidad de rotación continua sobre el eje vertical en grados por segundo")]
    public float rotationSpeed = 60f;

    // ==========================================
    // MATERIALES Y RENDERIZADO VISUAL
    // ==========================================

    [Header("Referencia al Renderer")]
    [Tooltip("Renderer del modelo 3D de la caja para actualizar el material visual")]
    public Renderer boxRenderer;

    [Header("Materiales por Rareza")]
    [Tooltip("Material de color blanco para potenciadores básicos")]
    public Material whiteMat;

    [Tooltip("Material de color verde para potenciadores poco comunes")]
    public Material greenMat;

    [Tooltip("Material de color azul para potenciadores raros")]
    public Material blueMat;

    [Tooltip("Material de color amarillo para potenciadores épicos")]
    public Material yellowMat;

    [Tooltip("Material de color rojo para potenciadores legendarios")]
    public Material redMat;

    // ==========================================
    // EFECTOS VISUALES Y SONOROS
    // ==========================================

    [Header("Efectos")]
    [Tooltip("Prefab del efecto de partículas (VFX) de curación")]
    public GameObject healVFXPrefab;

    [Tooltip("Prefab del efecto de partículas (VFX) de modo Nuke (humo/aura amarilla)")]
    public GameObject nukeVFXPrefab;

    [Tooltip("Efecto de audio (SFX) reproducido al recolectar la caja")]
    public AudioClip pickupSFX;

    // ==========================================
    // ESTADO INTERNO
    // ==========================================

    private bool isCollected = false; // Bandera para evitar doble recolección o interacciones simultáneas

    // ==========================================
    // INICIALIZACIÓN Y BUCLE PRINCIPAL
    // ==========================================

    void Awake()
    {
        // Auto-detectar el componente Renderer en los hijos si no fue asignado manualmente
        if (boxRenderer == null) boxRenderer = GetComponentInChildren<Renderer>();
    }

    void Start()
    {
        // Aplica el material correspondiente al nivel de rareza al instanciarse
        UpdateBoxMaterial();
    }

    void Update()
    {
        // Si aún no ha sido recolectado, hace girar la caja sobre su propio eje Y en espacio de mundo
        if (!isCollected)
        {
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    // ==========================================
    // ASIGNACIÓN DINÁMICA DE MATERIALES
    // ==========================================

    /// <summary>
    /// Asigna la textura/material adecuado al Renderer de la caja según el valor de 'tier'.
    /// </summary>
    public void UpdateBoxMaterial()
    {
        if (boxRenderer == null) boxRenderer = GetComponentInChildren<Renderer>();
        if (boxRenderer == null) return;

        switch (tier)
        {
            case PowerUpTier.White_Basic:
                if (whiteMat != null) boxRenderer.material = whiteMat;
                break;
            case PowerUpTier.Green_Uncommon:
                if (greenMat != null) boxRenderer.material = greenMat;
                break;
            case PowerUpTier.Blue_Rare:
                if (blueMat != null) boxRenderer.material = blueMat;
                break;
            case PowerUpTier.Yellow_Epic:
                if (yellowMat != null) boxRenderer.material = yellowMat;
                break;
            case PowerUpTier.Red_Legendary:
                if (redMat != null) boxRenderer.material = redMat;
                break;
        }
    }

    // ==========================================
    // DETECCIÓN DE COLISIONES Y RECOGIDA
    // ==========================================

    /// <summary>
    /// Detecta el contacto del tanque del jugador con el área del Trigger de la caja.
    /// Valida que el receptor no sea un enemigo y aplica el beneficio correspondiente.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Si ya fue recolectada por otra entidad en este mismo frame, ignora
        if (isCollected) return;

        // FILTRO DE SEGURIDAD 1: Evita que los tanques enemigos recojan las cajas del escenario
        if (other.CompareTag("Enemy")) return;

        // Intenta obtener la salud del tanque en el objeto o sus padres
        TankHealth tankHealth = other.GetComponentInParent<TankHealth>();

        if (tankHealth != null)
        {
            // FILTRO DE SEGURIDAD 2: Verificación estricta para garantizar que la salud pertenezca al jugador
            if (tankHealth.CompareTag("Enemy")) return;

            isCollected = true;

            // Deshabilita el Collider inmediatamente para evitar múltiples activaciones
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Si quien recoge es el tanque del jugador, incrementa la estadística en la base de datos persistente
            TankController isPlayer = tankHealth.GetComponent<TankController>();
            if (isPlayer != null || tankHealth.CompareTag("Player"))
            {
                DatabaseManager.AddPowerUpCollected(); // Incrementa contador de Power-Ups para logros
            }

            // Aplica el efecto específico según la rareza
            ApplyPowerUp(tankHealth);

            // Reproduce el efecto de sonido de recogida en la posición 3D actual
            if (pickupSFX != null)
            {
                AudioSource.PlayClipAtPoint(pickupSFX, transform.position);
            }

            // Inicia la animación de encogimiento y elevación antes de destruir el objeto
            StartCoroutine(DisappearAnimationRoutine());
        }
    }

    // ==========================================
    // APLICACIÓN DE HABILIDADES Y POTENCIADORES
    // ==========================================

    /// <summary>
    /// Ejecuta la lógica del beneficio correspondiente al nivel de rareza del Power-Up.
    /// </summary>
    /// <param name="tankHealth">Referencia al componente TankHealth del jugador beneficiado.</param>
    void ApplyPowerUp(TankHealth tankHealth)
    {
        switch (tier)
        {
            case PowerUpTier.White_Basic:
                // Si el tanque ya está al 100% de vida, lo notifica sin desperdiciar curación
                if (tankHealth.currentHealth >= tankHealth.maxHealth)
                {
                    if (PopupNotification.Instance != null) PopupNotification.Instance.ShowMessage("¡Salud Completa!", Color.green);
                }
                else
                {
                    // Repara el 20% de la salud máxima
                    tankHealth.HealPercent(0.20f);

                    // Instancia el efecto visual de curación como hijo del tanque para que lo siga en movimiento
                    if (healVFXPrefab != null)
                    {
                        GameObject vfx = Instantiate(healVFXPrefab, tankHealth.transform.position, Quaternion.identity, tankHealth.transform);
                        Destroy(vfx, 2.5f);
                    }
                    if (PopupNotification.Instance != null) PopupNotification.Instance.ShowMessage("¡+20% SALUD REPARADA!", Color.green);
                }
                break;

            case PowerUpTier.Green_Uncommon:
                // Activa un escudo protector invulnerable por 10 segundos
                tankHealth.ActivateShield(10f);
                if (PopupNotification.Instance != null) PopupNotification.Instance.ShowMessage("¡ESCUDO TEMPORAL ACTIVADO POR 10 SEGUNDOS!", Color.green);
                break;

            case PowerUpTier.Yellow_Epic:
                // Activa el modo Nuke (daño x2 en el siguiente disparo) enviando el mensaje al tanque
                tankHealth.gameObject.SendMessage("ActivateNukeMode", SendMessageOptions.DontRequireReceiver);

                // Instancia el aura/efecto de humo amarillo asignado al tanque
                if (nukeVFXPrefab != null)
                {
                    GameObject vfx = Instantiate(nukeVFXPrefab, tankHealth.transform.position, Quaternion.identity, tankHealth.transform);
                    Destroy(vfx, 2.5f);
                }

                if (PopupNotification.Instance != null)
                {
                    PopupNotification.Instance.ShowMessage("¡PROYECTIL TÁCTICO CARGADO, HACES X2 DE DAÑO EN EL SIGUIENTE DISPARO!", Color.yellow);
                }
                break;
        }
    }

    // ==========================================
    // ANIMACIÓN DE DESAPARICIÓN
    // ==========================================

    /// <summary>
    /// Corrutina que anima el Power-Up al ser recolectado:
    /// Se eleva ligeramente, rota a alta velocidad y se reduce de tamaño de 1.0 a 0.0 antes de destruirse.
    /// </summary>
    private IEnumerator DisappearAnimationRoutine()
    {
        Vector3 initialScale = transform.localScale;
        Vector3 initialPosition = transform.position;
        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // Reduce progresivamente la escala linealmente a cero
            float scaleMultiplier = Mathf.Lerp(1f, 0f, progress);
            transform.localScale = initialScale * scaleMultiplier;

            // Eleva suavemente la posición Y durante la animación
            transform.position = initialPosition + new Vector3(0f, progress * 0.25f, 0f);

            // Giro rápido de aceleración (720 grados/seg)
            transform.Rotate(0f, 720f * Time.deltaTime, 0f);

            yield return null;
        }

        // Elimina definitivamente la entidad de la escena
        Destroy(gameObject);
    }
}