using UnityEngine;
using TMPro;

/// <summary>
/// Gestiona la visualización del panel de progreso y estadísticas del jugador en la interfaz del Menú Principal.
/// Recupera los datos almacenados en PlayerPrefs a través de DatabaseManager (apodo, tanques destruidos, 
/// potenciadores recolectados, partidas jugadas, victorias, derrotas y eliminaciones) y los despliega en componentes TextMeshPro.
/// </summary>
public class ProgressManager : MonoBehaviour
{
    // ==========================================
    // REFERENCIAS DE PANELES UI
    // ==========================================

    [Header("Paneles UI")]
    [Tooltip("Contenedor de los botones principales del menú para ocultar al abrir las estadísticas")]
    public GameObject mainButtonsPanel;

    [Tooltip("Panel modal de progreso y estadísticas del jugador")]
    public GameObject progressPanel;

    // ==========================================
    // REFERENCIAS A BASE DE DATOS Y ELEMENTOS DE TEXTO
    // ==========================================

    [Header("Referencia al Database Manager")]
    [Tooltip("Referencia al gestor de persistencia de datos (PlayerPrefs)")]
    public DatabaseManager databaseManager;

    [Header("Textos de Estadísticas (Valores)")]
    [Tooltip("Texto donde se despliega el apodo o nickname del jugador")]
    public TextMeshProUGUI txtNickname;

    [Tooltip("Texto donde se despliega la cantidad total de tanques enemigos destruidos")]
    public TextMeshProUGUI txtTanksDestroyed;

    [Tooltip("Texto donde se despliega la cantidad total de cajas de Power-Ups recolectadas")]
    public TextMeshProUGUI txtPowerUps;

    [Tooltip("Texto donde se despliega el número total de partidas jugadas")]
    public TextMeshProUGUI txtGamesPlayed;

    [Tooltip("Texto donde se despliega la cantidad total de victorias obtenidas")]
    public TextMeshProUGUI txtGamesWon;

    [Tooltip("Texto donde se despliega la cantidad total de derrotas acumuladas")]
    public TextMeshProUGUI txtGamesLost;

    [Tooltip("Texto donde se despliega el contador total de eliminaciones")]
    public TextMeshProUGUI txtPlayerEliminations;

    // ==========================================
    // CONTROL DE VISIBILIDAD DE PANELES
    // ==========================================

    /// <summary>
    /// Despliega el panel de estadísticas, oculta los botones del menú principal y fuerza la lectura de los datos guardados.
    /// </summary>
    public void OpenProgressPanel()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (progressPanel != null) progressPanel.SetActive(true);

        // Carga y actualiza los valores de texto en pantalla desde la base de datos
        LoadAndDisplayProgress();
    }

    /// <summary>
    /// Cierra el panel de estadísticas y restablece la visibilidad de los botones del menú principal.
    /// </summary>
    public void CloseProgressPanel()
    {
        if (progressPanel != null) progressPanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
    }

    // ==========================================
    // LECTURA Y DESPLIEGUE DE ESTADÍSTICAS
    // ==========================================

    /// <summary>
    /// Consulta los datos persistentes almacenados en DatabaseManager y asigna los valores a los componentes de texto.
    /// Si la referencia a DatabaseManager es nula, intenta buscarla automáticamente en la escena activa.
    /// </summary>
    private void LoadAndDisplayProgress()
    {
        // Buscar DatabaseManager automáticamente en la escena si no se asignó manualmente en el Inspector
        if (databaseManager == null)
        {
            databaseManager = FindFirstObjectByType<DatabaseManager>();
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