using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementDisplayManager : MonoBehaviour
{
    [Header("Contenedores de Vista Internos")]
    public GameObject mainContent;  // Arrastra el objeto MainContent
    public GameObject detailPanel;  // Arrastra el objeto AchievementDetailPanel

    [Header("Achievement Buttons in ProgressPanel (Order 1-5)")]
    public Button[] achievementButtons;

    [Header("Color Sprites (Unlocked - Order 1-5)")]
    public Sprite[] unlockedSprites;

    [Header("Grayscale Sprites (Locked - Order 1-5)")]
    public Sprite[] lockedSprites;

    [Header("UI Pop-up Detalle Logro")]
    public Image detailIconImage;
    public TextMeshProUGUI detailTitleText;
    public TextMeshProUGUI detailDescriptionText;
    public TextMeshProUGUI detailStatusText;

    private string[] titles = new string[]
    {
        "La primera vez nunca se olvida",
        "¿Alguien trajo escoba y recogedor?",
        "¿Quién dejó todo esto botado?",
        "Pura habilidad... y un poco de suerte",
        "Tutorial de como NO jugar"
    };

    private string[] descriptions = new string[]
    {
        "Juega tu primera partida.",
        "Destruye un total de 15 tanques enemigos.",
        "Recolecta 10 Power-Ups.",
        "Consigue un total de 5 victorias.",
        "Sufre tu primera derrota en combate."
    };

    private float[] thresholds = new float[] { 1f, 15f, 10f, 5f, 1f };

    private void OnEnable()
    {
        UpdateAchievementVisuals();
        SetupButtonListeners();

        // Al abrir la ventana, muestra siempre la lista principal y oculta el detalle
        if (mainContent != null) mainContent.SetActive(true);
        if (detailPanel != null) detailPanel.SetActive(false);
    }

    private void SetupButtonListeners()
    {
        for (int i = 0; i < achievementButtons.Length; i++)
        {
            int index = i;
            if (achievementButtons[i] != null)
            {
                achievementButtons[i].onClick.RemoveAllListeners();
                achievementButtons[i].onClick.AddListener(() => OpenAchievementDetail(index));
            }
        }
    }

    public void UpdateAchievementVisuals()
    {
        for (int i = 0; i < achievementButtons.Length; i++)
        {
            if (i >= thresholds.Length || i >= unlockedSprites.Length || i >= lockedSprites.Length) break;

            float currentValue = GetStatisticValue(i);
            bool isUnlocked = currentValue >= thresholds[i];

            Image btnImage = achievementButtons[i].GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.color = Color.white;
                btnImage.sprite = isUnlocked ? unlockedSprites[i] : lockedSprites[i];
            }
        }
    }

    public void OpenAchievementDetail(int index)
    {
        if (index < 0 || index >= titles.Length) return;

        float currentValue = GetStatisticValue(index);
        bool isUnlocked = currentValue >= thresholds[index];

        if (detailIconImage != null)
        {
            detailIconImage.sprite = isUnlocked ? unlockedSprites[index] : lockedSprites[index];
        }

        if (detailTitleText != null) detailTitleText.text = titles[index];
        if (detailDescriptionText != null) detailDescriptionText.text = descriptions[index];

        if (detailStatusText != null)
        {
            detailStatusText.text = isUnlocked ? "¡DESBLOQUEADO!" : "BLOQUEADO";
            detailStatusText.color = isUnlocked ? Color.green : Color.red;
        }

        // Oculta únicamente la lista/estadísticas y muestra el detalle
        if (mainContent != null) mainContent.SetActive(false);
        if (detailPanel != null) detailPanel.SetActive(true);
    }

    public void CloseAchievementDetail()
    {
        // Oculta el detalle y restaura la vista principal
        if (detailPanel != null) detailPanel.SetActive(false);
        if (mainContent != null) mainContent.SetActive(true);
    }

    private float GetStatisticValue(int statisticIndex)
    {
        switch (statisticIndex)
        {
            case 0: return DatabaseManager.GetGamesPlayed();
            case 1: return DatabaseManager.GetTanksDestroyed();
            case 2: return DatabaseManager.GetPowerUpsCollected();
            case 3: return DatabaseManager.GetGamesWon();
            case 4: return DatabaseManager.GetGamesLost();
            default: return 0f;
        }
    }
}