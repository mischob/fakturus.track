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
    private WorkSession? _activeSession;
    private int _accumulatedPauseMinutes;
    private DateTime? _pauseStartUtc;

    public TimerState State { get; private set; } = TimerState.Idle;
    public WorkSession? ActiveSession => _activeSession;
    public TimeSpan Elapsed { get; private set; }
    public int PauseMinutes => _accumulatedPauseMinutes +
        (_pauseStartUtc.HasValue ? (int)(DateTime.UtcNow - _pauseStartUtc.Value).TotalMinutes : 0);

    public event Action? OnStateChanged;

    public TimerService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// Load active (unfinished) session from backend
    public async Task InitializeAsync()
    {
        try
        {
            var sessions = await _apiClient.GetWorkSessionsAsync();
            _activeSession = sessions.FirstOrDefault(s => !s.IsFinished);

            if (_activeSession != null)
            {
                _accumulatedPauseMinutes = _activeSession.PauseMinutes;
                if (_activeSession.StopTime.HasValue && !_activeSession.IsFinished)
                {
                    State = TimerState.Stopped;
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

    /// Start a new session — persists immediately to backend (circuit-disconnect safe)
    public async Task StartAsync()
    {
        var now = DateTime.UtcNow;
        var request = new CreateWorkSessionRequest
        {
            Id = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(now),
            StartTime = now,
            StopTime = null,
            PauseMinutes = 0
        };

        _activeSession = await _apiClient.CreateWorkSessionAsync(request);
        _accumulatedPauseMinutes = 0;
        _pauseStartUtc = null;
        State = TimerState.Running;
        StartTicking();
        OnStateChanged?.Invoke();
    }

    /// Stop the timer (set stop time, but don't finish yet)
    public async Task StopAsync()
    {
        if (_activeSession == null) return;

        if (State == TimerState.Paused && _pauseStartUtc.HasValue)
        {
            _accumulatedPauseMinutes += (int)Math.Ceiling((DateTime.UtcNow - _pauseStartUtc.Value).TotalMinutes);
            _pauseStartUtc = null;
        }

        var request = new UpdateWorkSessionRequest
        {
            Date = _activeSession.Date,
            StartTime = _activeSession.StartTime,
            StopTime = DateTime.UtcNow,
            PauseMinutes = _accumulatedPauseMinutes
        };

        await _apiClient.UpdateWorkSessionAsync(_activeSession.Id, request);
        _activeSession.StopTime = request.StopTime;
        State = TimerState.Stopped;
        StopTicking();
        OnStateChanged?.Invoke();
    }

    /// Finish and save the session
    public async Task FinishAsync()
    {
        if (_activeSession == null) return;

        if (State == TimerState.Paused && _pauseStartUtc.HasValue)
        {
            _accumulatedPauseMinutes += (int)Math.Ceiling((DateTime.UtcNow - _pauseStartUtc.Value).TotalMinutes);
            _pauseStartUtc = null;
        }

        var stopTime = _activeSession.StopTime ?? DateTime.UtcNow;
        var request = new UpdateWorkSessionRequest
        {
            Date = _activeSession.Date,
            StartTime = _activeSession.StartTime,
            StopTime = stopTime,
            PauseMinutes = _accumulatedPauseMinutes,
            IsFinished = true
        };

        await _apiClient.UpdateWorkSessionAsync(_activeSession.Id, request);
        _activeSession = null;
        State = TimerState.Idle;
        Elapsed = TimeSpan.Zero;
        _accumulatedPauseMinutes = 0;
        StopTicking();
        OnStateChanged?.Invoke();
    }

    /// Pause the timer
    public void Pause()
    {
        if (State != TimerState.Running) return;
        _pauseStartUtc = DateTime.UtcNow;
        State = TimerState.Paused;
        StopTicking();
        OnStateChanged?.Invoke();
    }

    /// Resume from pause
    public void Resume()
    {
        if (State != TimerState.Paused || _pauseStartUtc == null) return;
        _accumulatedPauseMinutes += (int)Math.Ceiling((DateTime.UtcNow - _pauseStartUtc.Value).TotalMinutes);
        _pauseStartUtc = null;
        State = TimerState.Running;
        StartTicking();
        OnStateChanged?.Invoke();
    }

    private void StartTicking()
    {
        _timer?.Dispose();
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (_, _) =>
        {
            if (_activeSession != null)
            {
                var totalPause = TimeSpan.FromMinutes(_accumulatedPauseMinutes);
                Elapsed = DateTime.UtcNow - _activeSession.StartTime - totalPause;
                if (Elapsed < TimeSpan.Zero) Elapsed = TimeSpan.Zero;
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
