using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    [Header("Ссылки")]
    // 1. Добавляем поле для ссылки на дашборд
    [SerializeField] private BoatDashboard dashboard;

    [Header("Панели")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Текстовые поля итогов")]
    [SerializeField] private TextMeshProUGUI victoryTimeText;


    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        ShowMainMenu();
    }

    // --- УПРАВЛЕНИЕ СОСТОЯНИЯМИ ---

    public void ShowMainMenu()
    {
        HideAllPanels();
        mainMenuPanel.SetActive(true);
        // Time.timeScale = 0f; // Если хочешь, чтобы вода двигалась в меню - оставь закомментированным
    }

    public void ShowVictory(float finalTime)
    {
        HideAllPanels();

        // Останавливаем таймер на лодке, чтобы он не ушел в минус
        if (dashboard != null) dashboard.StopTimer();

        victoryPanel.SetActive(true);
        // Time.timeScale = 0f; // Пауза физики (по желанию)

        System.TimeSpan t = System.TimeSpan.FromSeconds(finalTime);
        victoryTimeText.text = string.Format("TIME: {0:D2}:{1:D2}", t.Minutes, t.Seconds);
    }

    public void ShowGameOver()
    {
        HideAllPanels();
        if (dashboard != null) dashboard.StopTimer();
        gameOverPanel.SetActive(true);
        // Time.timeScale = 0f;
    }

    // --- КНОПКИ ---
    public void OnStartGameButton()
    {
        HideAllPanels();
        // Time.timeScale = 1f;

        // 2. Запускаем таймер на лодке
        if (dashboard != null)
        {
            dashboard.StartTimer();
        }
    }

    public void OnRestartButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }

    private void HideAllPanels()
    {
        mainMenuPanel.SetActive(false);
        victoryPanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }

    private void Update()
    {
    }
}