using Craft.Utilities.Helpers;

namespace Craft.Utilities.Tests.Helpers;

public class RetryHelperTests
{

    [Theory]
    [InlineData("value")]
    [InlineData("void")]
    [InlineData("typed")]
    [InlineData("backoff")]
    public void SynchronousRetry_Cancellation_IsNeverRetried(string kind)
    {
        int calls = 0;
        OperationCanceledException expected = new();
        int Action() { calls++; throw expected; }
        Action invoke = kind switch
        {
            "value" => () => RetryHelper.Retry(Action, delayMs: 0),
            "void" => () => RetryHelper.Retry(() => { Action(); }, delayMs: 0),
            "typed" => () => RetryHelper.RetryOnException<int, Exception>(Action, delayMs: 0),
            _ => () => RetryHelper.RetryWithExponentialBackoff(Action, initialDelayMs: 0)
        };
        Assert.Same(expected, Assert.Throws<OperationCanceledException>(invoke));
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("value")]
    [InlineData("void")]
    [InlineData("typed")]
    [InlineData("backoff")]
    public async Task AsynchronousRetry_Cancellation_IsNeverRetriedOrWrapped(string kind)
    {
        int calls = 0;
        OperationCanceledException expected = new();
        Task<int> Action() { calls++; throw expected; }
        Func<Task> invoke = kind switch
        {
            "value" => () => RetryHelper.RetryAsync(Action, delayMs: 0, cancellationToken: TestContext.Current.CancellationToken),
            "void" => () => RetryHelper.RetryAsync(async () => { await Action(); }, delayMs: 0, cancellationToken: TestContext.Current.CancellationToken),
            "typed" => () => RetryHelper.RetryOnExceptionAsync<int, Exception>(Action, delayMs: 0, cancellationToken: TestContext.Current.CancellationToken),
            _ => () => RetryHelper.RetryWithExponentialBackoffAsync(Action, initialDelayMs: 0, cancellationToken: TestContext.Current.CancellationToken)
        };
        Assert.Same(expected, await Assert.ThrowsAsync<OperationCanceledException>(invoke));
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("value")]
    [InlineData("void")]
    [InlineData("typed")]
    [InlineData("backoff")]
    public async Task TokenAwareRetry_PassesTokenToEachAttempt(string kind)
    {
        using CancellationTokenSource source = new();
        int calls = 0;
        Task<int> Action(CancellationToken token)
        {
            Assert.Equal(source.Token, token);
            calls++;
            return calls == 1 ? Task.FromException<int>(new IOException()) : Task.FromResult(42);
        }
        Task<int> valueTask = kind switch
        {
            "value" => RetryHelper.RetryAsync(Action, delayMs: 0, cancellationToken: source.Token),
            "typed" => RetryHelper.RetryOnExceptionAsync<int, IOException>(Action, delayMs: 0, cancellationToken: source.Token),
            "backoff" => RetryHelper.RetryWithExponentialBackoffAsync(Action, initialDelayMs: 0, cancellationToken: source.Token),
            _ => Task.FromResult(42)
        };
        Assert.Equal(42, await valueTask);
        if (kind == "void")
            await RetryHelper.RetryAsync(async token => { await Action(token); }, delayMs: 0, cancellationToken: source.Token);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task TokenAwareRetry_CancellationDuringAction_StopsAttempts()
    {
        using CancellationTokenSource source = new();
        int calls = 0;
        await Assert.ThrowsAsync<OperationCanceledException>(() => RetryHelper.RetryAsync<int>(token =>
        {
            calls++;
            source.Cancel();
            token.ThrowIfCancellationRequested();
            return Task.FromResult(1);
        }, delayMs: 0, cancellationToken: source.Token));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task TokenAwareRetry_NullDelegates_Throw()
    {
        await Assert.ThrowsAsync<ArgumentNullException>("action", () => RetryHelper.RetryAsync<int>((Func<CancellationToken, Task<int>>)null!, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>("action", () => RetryHelper.RetryAsync((Func<CancellationToken, Task>)null!, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>("action", () => RetryHelper.RetryOnExceptionAsync<int, IOException>((Func<CancellationToken, Task<int>>)null!, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>("action", () => RetryHelper.RetryWithExponentialBackoffAsync<int>((Func<CancellationToken, Task<int>>)null!, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    [InlineData(1, -1)]
    public async Task Retry_InvalidArguments_AllStrategiesValidate(int attempts, int delay)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryHelper.Retry(() => 1, attempts, delay));
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryHelper.Retry(() => { }, attempts, delay));
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryHelper.RetryOnException<int, IOException>(() => 1, attempts, delay));
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryHelper.RetryWithExponentialBackoff(() => 1, attempts, delay));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => RetryHelper.RetryAsync(() => Task.FromResult(1), attempts, delay, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => RetryHelper.RetryAsync(() => Task.CompletedTask, attempts, delay, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => RetryHelper.RetryOnExceptionAsync<int, IOException>(() => Task.FromResult(1), attempts, delay, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => RetryHelper.RetryWithExponentialBackoffAsync(() => Task.FromResult(1), attempts, delay, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(40)]
    public async Task Backoff_ZeroDelayAndManyAttempts_PreservesLastFailure(int maxAttempts)
    {
        int calls = 0;
        IOException expected = new("last");
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            RetryHelper.RetryWithExponentialBackoff<int>(() => { calls++; throw expected; }, maxAttempts, 0, 0));
        Assert.Same(expected, error.InnerException);
        Assert.Equal(maxAttempts, calls);
        calls = 0;
        error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RetryHelper.RetryWithExponentialBackoffAsync<int>(() => { calls++; return Task.FromException<int>(expected); },
                maxAttempts, 0, 0, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Same(expected, error.InnerException);
        Assert.Equal(maxAttempts, calls);
    }

    [Fact]
    public async Task RetryAsync_VoidNullAndTypedExhaustion_PreserveFailures()
    {
        await Assert.ThrowsAsync<ArgumentNullException>("action", () => RetryHelper.RetryAsync((Func<Task>)null!, cancellationToken: TestContext.Current.CancellationToken));
        IOException expected = new();
        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RetryHelper.RetryOnExceptionAsync<int, IOException>(() => Task.FromException<int>(expected), 1, 0, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Same(expected, error.InnerException);
    }

    #region Retry<T> Tests

    [Fact]
    public void Retry_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var expectedValue = 42;

        // Act
        var result = RetryHelper.Retry(() => expectedValue);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void Retry_FailsOnceSucceedsSecond_ReturnsResult()
    {
        // Arrange
        var attemptCount = 0;
        var expectedValue = 42;

        // Act
        var result = RetryHelper.Retry(() =>
        {
            attemptCount++;
            if (attemptCount < 2)
                throw new InvalidOperationException("First attempt fails");
            return expectedValue;
        }, maxAttempts: 3, delayMs: 10);

        // Assert
        Assert.Equal(expectedValue, result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public void Retry_AllAttemptsFail_ThrowsInvalidOperationException()
    {
        // Arrange
        var attemptCount = 0;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            RetryHelper.Retry<int>(() =>
            {
                attemptCount++;
                throw new InvalidOperationException("Operation failed");
            }, maxAttempts: 3, delayMs: 10));

        Assert.Equal(3, attemptCount);
        Assert.Contains("Operation failed after 3 attempts", exception.Message);
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public void Retry_NullAction_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            RetryHelper.Retry<int>(null!, maxAttempts: 3, delayMs: 1000));
    }

    [Fact]
    public void Retry_InvalidMaxAttempts_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RetryHelper.Retry(() => 42, maxAttempts: 0, delayMs: 1000));
    }

    [Fact]
    public void Retry_NegativeDelay_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RetryHelper.Retry(() => 42, maxAttempts: 3, delayMs: -1));
    }

    #endregion

    #region Retry (void) Tests

    [Fact]
    public void Retry_Void_SuccessfulOperation_Executes()
    {
        // Arrange
        var executed = false;

        // Act
        RetryHelper.Retry(() => executed = true);

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public void Retry_Void_FailsOnceSucceedsSecond_Executes()
    {
        // Arrange
        var attemptCount = 0;

        // Act
        RetryHelper.Retry(() =>
        {
            attemptCount++;
            if (attemptCount < 2)
                throw new InvalidOperationException("First attempt fails");
        }, maxAttempts: 3, delayMs: 10);

        // Assert
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public void Retry_Void_AllAttemptsFail_ThrowsInvalidOperationException()
    {
        // Arrange
        var attemptCount = 0;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            RetryHelper.Retry(() =>
            {
                attemptCount++;
                throw new InvalidOperationException("Operation failed");
            }, maxAttempts: 3, delayMs: 10));

        Assert.Equal(3, attemptCount);
        Assert.Contains("Operation failed after 3 attempts", exception.Message);
    }

    [Fact]
    public void Retry_Void_NullAction_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            RetryHelper.Retry(null!, maxAttempts: 3, delayMs: 1000));
    }

    #endregion

    #region RetryAsync<T> Tests

    [Fact]
    public async Task RetryAsync_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var expectedValue = 42;

        // Act
        var result = await RetryHelper.RetryAsync(() => Task.FromResult(expectedValue), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task RetryAsync_FailsOnceSucceedsSecond_ReturnsResult()
    {
        // Arrange
        var attemptCount = 0;
        var expectedValue = 42;

        // Act
        var result = await RetryHelper.RetryAsync(() =>
        {
            attemptCount++;
            if (attemptCount < 2)
                throw new InvalidOperationException("First attempt fails");
            return Task.FromResult(expectedValue);
        }, maxAttempts: 3, delayMs: 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedValue, result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task RetryAsync_AllAttemptsFail_ThrowsInvalidOperationException()
    {
        // Arrange
        var attemptCount = 0;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await RetryHelper.RetryAsync<int>(() =>
            {
                attemptCount++;
                throw new InvalidOperationException("Operation failed");
            }, maxAttempts: 3, delayMs: 10, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(3, attemptCount);
        Assert.Contains("Operation failed after 3 attempts", exception.Message);
    }

    [Fact]
    public async Task RetryAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await RetryHelper.RetryAsync(() => Task.FromResult(42), cancellationToken: cts.Token));
    }

    [Fact]
    public async Task RetryAsync_CancellationRequestedDuringRetry_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await RetryHelper.RetryAsync(() =>
            {
                attemptCount++;
                if (attemptCount == 2)
                    cts.Cancel();
                throw new InvalidOperationException("Operation failed");
            }, maxAttempts: 5, delayMs: 10, cancellationToken: cts.Token));

        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task RetryAsync_NullAction_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await RetryHelper.RetryAsync<int>((Func<Task<int>>)null!, maxAttempts: 3, delayMs: 1000, cancellationToken: TestContext.Current.CancellationToken));
    }

    #endregion

    #region RetryAsync (void) Tests

    [Fact]
    public async Task RetryAsync_Void_SuccessfulOperation_Executes()
    {
        // Arrange
        var executed = false;

        // Act
        await RetryHelper.RetryAsync(() =>
        {
            executed = true;
            return Task.CompletedTask;
        }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task RetryAsync_Void_FailsOnceSucceedsSecond_Executes()
    {
        // Arrange
        var attemptCount = 0;

        // Act
        await RetryHelper.RetryAsync(() =>
        {
            attemptCount++;
            if (attemptCount < 2)
                throw new InvalidOperationException("First attempt fails");
            return Task.CompletedTask;
        }, maxAttempts: 3, delayMs: 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task RetryAsync_Void_AllAttemptsFail_ThrowsInvalidOperationException()
    {
        // Arrange
        var attemptCount = 0;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await RetryHelper.RetryAsync(() =>
            {
                attemptCount++;
                throw new InvalidOperationException("Operation failed");
            }, maxAttempts: 3, delayMs: 10, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(3, attemptCount);
        Assert.Contains("Operation failed after 3 attempts", exception.Message);
    }

    [Fact]
    public async Task RetryAsync_Void_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await RetryHelper.RetryAsync(() => Task.CompletedTask, cancellationToken: cts.Token));
    }

    #endregion

    #region RetryOnException<T, TException> Tests

    [Fact]
    public void RetryOnException_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var expectedValue = 42;

        // Act
        var result = RetryHelper.RetryOnException<int, InvalidOperationException>(() => expectedValue);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void RetryOnException_ThrowsSpecificException_Retries()
    {
        // Arrange
        var attemptCount = 0;
        var expectedValue = 42;

        // Act
        var result = RetryHelper.RetryOnException<int, InvalidOperationException>(() =>
        {
            attemptCount++;
            if (attemptCount < 2)
                throw new InvalidOperationException("First attempt fails");
            return expectedValue;
        }, maxAttempts: 3, delayMs: 10);

        // Assert
        Assert.Equal(expectedValue, result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public void RetryOnException_ThrowsDifferentException_ThrowsImmediately()
    {
        // Arrange
        var attemptCount = 0;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            RetryHelper.RetryOnException<int, InvalidOperationException>(() =>
            {
                attemptCount++;
                throw new ArgumentException("Different exception");
            }, maxAttempts: 3, delayMs: 10));

        Assert.Equal(1, attemptCount);
    }

    [Fact]
    public void RetryOnException_AllAttemptsFailWithSpecificException_ThrowsInvalidOperationException()
    {
        // Arrange
        var attemptCount = 0;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            RetryHelper.RetryOnException<int, ArgumentException>(() =>
            {
                attemptCount++;
                throw new ArgumentException("Operation failed");
            }, maxAttempts: 3, delayMs: 10));

        Assert.Equal(3, attemptCount);
        Assert.Contains("Operation failed after 3 attempts", exception.Message);
    }

    [Fact]
    public void RetryOnException_NullAction_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            RetryHelper.RetryOnException<int, InvalidOperationException>(null!, maxAttempts: 3, delayMs: 1000));
    }

    #endregion

    #region RetryOnExceptionAsync<T, TException> Tests

    [Fact]
    public async Task RetryOnExceptionAsync_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var expectedValue = 42;

        // Act
        var result = await RetryHelper.RetryOnExceptionAsync<int, InvalidOperationException>(
            () => Task.FromResult(expectedValue), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task RetryOnExceptionAsync_ThrowsSpecificException_Retries()
    {
        // Arrange
        var attemptCount = 0;
        var expectedValue = 42;

        // Act
        var result = await RetryHelper.RetryOnExceptionAsync<int, InvalidOperationException>(() =>
        {
            attemptCount++;
            if (attemptCount < 2)
                throw new InvalidOperationException("First attempt fails");
            return Task.FromResult(expectedValue);
        }, maxAttempts: 3, delayMs: 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedValue, result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task RetryOnExceptionAsync_ThrowsDifferentException_ThrowsImmediately()
    {
        // Arrange
        var attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await RetryHelper.RetryOnExceptionAsync<int, InvalidOperationException>(() =>
            {
                attemptCount++;
                throw new ArgumentException("Different exception");
            }, maxAttempts: 3, delayMs: 10, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(1, attemptCount);
    }

    [Fact]
    public async Task RetryOnExceptionAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await RetryHelper.RetryOnExceptionAsync<int, InvalidOperationException>(
                () => Task.FromResult(42),
                cancellationToken: cts.Token));
    }

    [Fact]
    public async Task RetryOnExceptionAsync_NullAction_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await RetryHelper.RetryOnExceptionAsync<int, InvalidOperationException>(
                (Func<Task<int>>)null!, maxAttempts: 3, delayMs: 1000, cancellationToken: TestContext.Current.CancellationToken));
    }

    #endregion

    #region RetryWithExponentialBackoff<T> Tests

    [Fact]
    public void RetryWithExponentialBackoff_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var expectedValue = 42;

        // Act
        var result = RetryHelper.RetryWithExponentialBackoff(() => expectedValue);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void RetryWithExponentialBackoff_FailsOnceSucceedsSecond_ReturnsResult()
    {
        // Arrange
        var attemptCount = 0;
        var expectedValue = 42;

        // Act
        var result = RetryHelper.RetryWithExponentialBackoff(() =>
        {
            attemptCount++;
            if (attemptCount < 2)
                throw new InvalidOperationException("First attempt fails");
            return expectedValue;
        }, maxAttempts: 3, initialDelayMs: 10);

        // Assert
        Assert.Equal(expectedValue, result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public void RetryWithExponentialBackoff_AllAttemptsFail_ThrowsInvalidOperationException()
    {
        // Arrange
        var attemptCount = 0;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            RetryHelper.RetryWithExponentialBackoff<int>(() =>
            {
                attemptCount++;
                throw new InvalidOperationException("Operation failed");
            }, maxAttempts: 3, initialDelayMs: 10));

        Assert.Equal(3, attemptCount);
        Assert.Contains("Operation failed after 3 attempts", exception.Message);
    }

    [Fact]
    public void RetryWithExponentialBackoff_DelaysIncrease_ExponentiallyGrows()
    {
        // Arrange
        var attemptCount = 0;
        var delays = new List<long>();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var lastTime = sw.ElapsedMilliseconds;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            RetryHelper.RetryWithExponentialBackoff<int>(() =>
            {
                attemptCount++;
                if (attemptCount > 1)
                {
                    var currentTime = sw.ElapsedMilliseconds;
                    delays.Add(currentTime - lastTime);
                    lastTime = currentTime;
                }
                throw new InvalidOperationException("Operation failed");
            }, maxAttempts: 4, initialDelayMs: 100, maxDelayMs: 10000));

        // Assert delays are increasing (allowing for some timing variance)
        Assert.Equal(4, attemptCount);
        Assert.Equal(3, delays.Count);
        Assert.True(delays[1] > delays[0], $"Second delay {delays[1]} should be greater than first {delays[0]}");
        Assert.True(delays[2] > delays[1], $"Third delay {delays[2]} should be greater than second {delays[1]}");
    }

    [Fact]
    public void RetryWithExponentialBackoff_MaxDelay_CapsDelay()
    {
        // Arrange
        var attemptCount = 0;
        var delays = new List<long>();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var lastTime = sw.ElapsedMilliseconds;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            RetryHelper.RetryWithExponentialBackoff<int>(() =>
            {
                attemptCount++;
                if (attemptCount > 1)
                {
                    var currentTime = sw.ElapsedMilliseconds;
                    delays.Add(currentTime - lastTime);
                    lastTime = currentTime;
                }
                throw new InvalidOperationException("Operation failed");
            }, maxAttempts: 10, initialDelayMs: 100, maxDelayMs: 200));

        // Assert all delays respect the max delay cap (with tolerance for timing variance)
        Assert.Equal(10, attemptCount);
        Assert.All(delays, delay => Assert.True(delay <= 300, $"Delay {delay} exceeded max with tolerance"));
    }

    [Fact]
    public void RetryWithExponentialBackoff_InvalidMaxDelayLessThanInitial_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RetryHelper.RetryWithExponentialBackoff(() => 42,
                maxAttempts: 3,
                initialDelayMs: 1000,
                maxDelayMs: 500));
    }

    [Fact]
    public void RetryWithExponentialBackoff_NullAction_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            RetryHelper.RetryWithExponentialBackoff<int>(null!, maxAttempts: 3, initialDelayMs: 1000));
    }

    #endregion

    #region RetryWithExponentialBackoffAsync<T> Tests

    [Fact]
    public async Task RetryWithExponentialBackoffAsync_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var expectedValue = 42;

        // Act
        var result = await RetryHelper.RetryWithExponentialBackoffAsync(() => Task.FromResult(expectedValue), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task RetryWithExponentialBackoffAsync_FailsOnceSucceedsSecond_ReturnsResult()
    {
        // Arrange
        var attemptCount = 0;
        var expectedValue = 42;

        // Act
        var result = await RetryHelper.RetryWithExponentialBackoffAsync(() =>
        {
            attemptCount++;
            if (attemptCount < 2)
                throw new InvalidOperationException("First attempt fails");
            return Task.FromResult(expectedValue);
        }, maxAttempts: 3, initialDelayMs: 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedValue, result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task RetryWithExponentialBackoffAsync_AllAttemptsFail_ThrowsInvalidOperationException()
    {
        // Arrange
        var attemptCount = 0;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await RetryHelper.RetryWithExponentialBackoffAsync<int>(() =>
            {
                attemptCount++;
                throw new InvalidOperationException("Operation failed");
            }, maxAttempts: 3, initialDelayMs: 10, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(3, attemptCount);
        Assert.Contains("Operation failed after 3 attempts", exception.Message);
    }

    [Fact]
    public async Task RetryWithExponentialBackoffAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await RetryHelper.RetryWithExponentialBackoffAsync(
                () => Task.FromResult(42),
                cancellationToken: cts.Token));
    }

    [Fact]
    public async Task RetryWithExponentialBackoffAsync_DelaysIncrease_ExponentiallyGrows()
    {
        // Arrange
        var attemptCount = 0;
        var delays = new List<long>();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var lastTime = sw.ElapsedMilliseconds;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await RetryHelper.RetryWithExponentialBackoffAsync<int>(() =>
            {
                attemptCount++;
                if (attemptCount > 1)
                {
                    var currentTime = sw.ElapsedMilliseconds;
                    delays.Add(currentTime - lastTime);
                    lastTime = currentTime;
                }
                throw new InvalidOperationException("Operation failed");
            }, maxAttempts: 4, initialDelayMs: 100, maxDelayMs: 10000, cancellationToken: TestContext.Current.CancellationToken));

        // Assert delays are increasing (allowing for some timing variance)
        Assert.Equal(4, attemptCount);
        Assert.Equal(3, delays.Count);
        Assert.True(delays[1] > delays[0], $"Second delay {delays[1]} should be greater than first {delays[0]}");
        Assert.True(delays[2] > delays[1], $"Third delay {delays[2]} should be greater than second {delays[1]}");
    }

    [Fact]
    public async Task RetryWithExponentialBackoffAsync_MaxDelay_CapsDelay()
    {
        // Arrange
        var attemptCount = 0;
        var delays = new List<long>();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var lastTime = sw.ElapsedMilliseconds;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await RetryHelper.RetryWithExponentialBackoffAsync<int>(() =>
            {
                attemptCount++;
                if (attemptCount > 1)
                {
                    var currentTime = sw.ElapsedMilliseconds;
                    delays.Add(currentTime - lastTime);
                    lastTime = currentTime;
                }
                throw new InvalidOperationException("Operation failed");
            }, maxAttempts: 10, initialDelayMs: 100, maxDelayMs: 200, cancellationToken: TestContext.Current.CancellationToken));

        // Assert all delays respect the max delay cap (with tolerance for timing variance)
        Assert.Equal(10, attemptCount);
        Assert.All(delays, delay => Assert.True(delay <= 300, $"Delay {delay} exceeded max with tolerance"));
    }

    [Fact]
    public async Task RetryWithExponentialBackoffAsync_InvalidMaxDelayLessThanInitial_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await RetryHelper.RetryWithExponentialBackoffAsync(() => Task.FromResult(42),
                maxAttempts: 3,
                initialDelayMs: 1000,
                maxDelayMs: 500,
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RetryWithExponentialBackoffAsync_NullAction_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await RetryHelper.RetryWithExponentialBackoffAsync<int>(
                (Func<Task<int>>)null!, maxAttempts: 3, initialDelayMs: 1000, cancellationToken: TestContext.Current.CancellationToken));
    }

    #endregion
}
