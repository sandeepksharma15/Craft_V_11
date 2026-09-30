namespace Craft.Utilities.Helpers;

/// <summary>Schedules the latest asynchronous action after a quiet period or at a limited rate.</summary>
/// <remarks>Debounce and throttle share one pending action. Rescheduling or disposal cancels pending
/// work, but cannot cancel an action that has already started. Actions run outside the scheduling lock.</remarks>
public class Debouncer : IDisposable
{
    private readonly TimeProvider _timeProvider;
    private readonly Lock _lock = new();
    private ITimer? _timer;
    private long? _lastActionTimestamp;
    private long _generation;
    private bool _disposed;

    public Debouncer() : this(TimeProvider.System) { }

    public Debouncer(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    /// <summary>Reports action failures. Cancellation is treated as a normal completion.</summary>
    /// <remarks>Handlers must not throw.</remarks>
    public event Action<Exception>? OnError;

    /// <summary>Executes the latest action after the interval in milliseconds without a new call.</summary>
    public void Debounce(int interval, Func<Task> action) => Schedule(interval, action, throttle: false);

    /// <summary>Executes the latest action at most once per interval, measured between action starts.</summary>
    /// <remarks>The first action is scheduled immediately; subsequent calls coalesce until the interval ends.</remarks>
    public void Throttle(int interval, Func<Task> action) => Schedule(interval, action, throttle: true);

    private void Schedule(int interval, Func<Task> action, bool throttle)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(interval);
        ArgumentNullException.ThrowIfNull(action);

        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            TimeSpan delay = TimeSpan.FromMilliseconds(interval);
            if (throttle)
            {
                TimeSpan elapsed = _lastActionTimestamp is long timestamp
                    ? _timeProvider.GetElapsedTime(timestamp) : delay;
                delay = TimeSpan.FromMilliseconds(Math.Max(1, (delay - elapsed).TotalMilliseconds));
            }

            _timer?.Dispose();
            long generation = ++_generation;
            _timer = _timeProvider.CreateTimer(state => { _ = ExecuteAsync(action, throttle, generation); },
                null, delay, Timeout.InfiniteTimeSpan);
        }
    }

    private async Task ExecuteAsync(Func<Task> action, bool throttle, long generation)
    {
        lock (_lock)
        {
            if (_disposed || generation != _generation)
                return;

            _timer!.Dispose();
            _timer = null;
            if (throttle)
                _lastActionTimestamp = _timeProvider.GetTimestamp();
        }

        try
        {
            await action().ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            OnError?.Invoke(error);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            _disposed = true;
            ++_generation;
            _timer?.Dispose();
            _timer = null;
        }

        GC.SuppressFinalize(this);
    }
}
