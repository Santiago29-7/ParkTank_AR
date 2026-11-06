using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementNotificationManager : MonoBehaviour
{
    public static AchievementNotificationManager Instance;

    [Header("UI Toast Components")]
    public GameObject toastPanel;
    public CanvasGroup toastCanvasGroup;
    public Image toastIcon;
    public TextMeshProUGUI toastTitleText;

    [Header("Color Sprites (Order 1-5)")]
    public Sprite[] unlockedSprites;

    [Header("Ajustes de Tiempo y Animación")]
    [Tooltip("Tiempo de espera solo para el logro de inicio de partida")]
    public float startDelay = 1.5f;
    [Tooltip("Distancia en Y desde la cual caerá la notificación")]
    public float slideOffsetY = 150f;
    public float animDuration = 0.4f;
    public float displayTime = 3.0f;

    private string[] titles = new string[]
    {
        "La primera vez nunca se olvida",
        "¿Alguien trajo escoba y recogedor?",
        "¿Quién dejó todo esto botado?",
        "Pura habilidad... y un poco de suerte",
        "Tutorial de como NO jugar"
    };

    private float[] thresholds = new float[] { 1f, 1f, 1f, 1f, 1f };

    private Coroutine activeToastCoroutine;
    private RectTransform toastRectTransform;
    private Vector2 targetAnchoredPosition;
    private Vector2 startAnchoredPosition;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (toastPanel != null)
        {
            toastRectTransform = toastPanel.GetComponent<RectTransform>();
            if (toastRectTransform != null)
            {
                targetAnchoredPosition = toastRectTransform.anchoredPosition;
                startAnchoredPosition = targetAnchoredPosition + new Vector2(0, slideOffsetY);
            }
        }
    }

    private void Start()
    {
        if (toastPanel != null) toastPanel.SetActive(false);
        if (toastCanvasGroup != null) toastCanvasGroup.alpha = 0f;

        // Verificar si hay logros acumulados pendientes de notificar al iniciar la escena
        CheckAchievements();
    }

    public void CheckAchievements()
    {
        for (int i = 0; i < thresholds.Length; i++)
        {
            float currentValue = GetStatisticValue(i);
            bool reachedThreshold = currentValue >= thresholds[i];
            bool alreadyNotified = PlayerPrefs.GetInt("Notified_Achievement_" + i, 0) == 1;

            if (reachedThreshold && !alreadyNotified)
            {
                PlayerPrefs.SetInt("Notified_Achievement_" + i, 1);
                PlayerPrefs.Save();

                TriggerToast(i);
                break;
            }
        }
    }

    private void TriggerToast(int achievementIndex)
    {
        if (activeToastCoroutine != null) StopCoroutine(activeToastCoroutine);
        activeToastCoroutine = StartCoroutine(ShowToastCoroutine(achievementIndex));
    }

    private IEnumerator ShowToastCoroutine(int index)
    {
        // El retraso solo aplica al logro de iniciar partida (índice 0)
        if (index == 0 && startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        if (toastIcon != null && index < unlockedSprites.Length)
        {
            toastIcon.sprite = unlockedSprites[index];
        }

        if (toastTitleText != null && index < titles.Length)
        {
            toastTitleText.text = titles[index];
        }

        toastPanel.SetActive(true);

        if (toastRectTransform != null) toastRectTransform.anchoredPosition = startAnchoredPosition;

        // 1. Entrar (Deslizar hacia abajo + Fade In)
        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            toastCanvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothT);
            if (toastRectTransform != null)
            {
                toastRectTransform.anchoredPosition = Vector2.Lerp(startAnchoredPosition, targetAnchoredPosition, smoothT);
            }
            yield return null;
        }

        toastCanvasGroup.alpha = 1f;
        if (toastRectTransform != null) toastRectTransform.anchoredPosition = targetAnchoredPosition;

        // 2. Permanecer visible
        yield return new WaitForSeconds(displayTime);

        // 3. Salir (Deslizar hacia arriba + Fade Out)
        elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            toastCanvasGroup.alpha = Mathf.Lerp(1f, 0f, smoothT);
            if (toastRectTransform != null)
            {
                toastRectTransform.anchoredPosition = Vector2.Lerp(targetAnchoredPosition, startAnchoredPosition, smoothT);
            }
            yield return null;
        }

        toastCanvasGroup.alpha = 0f;
        if (toastRectTransform != null) toastRectTransform.anchoredPosition = startAnchoredPosition;

        toastPanel.SetActive(false);
    }

    private float GetStatisticValue(int index)
    {
        switch (index)
        {
            case 0: return DatabaseManager.GetGamesPlayed();
            case 1: return DatabaseManager.GetTanksDestroyed();
            case 2: return DatabaseManager.GetPowerUpsCollected();
            case 3: return DatabaseManager.GetGamesWon();
            case 4: return DatabaseManager.GetGamesLost();
            default: return 0f;
        }
    }

    [ContextMenu("Resetear Notificaciones de Logros")]
    public void ResetNotifications()
    {
        for (int i = 0; i < 5; i++)
        {
            PlayerPrefs.DeleteKey("Notified_Achievement_" + i);
        }
        PlayerPrefs.Save();
        Debug.Log("Banderas de notificación reseteadas.");
    }
}