using Fakturus.Track.WebApp.Models;

namespace Fakturus.Track.WebApp.Services;

public enum TimerState
{
    Idle,
    Running,
    Paused,
    Stopped
}

public class TimerService : IDisposable
{
    private readonly ApiClient _apiClient;
    private System.Timers.Timer? _timer;
    private WorkSession? _currentSession;

    public TimerState State { get; private set; } = TimerState.Idle;
    public WorkSession? CurrentSession => _currentSession;
    public TimeSpan Elapsed { get; private set; }
    public TimeSpan PauseDuration { get; private set; }
    public DateTime? PauseStartedAt { get; private set; }

    public event Action? OnStateChanged;

    public TimerService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _currentSession = await _apiClient.GetTodaySessionAsync();
            if (_currentSession != null && !_currentSession.IsFinished)
            {
                if (_currentSession.StopTime.HasValue)
                {
                    State = TimerState.Paused;
                    PauseStartedAt = _currentSession.StopTime.Value;
                    Elapsed = _currentSession.StopTime.Value - _currentSession.StartTime;
                }
                else
                {
                    State = TimerState.Running;
                    StartTicking();
                }
            }
        }
        catch
        {
            // API not available, stay idle
        }
    }

    public async Task StartAsync()
    {
        _currentSession = await _apiClient.StartSessionAsync();
        State = TimerState.Running;
        PauseDuration = TimeSpan.Zero;
        StartTicking();
        OnStateChanged?.Invoke();
    }

    public async Task PauseAsync()
    {
        if (_currentSession == null) return;
        _currentSession = await _apiClient.StopSessionAsync(_currentSession.Id);
        State = TimerState.Paused;
        PauseStartedAt = DateTime.UtcNow;
        StopTicking();
        OnStateChanged?.Invoke();
    }

    public async Task ResumeAsync()
    {
        if (_currentSession == null) return;
        if (PauseStartedAt.HasValue)
        {
            PauseDuration += DateTime.UtcNow - PauseStartedAt.Value;
            PauseStartedAt = null;
        }
        _currentSession = await _apiClient.ResumeSessionAsync(_currentSession.Id);
        State = TimerState.Running;
        StartTicking();
        OnStateChanged?.Invoke();
    }

    public async Task StopAsync()
    {
        if (_currentSession == null) return;
        _currentSession = await _apiClient.StopSessionAsync(_currentSession.Id);
        State = TimerState.Stopped;
        StopTicking();
        OnStateChanged?.Invoke();
    }

    public async Task FinishAsync()
    {
        if (_currentSession == null) return;
        _currentSession = await _apiClient.FinishSessionAsync(_currentSession.Id);
        State = TimerState.Idle;
        Elapsed = TimeSpan.Zero;
        PauseDuration = TimeSpan.Zero;
        StopTicking();
        OnStateChanged?.Invoke();
    }

    private void StartTicking()
    {
        _timer?.Dispose();
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (_, _) =>
        {
            if (_currentSession != null)
            {
                Elapsed = DateTime.UtcNow - _currentSession.StartTime - PauseDuration;
            }
            OnStateChanged?.Invoke();
        };
        _timer.Start();
    }

    private void StopTicking()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
