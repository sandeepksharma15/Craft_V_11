using Craft.Utilities.Helpers.Timing;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Craft.Utilities.Tests.Helpers.Timing;

public class CountdownTimerTests
{
    #region Public Methods

    [Theory]
    [InlineData(0, 100)]
    [InlineData(-1, 100)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 1001)]
    [InlineData(int.MaxValue, 1)]
    public void Constructor_InvalidConfiguration_Throws(int seconds, int ticks)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new CountdownTimer(seconds, ticks));

    [Fact]
    public void Constructor_LargeTimeout_DoesNotOverflow()
    {
        using CountdownTimer timer = new(int.MaxValue, 1000, new FakeTimeProvider());
        timer.Start();
    }

    [Fact]
    public void Constructor_NullTimeProvider_Throws()
        => Assert.Throws<ArgumentNullException>("timeProvider", () => new CountdownTimer(1, 10, null!));

    [Fact]
    public void Countdown_NoSubscribers_Completes()
    {
        FakeTimeProvider time = new();
        using CountdownTimer timer = new(1, 2, time);
        timer.Start();
        time.Advance(TimeSpan.FromMilliseconds(500));
        time.Advance(TimeSpan.FromMilliseconds(500));
        timer.Reset();
        timer.Start();
        time.Advance(TimeSpan.FromMilliseconds(500));
        time.Advance(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void Dispose_IsIdempotentAndPreventsOperationsAndPendingTicks()
    {
        FakeTimeProvider time = new();
        CountdownTimer timer = new(1, 10, time);
        int ticks = 0;
        timer.OnTick += _ => ticks++;
        timer.Start();
        ((IDisposable)timer).Dispose();
        timer.Dispose();
        time.Advance(TimeSpan.FromSeconds(2));
        Assert.Equal(0, ticks);
        Assert.Throws<ObjectDisposedException>(() => timer.Start());
        Assert.Throws<ObjectDisposedException>(() => timer.Stop());
        Assert.Throws<ObjectDisposedException>(() => timer.Reset());
    }

    [Fact]
    public void HandleTick_StaleCallback_DoesNotAdvanceRestartedCountdown()
    {
        List<TimerCallback> callbacks = [];
        Mock<TimeProvider> time = new();
        time.Setup(t => t.CreateTimer(It.IsAny<TimerCallback>(), null, It.IsAny<TimeSpan>(), Timeout.InfiniteTimeSpan))
            .Returns((TimerCallback callback, object? state, TimeSpan due, TimeSpan period) =>
            {
                callbacks.Add(callback);
                return Mock.Of<ITimer>();
            });
        using CountdownTimer timer = new(1, 2, time.Object);
        List<int> ticks = [];
        timer.OnTick += ticks.Add;
        timer.Start();
        timer.Stop();
        callbacks[0](null);
        timer.Start();
        callbacks[0](null);
        Assert.Empty(ticks);
        callbacks[1](null);
        Assert.Equal([1], ticks);
        timer.Dispose();
        callbacks[1](null);
        Assert.Equal([1], ticks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OnTick_ResetOrDispose_DoesNotEmitOldCompletion(bool dispose)
    {
        FakeTimeProvider time = new();
        using CountdownTimer timer = new(1, 1, time);
        int elapsed = 0;
        timer.OnTick += _ => { if (dispose) timer.Dispose(); else timer.Reset(); };
        timer.OnElapsed += () => elapsed++;
        timer.Start();
        time.Advance(TimeSpan.FromSeconds(2));
        Assert.Equal(0, elapsed);
    }

    [Fact]
    public async Task Start_ConcurrentCalls_CreateSingleCountdown()
    {
        FakeTimeProvider time = new();
        using CountdownTimer timer = new(1, 2, time);
        List<int> ticks = [];
        timer.OnTick += ticks.Add;
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(timer.Start)));
        time.Advance(TimeSpan.FromMilliseconds(500));
        time.Advance(TimeSpan.FromMilliseconds(500));
        Assert.Equal([1, 2], ticks);
    }

    [Fact]
    public void Start_RunningOrCompleted_DoesNotAddTicks()
    {
        FakeTimeProvider time = new();
        using CountdownTimer timer = new(1, 4, time);
        List<int> ticks = [];
        int elapsed = 0;
        timer.OnTick += ticks.Add;
        timer.OnElapsed += () => elapsed++;
        timer.Start();
        timer.Start();
        for (int i = 0; i < 4; i++) time.Advance(TimeSpan.FromMilliseconds(250));
        Assert.Equal([1, 2, 3, 4], ticks);
        Assert.Equal(1, elapsed);
        timer.Start();
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal([1, 2, 3, 4], ticks);
        Assert.Equal(1, elapsed);
    }

    [Fact]
    public void StopAndReset_PauseResumeAndRestartCount()
    {
        FakeTimeProvider time = new();
        using CountdownTimer timer = new(1, 4, time);
        List<int> ticks = [];
        timer.OnTick += ticks.Add;
        timer.Stop();
        timer.Start();
        time.Advance(TimeSpan.FromMilliseconds(250));
        timer.Stop();
        timer.Stop();
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal([1], ticks);
        timer.Start();
        time.Advance(TimeSpan.FromMilliseconds(250));
        Assert.Equal([1, 2], ticks);
        timer.Reset();
        time.Advance(TimeSpan.FromSeconds(1));
        timer.Start();
        for (int i = 0; i < 4; i++) time.Advance(TimeSpan.FromMilliseconds(250));
        Assert.Equal([1, 2, 1, 2, 3, 4], ticks);
    }

    [Fact]
    public void Subscribers_Throwing_ReportsBothFailuresAndCompletes()
    {
        FakeTimeProvider time = new();
        using CountdownTimer timer = new(1, 1, time);
        List<Exception> errors = [];
        InvalidOperationException tickError = new("tick");
        InvalidOperationException elapsedError = new("elapsed");
        timer.OnError += errors.Add;
        timer.OnTick += _ => throw tickError;
        timer.OnElapsed += () => throw elapsedError;
        timer.Start();
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal([tickError, elapsedError], errors);
    }

    [Fact]
    public void Subscribers_ThrowingWithoutErrorSubscriber_PreserveCompatibility()
    {
        FakeTimeProvider time = new();
        using CountdownTimer timer = new(1, 1, time);
        timer.OnTick += _ => throw new InvalidOperationException();
        timer.OnElapsed += () => throw new InvalidOperationException();
        timer.Start();
        time.Advance(TimeSpan.FromSeconds(1));
    }

    #endregion Public Methods
}
