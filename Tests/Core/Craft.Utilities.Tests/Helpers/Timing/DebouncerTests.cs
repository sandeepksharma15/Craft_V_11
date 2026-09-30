using Craft.Utilities.Helpers.Timing;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Craft.Utilities.Tests.Helpers.Timing;

public class DebouncerTests
{
    #region Private Methods

    private static void Schedule(Debouncer debouncer, bool throttle, int interval, Func<Task> action)
    {
        if (throttle)
            debouncer.Throttle(interval, action);
        else
            debouncer.Debounce(interval, action);
    }

    #endregion Private Methods

    #region Public Methods

    [Fact]
    public void Constructor_NullTimeProvider_Throws()
        => Assert.Throws<ArgumentNullException>("timeProvider", () => new Debouncer(null!));

    [Fact]
    public async Task Debounce_ConcurrentScheduling_CoalescesPendingWork()
    {
        FakeTimeProvider time = new();
        using Debouncer debouncer = new(time);
        int calls = 0;
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
            debouncer.Debounce(100, () => { Interlocked.Increment(ref calls); return Task.CompletedTask; }))));
        time.Advance(TimeSpan.FromMilliseconds(100));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Debounce_NewCall_RestartsQuietPeriod()
    {
        FakeTimeProvider time = new();
        using Debouncer debouncer = new(time);
        int calls = 0;
        Task action() { calls++; return Task.CompletedTask; }
        debouncer.Debounce(100, action);
        time.Advance(TimeSpan.FromMilliseconds(99));
        debouncer.Debounce(100, action);
        time.Advance(TimeSpan.FromMilliseconds(99));
        Assert.Equal(0, calls);
        time.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Debounce_StaleQueuedCallback_DoesNotDisposeNewTimerOrRunAction()
    {
        List<TimerCallback> callbacks = [];
        Mock<TimeProvider> time = new();
        time.Setup(t => t.CreateTimer(It.IsAny<TimerCallback>(), null, It.IsAny<TimeSpan>(), Timeout.InfiniteTimeSpan))
            .Returns((TimerCallback callback, object? state, TimeSpan due, TimeSpan period) =>
            {
                callbacks.Add(callback);
                return Mock.Of<ITimer>();
            });
        using Debouncer debouncer = new(time.Object);
        int calls = 0;
        debouncer.Debounce(100, () => { calls++; return Task.CompletedTask; });
        debouncer.Debounce(100, () => { calls++; return Task.CompletedTask; });
        callbacks[0](null);
        Assert.Equal(0, calls);
        callbacks[1](null);
        Assert.Equal(1, calls);
        debouncer.Debounce(100, () => { calls++; return Task.CompletedTask; });
        debouncer.Dispose();
        callbacks[2](null);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Dispose_ActionAlreadyStarted_AllowsCompletion()
    {
        FakeTimeProvider time = new();
        Debouncer debouncer = new(time);
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        debouncer.Debounce(100, async () => { await gate.Task; finished.SetResult(); });
        time.Advance(TimeSpan.FromMilliseconds(100));
        debouncer.Dispose();
        gate.SetResult();
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Dispose_PendingAction_CancelsIt(bool throttle)
    {
        FakeTimeProvider time = new();
        Debouncer debouncer = new(time);
        int calls = 0;
        Schedule(debouncer, throttle, 100, () => { calls++; return Task.CompletedTask; });
        debouncer.Dispose();
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Schedule_CancelledOrUnobservedFailure_DoesNotEscapeTimer(bool throttle)
    {
        FakeTimeProvider time = new();
        using Debouncer debouncer = new(time);
        int failures = 0;
        debouncer.OnError += _ => failures++;
        Schedule(debouncer, throttle, 100, () => throw new OperationCanceledException());
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(0, failures);
        using Debouncer unobserved = new(time);
        Schedule(unobserved, throttle, 100, () => throw new InvalidOperationException());
        time.Advance(TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Schedule_FaultedAction_ReportsError(bool throttle)
    {
        FakeTimeProvider time = new();
        using Debouncer debouncer = new(time);
        TaskCompletionSource<Exception> error = new(TaskCreationOptions.RunContinuationsAsynchronously);
        InvalidOperationException expected = new("Action failed");
        debouncer.OnError += exception => error.TrySetResult(exception);
        Schedule(debouncer, throttle, 100, () => Task.FromException(expected));
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Same(expected, await error.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, -1)]
    [InlineData(true, 0)]
    [InlineData(true, -1)]
    public void Schedule_InvalidInterval_Throws(bool throttle, int interval)
    {
        using Debouncer debouncer = new();
        Assert.Throws<ArgumentOutOfRangeException>(nameof(interval),
            () => Schedule(debouncer, throttle, interval, () => Task.CompletedTask));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Schedule_NullActionOrDisposed_Throws(bool throttle)
    {
        using Debouncer debouncer = new();
        Assert.Throws<ArgumentNullException>("action", () => Schedule(debouncer, throttle, 100, null!));
        debouncer.Dispose();
        debouncer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => Schedule(debouncer, throttle, 100, () => Task.CompletedTask));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Schedule_ValidAction_UsesLatestCall(bool throttle)
    {
        FakeTimeProvider time = new();
        using Debouncer debouncer = new(time);
        List<int> calls = [];
        Schedule(debouncer, throttle, 100, () => { calls.Add(1); return Task.CompletedTask; });
        Schedule(debouncer, throttle, 100, () => { calls.Add(2); return Task.CompletedTask; });
        time.Advance(TimeSpan.FromMilliseconds(throttle ? 1 : 100));
        Assert.Equal([2], calls);
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal([2], calls);
    }

    [Fact]
    public void Throttle_RepeatedCalls_DoNotExtendInterval()
    {
        FakeTimeProvider time = new();
        using Debouncer debouncer = new(time);
        List<int> calls = [];
        debouncer.Throttle(100, () => { calls.Add(1); return Task.CompletedTask; });
        time.Advance(TimeSpan.FromMilliseconds(1));
        time.Advance(TimeSpan.FromMilliseconds(30));
        debouncer.Throttle(100, () => { calls.Add(2); return Task.CompletedTask; });
        time.Advance(TimeSpan.FromMilliseconds(30));
        debouncer.Throttle(100, () => { calls.Add(3); return Task.CompletedTask; });
        time.Advance(TimeSpan.FromMilliseconds(39));
        Assert.Equal([1], calls);
        time.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal([1, 3], calls);

        time.Advance(TimeSpan.FromDays(365));
        debouncer.Throttle(100, () => { calls.Add(4); return Task.CompletedTask; });
        time.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal([1, 3, 4], calls);
    }

    #endregion Public Methods
}
