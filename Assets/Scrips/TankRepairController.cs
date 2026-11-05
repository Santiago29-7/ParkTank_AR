using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TankRepairController : MonoBehaviour
{
    [Header("Configuración de Reparación")]
    public float repairCycleTime = 2.5f;   // Tiempo en segundos para completar 1 ciclo
    public float healAmountPerCycle = 10f; // Vida otorgada al completar el ciclo

    [Header("Referencias UI")]
    public GameObject repairUIPanel;     // Objeto contenedor RepairUIPanel
    public Image repairProgressImage;    // Imagen Radial (RepairProgressImage)
    public RectTransform wrenchIcon;     // Icono de la herramienta (WrenchIcon)

    [Header("Referencias del Tanque")]
    public TankHealth tankHealth;
    public TankController tankController;

    private bool isHoldingRepair = false;
    private Coroutine repairCoroutine;

    void Start()
    {
        if (repairUIPanel != null)
        {
            repairUIPanel.SetActive(false);
        }
    }

    private void FindLocalTank()
    {
        if (tankController == null || tankHealth == null)
        {
            TankController tank = FindFirstObjectByType<TankController>();
            if (tank != null)
            {
                tankController = tank;
                tankHealth = tank.GetComponent<TankHealth>();
            }
        }
    }

    public void OnPointerDownRepair()
    {
        FindLocalTank();

        if (tankHealth == null || tankHealth.currentHealth >= tankHealth.maxHealth) return;

        isHoldingRepair = true;

        if (repairCoroutine != null)
            StopCoroutine(repairCoroutine);

        repairCoroutine = StartCoroutine(RepairRoutine());
    }

    public void OnPointerUpRepair()
    {
        isHoldingRepair = false;

        if (repairCoroutine != null)
        {
            StopCoroutine(repairCoroutine);
            repairCoroutine = null;
        }

        ResetRepairState();
    }

    private IEnumerator RepairRoutine()
    {
        if (tankController != null)
        {
            tankController.SetCanMove(false);
        }

        if (repairUIPanel != null)
        {
            repairUIPanel.SetActive(true);
        }

        while (isHoldingRepair)
        {
            if (tankHealth != null && tankHealth.currentHealth >= tankHealth.maxHealth)
            {
                break;
            }

            float timer = 0f;
            if (repairProgressImage != null) repairProgressImage.fillAmount = 0f;

            while (timer < repairCycleTime && isHoldingRepair)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / repairCycleTime);

                if (repairProgressImage != null)
                {
                    repairProgressImage.fillAmount = progress;
                }

                // Giro de 360° en sentido horario sincronizado exactamente con la carga
                if (wrenchIcon != null)
                {
                    wrenchIcon.localRotation = Quaternion.Euler(0f, 0f, -progress * 360f);
                }

                yield return null;
            }

            if (timer >= repairCycleTime && isHoldingRepair)
            {
                if (tankHealth != null)
                {
                    tankHealth.HealAmount(healAmountPerCycle);
                }

                if (repairProgressImage != null)
                {
                    repairProgressImage.fillAmount = 0f;
                }

                if (wrenchIcon != null)
                {
                    wrenchIcon.localRotation = Quaternion.identity;
                }
            }
        }

        ResetRepairState();
    }

    private void ResetRepairState()
    {
        if (tankController != null)
        {
            tankController.SetCanMove(true);
        }

        if (repairUIPanel != null)
        {
            repairUIPanel.SetActive(false);
        }

        if (repairProgressImage != null)
        {
            repairProgressImage.fillAmount = 0f;
        }

        if (wrenchIcon != null)
        {
            wrenchIcon.localRotation = Quaternion.identity;
        }
    }
}