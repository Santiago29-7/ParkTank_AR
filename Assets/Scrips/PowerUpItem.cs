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

    [Header("Efectos Visuales y Sonoros")]
    public GameObject healVFXPrefab; // Asigna un Particle System con destellos verdes
    public AudioClip pickupSFX;      // Sonido al recoger la caja

    void Start()
    {
        // Si no asignaste el renderer manualmente, lo busca en el objeto o sus hijos
        if (boxRenderer == null)
        {
            boxRenderer = GetComponentInChildren<Renderer>();
        }

        UpdateBoxMaterial();
    }

    // Cambia el color/material según la rareza elegida
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

    void Update()
    {
        // Hace que la caja gire continuamente flotando en el aire
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verificar si el objeto que tocó la caja es el tanque (o un objeto hijo del tanque)
        TankHealth tankHealth = other.GetComponentInParent<TankHealth>();

        if (tankHealth != null)
        {
            ApplyPowerUp(tankHealth);

            // Audio de recolección
            if (pickupSFX != null)
            {
                AudioSource.PlayClipAtPoint(pickupSFX, transform.position);
            }

            // VFX de curación
            if (healVFXPrefab != null)
            {
                GameObject vfx = Instantiate(healVFXPrefab, tankHealth.transform.position, Quaternion.identity, tankHealth.transform);
                Destroy(vfx, 2.5f);
            }

            Destroy(gameObject); // Eliminar la caja tras ser recogida
        }
    }

    void ApplyPowerUp(TankHealth tank)
    {
        switch (tier)
        {
            case PowerUpTier.White_Basic:
                TankHealth health = tank.GetComponent<TankHealth>();
                if (health != null)
                {
                    health.HealPercent(0.20f); // Cura el 20%

                    // Mostrar notificación flotante verde
                    if (PopupNotification.Instance != null)
                    {
                        PopupNotification.Instance.ShowMessage("¡+20% SALUD REPARADA!", Color.green);
                    }
                }
                break;

            case PowerUpTier.Green_Uncommon:
                // Reservado para la habilidad verde
                break;

            case PowerUpTier.Blue_Rare:
                // Reservado para la habilidad azul
                break;

            case PowerUpTier.Yellow_Epic:
                // Reservado para la habilidad amarilla
                break;

            case PowerUpTier.Red_Legendary:
                // Reservado para la habilidad roja
                break;
        }
    }
}