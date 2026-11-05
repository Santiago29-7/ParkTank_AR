using UnityEngine;
using TMPro;

public class ProgressManager : MonoBehaviour
{
    [Header("Paneles UI")]
    public GameObject mainButtonsPanel;
    public GameObject progressPanel;

    [Header("Referencia al Database Manager")]
    public DatabaseManager databaseManager;

    [Header("Textos de Estadísticas (Valores)")]
    public TextMeshProUGUI txtNickname;
    public TextMeshProUGUI txtTanksDestroyed;
    public TextMeshProUGUI txtPowerUps;
    public TextMeshProUGUI txtGamesPlayed;
    public TextMeshProUGUI txtGamesWon;
    public TextMeshProUGUI txtGamesLost;
    public TextMeshProUGUI txtPlayerEliminations;

    // --- ABRIR Y CERRAR EL PANEL ---
    public void OpenProgressPanel()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (progressPanel != null) progressPanel.SetActive(true);

        LoadAndDisplayProgress();
    }

    public void CloseProgressPanel()
    {
        if (progressPanel != null) progressPanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
    }

    // --- CARGAR DATOS DESDE DATABASE MANAGER ---
    private void LoadAndDisplayProgress()
    {
        // Buscar DatabaseManager automáticamente en la escena si no se arrastró al Inspector
        if (databaseManager == null)
        {
            databaseManager = FindObjectOfType<DatabaseManager>();
        }

        if (databaseManager != null)
        {
            if (txtNickname != null) txtNickname.text = databaseManager.GetNickname();
            if (txtTanksDestroyed != null) txtTanksDestroyed.text = databaseManager.GetStat("Stat_TanksDestroyed");
            if (txtPowerUps != null) txtPowerUps.text = databaseManager.GetStat("Stat_PowerUps");
            if (txtGamesPlayed != null) txtGamesPlayed.text = databaseManager.GetStat("Stat_GamesPlayed");
            if (txtGamesWon != null) txtGamesWon.text = databaseManager.GetStat("Stat_GamesWon");
            if (txtGamesLost != null) txtGamesLost.text = databaseManager.GetStat("Stat_GamesLost");
            if (txtPlayerEliminations != null) txtPlayerEliminations.text = databaseManager.GetStat("Stat_PlayerEliminations");
        }
        else
        {
            Debug.LogWarning("ProgressManager: No se encontró el objeto DatabaseManager en la escena.");
        }
    }
}