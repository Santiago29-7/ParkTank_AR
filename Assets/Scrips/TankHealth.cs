using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TankHealth : MonoBehaviour
{
    [Header("Configuración de Salud")]
    public float maxHealth = 100f;
    public float currentHealth;

    private Slider healthSlider;
    private TMP_Text healthText;

    void Start()
    {
        currentHealth = maxHealth; // Inicia siempre con 100 de vida (o la maxHealth configurada)

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

        UpdateUI();
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
        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        UpdateUI();
    }

    void UpdateUI()
    {
        // Actualizar el llenado de la barra verde
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        // Actualizar el número al lado de la barra
        if (healthText != null)
        {
            healthText.text = Mathf.RoundToInt(currentHealth).ToString();
        }
    }
}