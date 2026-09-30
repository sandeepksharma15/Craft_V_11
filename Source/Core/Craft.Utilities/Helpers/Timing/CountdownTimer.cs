namespace Craft.Utilities.Helpers;

/// <summary>
/// A pausable countdown that emits numbered ticks and one completion notification.
/// </summary>
/// <remarks>
/// Callbacks run outside the state lock. Stop and disposal cancel pending ticks; an event handler
/// already executing can finish. Call Reset before restarting a completed countdown.
/// </remarks>
public class CountdownTimer : IDisposable
{
    #region Private Fields

    private readonly TimeSpan _interval;
    private readonly Lock _lock = new();
    private readonly TimeProvider _timeProvider;
    private readonly int _totalTicks;
    private int _currentTick;
    private bool _disposed;
    private long _generation;
    private bool _running;
    private ITimer? _timer;

    #endregion Private Fields

    #region Private Methods

    private void HandleTick(long generation)
    {
        int tick;
        bool finished;
        lock (_lock)
        {
            if (_disposed || !_running || generation != _generation)
                return;

            tick = ++_currentTick;
            finished = tick == _totalTicks;
            if (finished)
            {
                _running = false;
                _timer!.Dispose();
                _timer = null;
            }
        }

        try
        {
            OnTick?.Invoke(tick);
        }
        catch (Exception error)
        {
            OnError?.Invoke(error);
        }

        lock (_lock)
        {
            if (_disposed || generation != _generation)
                return;

            if (!finished)
                _timer!.Change(_interval, Timeout.InfiniteTimeSpan);
        }

        if (finished)
        {
            try { OnElapsed?.Invoke(); }
            catch (Exception error) { OnError?.Invoke(error); }
        }
    }

    private void StopCore()
    {
        ++_generation;
        _running = false;
        _timer?.Dispose();
        _timer = null;
    }

    #endregion Private Methods

    #region Protected Methods

    protected virtual void Dispose(bool disposing)
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (disposing)
                StopCore();
        }
    }

    #endregion Protected Methods

    #region Public Constructors

    public CountdownTimer(int timeoutSeconds, int tickCount = 100)
        : this(timeoutSeconds, tickCount, TimeProvider.System) { }

    public CountdownTimer(int timeoutSeconds, int tickCount, TimeProvider timeProvider)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timeoutSeconds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tickCount);
        ArgumentNullException.ThrowIfNull(timeProvider);

        double intervalMs = (double)timeoutSeconds * 1000 / tickCount;

        ArgumentOutOfRangeException.ThrowIfLessThan(intervalMs, 1, nameof(tickCount));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(intervalMs, uint.MaxValue - 1d, nameof(timeoutSeconds));

        _interval = TimeSpan.FromMilliseconds(intervalMs);
        _totalTicks = tickCount;
        _timeProvider = timeProvider;
    }

    #endregion Public Constructors

    #region Public Events

    public event Action? OnElapsed;

    /// <summary>
    /// Reports subscriber failures. Error handlers must not throw.
    /// </summary>
    public event Action<Exception>? OnError;

    public event Action<int>? OnTick;

    #endregion Public Events

    #region Public Methods

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Reset()
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            StopCore();
            _currentTick = 0;
        }
    }

    public void Start()
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_running || _currentTick == _totalTicks)
                return;

            _running = true;
            long generation = ++_generation;
            _timer = _timeProvider.CreateTimer(_ => HandleTick(generation), null,
                _interval, Timeout.InfiniteTimeSpan);
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            StopCore();
        }
    }

    #endregion Public Methods
}
