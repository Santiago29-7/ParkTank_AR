using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Nombres de las Escenas")]
    public string singleplayerSceneName = "SampleScene";

    [Header("Referencias Persistencia y Nickname")]
    public DatabaseManager dbManager;
    public GameObject nicknamePanel;
    public GameObject mainButtonsPanel;
    public TMP_InputField nicknameInputField;
    public TMP_Text welcomeText;

    [Header("Paneles Adicionales")]
    public GameObject warningsPanel;
    public GameObject creditsPanel;

    [Header("Configuración de Texto de Créditos")]
    public RectTransform creditsTextRect;
    public float scrollSpeed = 60f;
    public float fastForwardMultiplier = 4f; // Multiplicador de velocidad al mantener presionado
    public float startPositionY = -400f;
    public float endPositionY = 500f;

    [Header("Configuración de Pantalla de Agradecimiento")]
    public CanvasGroup thanksCanvasGroup;
    public float fadeDuration = 1.2f;
    public float thanksDisplayTime = 3f;

    private Coroutine creditsCoroutine;

    void Start()
    {
        if (dbManager == null) dbManager = FindFirstObjectByType<DatabaseManager>();

        if (warningsPanel != null) warningsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);

        if (thanksCanvasGroup != null)
        {
            thanksCanvasGroup.alpha = 0f;
            thanksCanvasGroup.gameObject.SetActive(false);
        }

        string savedNickname = (dbManager != null) ? dbManager.GetNickname() : "";

        if (string.IsNullOrEmpty(savedNickname) || savedNickname == "Jugador")
        {
            if (nicknamePanel != null) nicknamePanel.SetActive(true);
            if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        }
        else
        {
            if (nicknamePanel != null) nicknamePanel.SetActive(false);
            if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
            ApplyNickname(savedNickname);
        }
    }

    public void OnConfirmNickname()
    {
        if (nicknameInputField == null) return;

        string enteredName = nicknameInputField.text.Trim();

        if (string.IsNullOrEmpty(enteredName)) return;

        if (dbManager != null)
        {
            dbManager.SaveNickname(enteredName);
        }

        ApplyNickname(enteredName);

        if (nicknamePanel != null) nicknamePanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
    }

    private void ApplyNickname(string name)
    {
        if (welcomeText != null)
        {
            welcomeText.text = "¡BIENVENIDO, " + name.ToUpper() + "!";
        }
    }

    // --- FLUJO DE MENÚ CON ADVERTENCIAS ---
    public void ShowWarningsPanel()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (warningsPanel != null) warningsPanel.SetActive(true);
    }

    public void HideWarningsPanel()
    {
        if (warningsPanel != null) warningsPanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
    }

    public void AcceptWarningsAndPlay()
    {
        Debug.Log("[MainMenu] Advertencias aceptadas. Cargando la escena de juego/QR...");
        SceneManager.LoadScene(singleplayerSceneName);
    }

    // --- FLUJO Y ANIMACIÓN COMPLETA DE CRÉDITOS ---
    public void ShowCreditsPanel()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);

        if (creditsCoroutine != null) StopCoroutine(creditsCoroutine);
        creditsCoroutine = StartCoroutine(FullCreditsSequenceRoutine());
    }

    public void HideCreditsPanel()
    {
        if (creditsCoroutine != null) StopCoroutine(creditsCoroutine);

        if (thanksCanvasGroup != null)
        {
            thanksCanvasGroup.alpha = 0f;
            thanksCanvasGroup.gameObject.SetActive(false);
        }

        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
    }

    private IEnumerator FullCreditsSequenceRoutine()
    {
        // 1. Resetear el logo de agradecimiento
        if (thanksCanvasGroup != null)
        {
            thanksCanvasGroup.alpha = 0f;
            thanksCanvasGroup.gameObject.SetActive(false);
        }

        // 2. Animación de desplazamiento del texto hacia arriba
        if (creditsTextRect != null)
        {
            creditsTextRect.gameObject.SetActive(true);
            Vector2 currentPos = creditsTextRect.anchoredPosition;
            currentPos.y = startPositionY;
            creditsTextRect.anchoredPosition = currentPos;

            while (currentPos.y < endPositionY)
            {
                // Detectar si el usuario mantiene presionada la pantalla
                bool isPressing = Input.GetMouseButton(0) || Input.touchCount > 0;
                float currentSpeed = isPressing ? (scrollSpeed * fastForwardMultiplier) : scrollSpeed;

                currentPos.y += currentSpeed * Time.deltaTime;
                creditsTextRect.anchoredPosition = currentPos;
                yield return null;
            }

            creditsTextRect.gameObject.SetActive(false);
        }

        // 3. Aparecer suavemente (Fade In) la imagen + texto "GRACIAS POR JUGAR"
        if (thanksCanvasGroup != null)
        {
            thanksCanvasGroup.gameObject.SetActive(true);
            float timer = 0f;

            while (timer < fadeDuration)
            {
                bool isPressing = Input.GetMouseButton(0) || Input.touchCount > 0;
                float timeDelta = isPressing ? (Time.deltaTime * fastForwardMultiplier) : Time.deltaTime;

                timer += timeDelta;
                thanksCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            thanksCanvasGroup.alpha = 1f;

            // 4. Mantener visible durante unos segundos (acelerable)
            float displayTimer = 0f;
            while (displayTimer < thanksDisplayTime)
            {
                bool isPressing = Input.GetMouseButton(0) || Input.touchCount > 0;
                float timeDelta = isPressing ? (Time.deltaTime * fastForwardMultiplier) : Time.deltaTime;

                displayTimer += timeDelta;
                yield return null;
            }

            // 5. Desvanecer suavemente (Fade Out)
            timer = 0f;
            while (timer < fadeDuration)
            {
                bool isPressing = Input.GetMouseButton(0) || Input.touchCount > 0;
                float timeDelta = isPressing ? (Time.deltaTime * fastForwardMultiplier) : Time.deltaTime;

                timer += timeDelta;
                thanksCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                yield return null;
            }
            thanksCanvasGroup.alpha = 0f;
        }

        // 6. Regresar al menú principal automáticamente
        HideCreditsPanel();
    }

    // --- SALIR ---
    public void QuitGame()
    {
        Debug.Log("[MainMenu] Cerrando aplicación.");
        Application.Quit();
    }
}