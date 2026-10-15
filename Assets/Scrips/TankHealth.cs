using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TankHealth : MonoBehaviour
{
    [Header("Configuración de Salud")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Configuración de Blindaje")]
    public bool hasShield = false;
    public GameObject shieldVFX; // Objeto 'escudo-hexagonal'

    private Slider healthSlider;
    private TMP_Text healthText;

    void Start()
    {
        currentHealth = 50f; // 50% de la vida total

        GameObject sliderObj = GameObject.Find("HealthBarSlider");
        if (sliderObj != null)
        {
            healthSlider = sliderObj.GetComponent<Slider>();
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        GameObject textObj = GameObject.Find("HealthTextNumber");
        if (textObj != null)
        {
            healthText = textObj.GetComponent<TMP_Text>();
        }

        // Búsqueda automática por código si no fue asignado en el Inspector
        FindShieldReference();

        // El efecto del escudo inicia desactivado
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
            // Busca en los objetos hijos la malla del escudo
            Transform foundShield = transform.Find("escudo-hexagonal");
            if (foundShield != null)
            {
                shieldVFX = foundShield.gameObject;
            }
        }
    }

    // Activa la Placa de Blindaje (Caja Verde)
    public void ActivateShield()
    {
        FindShieldReference(); // Asegurar referencia antes de encender

        hasShield = true;

        if (shieldVFX != null)
        {
            shieldVFX.SetActive(true); // Encender el objeto visual del escudo
            Debug.Log("¡Blindaje activado! Escudo encendido visualmente.");
        }
        else
        {
            Debug.LogWarning("TankHealth: No se encontró el objeto 'escudo-hexagonal' en los hijos del tanque.");
        }
    }

    public void HealPercent(float percentage)
    {
        float healAmount = maxHealth * percentage;
        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
        UpdateUI();
        Debug.Log($"¡Tanque curado! Salud actual: {currentHealth}/{maxHealth}");
    }

    public void TakeDamage(float amount)
    {
        // 1. Si el escudo está activo, absorbe todo el daño del impacto
        if (hasShield)
        {
            hasShield = false;
            if (shieldVFX != null)
            {
                shieldVFX.SetActive(false); // Apagar escudo tras recibir impacto
            }
            Debug.Log("¡El blindaje absorbió el impacto! Daño evitado.");
            return;
        }

        // 2. Si no hay escudo, descuenta salud normalmente
        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        UpdateUI();
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
}