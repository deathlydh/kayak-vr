using UnityEngine;
using TMPro;

public class BoatDashboard : MonoBehaviour
{
    [Header("Настройки Гонки")]
    [Tooltip("Время на прохождение трассы в секундах")]
    [SerializeField] private float timeLimitInSeconds = 120f;

    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI checkpointText;
    [SerializeField] private TextMeshProUGUI speedometerText;
    [SerializeField] private Rigidbody kayakRB;

    [Header("Settings")]
    [SerializeField] private float speedUpdateInterval = 1.0f;

    private float currentSpeedTimer;
    private float _currentTime;
    private bool _isTimerRunning = false;

    private void UpdateSpeedMeter()
    {

        if (kayakRB == null || speedometerText == null) return;
        currentSpeedTimer += Time.deltaTime;

        if (currentSpeedTimer >= speedUpdateInterval)
        {
            currentSpeedTimer = 0f;
            
            float sqrSpeed = kayakRB.linearVelocity.sqrMagnitude;

            if (sqrSpeed < 0.1f)
            {
                speedometerText.text = "0 km/h";
                return;
            }
            //перевод м/с в км/ч 
            float realSpeed = Mathf.Sqrt(sqrSpeed) * 3.6f;
            speedometerText.text = $"{realSpeed:F0} km/h";
        }
        
    }

    private void UpdateTimer(float time)
    {
        //Форматирование в 00:00:000
        System.TimeSpan t = System.TimeSpan.FromSeconds(time);
        timerText.text = string.Format("{0:D2}:{1:D2}:{2:D3}", t.Minutes, t.Seconds, t.Milliseconds);
    }

    private void UpdateCheckpoints(int current, int total)
    {
        checkpointText.text = $"{current} / {total}";
    }

    // 2. Публичный метод для запуска таймера (вызывается из MenuManager)
    public void StartTimer()
    {
        _currentTime = timeLimitInSeconds; // Сброс времени на начало
        _isTimerRunning = true;
    }

    // Метод для остановки (например при победе)
    public void StopTimer()
    {
        _isTimerRunning = false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _currentTime = timeLimitInSeconds;
        UpdateTimer(_currentTime);
        UpdateCheckpoints(0, 10);
    }

    // Update is called once per frame
    void Update()
    {
        // Спидометр работает всегда (для атмосферы)
        UpdateSpeedMeter();

        // 3. Таймер тикает ТОЛЬКО если флаг true
        if (_isTimerRunning)
        {
            _currentTime -= Time.deltaTime;

            if (_currentTime <= 0)
            {
                _currentTime = 0;
                _isTimerRunning = false; // Останавливаем счетчик
                MenuManager.Instance.ShowGameOver(); // Зовем Game Over напрямую
            }

            UpdateTimer(_currentTime);
        }
    }
}
