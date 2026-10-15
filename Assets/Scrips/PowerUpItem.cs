using System.Collections;
using UnityEngine;

public enum PowerUpTier
{
    White_Basic,
    Green_Uncommon,
    Blue_Rare,
    Yellow_Epic,
    Red_Legendary
}

public class PowerUpItem : MonoBehaviour
{
    [Header("Configuración del Power-Up")]
    public PowerUpTier tier = PowerUpTier.White_Basic;
    public float rotationSpeed = 60f;

    [Header("Referencia al Renderer")]
    public Renderer boxRenderer;

    [Header("Materiales por Rareza")]
    public Material whiteMat;
    public Material greenMat;
    public Material blueMat;
    public Material yellowMat;
    public Material redMat;

    [Header("Efecto de Humo en el Tanque")]
    public GameObject healVFXPrefab;   // Arrastra aquí el Prefab VFX_Heal_Smoke

    [Header("Sonido (Opcional)")]
    public AudioClip pickupSFX;

    private bool isCollected = false;

    void Start()
    {
        if (boxRenderer == null)
        {
            boxRenderer = GetComponentInChildren<Renderer>();
        }

        UpdateBoxMaterial();
    }

    void Update()
    {
        // Rotación continua mientras flota
        if (!isCollected)
        {
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    public void UpdateBoxMaterial()
    {
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

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        TankHealth tankHealth = other.GetComponentInParent<TankHealth>();

        if (tankHealth != null)
        {
            isCollected = true;

            // 1. Desactivar el colisionador inmediatamente
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // 2. Aplicar curación/salud
            ApplyPowerUp(tankHealth);

            // 3. Reproducir audio si está asignado
            if (pickupSFX != null)
            {
                AudioSource.PlayClipAtPoint(pickupSFX, transform.position);
            }

            // 4. Crear el humo verde ÚNICAMENTE sobre el tanque
            if (healVFXPrefab != null)
            {
                GameObject vfx = Instantiate(healVFXPrefab, tankHealth.transform.position, Quaternion.identity, tankHealth.transform);
                Destroy(vfx, 2.5f); // Se destruye automáticamente a los 2.5s
            }

            // 5. Animación de desvanecimiento de la caja
            StartCoroutine(DisappearAnimationRoutine());
        }
    }

    void ApplyPowerUp(TankHealth tankHealth)
    {
        switch (tier)
        {
            case PowerUpTier.White_Basic:
                tankHealth.HealPercent(0.20f); // Cura el 20%

                if (PopupNotification.Instance != null)
                {
                    PopupNotification.Instance.ShowMessage("¡+20% SALUD REPARADA!", Color.green);
                }
                break;

            case PowerUpTier.Green_Uncommon:
                break;

            case PowerUpTier.Blue_Rare:
                break;

            case PowerUpTier.Yellow_Epic:
                break;

            case PowerUpTier.Red_Legendary:
                break;
        }
    }

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

            // Reducir suavemente la escala a cero
            float scaleMultiplier = Mathf.Lerp(1f, 0f, progress * progress);
            transform.localScale = initialScale * scaleMultiplier;

            // Elevar ligeramente la caja mientras se encoge
            transform.position = initialPosition + new Vector3(0f, progress * 0.25f, 0f);

            // Giro rápido sobre su propio eje
            transform.Rotate(0f, 720f * Time.deltaTime, 0f);

            yield return null;
        }

        Destroy(gameObject);
    }
}