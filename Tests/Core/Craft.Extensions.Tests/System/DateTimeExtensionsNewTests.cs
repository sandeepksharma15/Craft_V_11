namespace Craft.Extensions.Tests.System;

public class DateTimeExtensionsTests
{
    [Fact]
    public void ClearTime_ReturnsStartOfDay_AndPreservesKind()
    {
        var value = new DateTime(2023, 6, 15, 14, 30, 45, DateTimeKind.Utc);

        var result = value.ClearTime();

        Assert.Equal(new DateTime(2023, 6, 15, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, true, false)]
    [InlineData(DayOfWeek.Friday, true, false)]
    [InlineData(DayOfWeek.Saturday, false, true)]
    [InlineData(DayOfWeek.Sunday, false, true)]
    public void DayOfWeekExtensions_ClassifyDays(DayOfWeek day, bool weekday, bool weekend)
    {
        Assert.Equal(weekday, day.IsWeekday());
        Assert.Equal(weekend, day.IsWeekend());
    }

    [Fact]
    public void SetKindUtc_SetsUtcWithoutChangingTicks()
    {
        var value = new DateTime(2023, 6, 15, 14, 30, 45, DateTimeKind.Unspecified);

        var result = value.SetKindUtc();

        Assert.Equal(value.Ticks, result.Ticks);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void SetKindUtc_ReturnsSameValueWhenAlreadyUtc()
    {
        var value = new DateTime(2023, 6, 15, 14, 30, 45, DateTimeKind.Utc);

        Assert.Equal(value, value.SetKindUtc());
    }

    [Fact]
    public void SetKindUtc_Nullable_ReturnsNullForNull()
    {
        DateTime? value = null;

        Assert.Null(value.SetKindUtc());
    }

    [Fact]
    public void SetKindUtc_Nullable_SetsUtc()
    {
        DateTime? value = new DateTime(2023, 6, 15, 14, 30, 45);

        var result = value.SetKindUtc();

        Assert.Equal(DateTimeKind.Utc, result?.Kind);
    }

    [Theory]
    [InlineData("1990-12-25", "2023-06-15", 32)]
    [InlineData("1990-03-15", "2023-06-15", 33)]
    [InlineData("1990-06-15", "2023-06-15", 33)]
    public void CalculateAge_ReturnsExpectedAge(string birthDate, string referenceDate, int expected)
    {
        Assert.Equal(expected, DateTime.Parse(birthDate).CalculateAge(DateTime.Parse(referenceDate)));
    }

    [Fact]
    public void CalculateAge_WithoutReferenceDate_UsesToday()
    {
        var birthDate = DateTime.Today.AddYears(-25);

        Assert.Equal(25, birthDate.CalculateAge());
    }

    [Fact]
    public void StartAndEndOfMonth_ReturnExactBoundaries()
    {
        var value = new DateTime(2024, 2, 15, 14, 30, 45, 123, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc), value.StartOfMonth());
        Assert.Equal(new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(-1), value.EndOfMonth());
    }

    [Fact]
    public void StartAndEndOfYear_ReturnExactBoundaries()
    {
        var value = new DateTime(2024, 6, 15, 14, 30, 45, 123, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), value.StartOfYear());
        Assert.Equal(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(-1), value.EndOfYear());
    }

    [Theory]
    [InlineData("2023-06-15", DayOfWeek.Monday, "2023-06-12")]
    [InlineData("2023-06-12", DayOfWeek.Monday, "2023-06-12")]
    [InlineData("2023-06-18", DayOfWeek.Monday, "2023-06-12")]
    [InlineData("2023-06-15", DayOfWeek.Sunday, "2023-06-11")]
    public void StartOfWeek_ReturnsExpectedStart(string dateText, DayOfWeek startOfWeek, string expected)
    {
        Assert.Equal(DateTime.Parse(expected), DateTime.Parse(dateText).StartOfWeek(startOfWeek));
    }

    [Fact]
    public void EndOfWeek_ReturnsLastTickOfWeek()
    {
        var value = new DateTime(2023, 6, 15, 14, 30, 45, DateTimeKind.Utc);

        var result = value.EndOfWeek();

        Assert.Equal(new DateTime(2023, 6, 19, DateTimeKind.Utc).AddTicks(-1), result);
    }

    [Theory]
    [InlineData(5, "2023-06-22")]
    [InlineData(-5, "2023-06-08")]
    [InlineData(0, "2023-06-15")]
    public void AddBusinessDays_ReturnsExpectedDate(int days, string expected)
    {
        var result = new DateTime(2023, 6, 15).AddBusinessDays(days);

        Assert.Equal(DateTime.Parse(expected), result);
    }

    [Fact]
    public void AddBusinessDays_HandlesWeekendStart()
    {
        Assert.Equal(
            new DateTime(2023, 6, 19),
            new DateTime(2023, 6, 17).AddBusinessDays(1));
    }

    [Theory]
    [InlineData("2023-06-15", true)]
    [InlineData("2023-06-17", false)]
    [InlineData("2023-06-18", false)]
    public void DateWeekdayClassification_ReturnsExpected(string dateText, bool expected)
    {
        Assert.Equal(expected, DateTime.Parse(dateText).IsWeekday());
        Assert.Equal(!expected, DateTime.Parse(dateText).IsWeekend());
    }

    [Fact]
    public void IsToday_ReturnsExpectedResult()
    {
        Assert.True(DateTime.Today.IsToday());
        Assert.False(DateTime.Today.AddDays(-1).IsToday());
    }

    [Fact]
    public void IsPastAndFuture_WithReferenceDate_AreDeterministic()
    {
        var reference = new DateTime(2023, 6, 15);

        Assert.True(new DateTime(2023, 6, 14).IsPast(reference));
        Assert.False(new DateTime(2023, 6, 15).IsPast(reference));
        Assert.True(new DateTime(2023, 6, 16).IsFuture(reference));
        Assert.False(new DateTime(2023, 6, 15).IsFuture(reference));
    }

    [Fact]
    public void IsPastAndFuture_UseCurrentTime()
    {
        Assert.True(DateTime.Now.AddSeconds(-1).IsPast());
        Assert.True(DateTime.Now.AddSeconds(1).IsFuture());
    }

    [Fact]
    public void SetTime_SetsSpecifiedTime_AndPreservesKind()
    {
        var value = new DateTime(2023, 6, 15, 10, 20, 30, DateTimeKind.Utc);

        Assert.Equal(
            new DateTime(2023, 6, 15, 14, 30, 45, 500, DateTimeKind.Utc),
            value.SetTime(14, 30, 45, 500));
    }

    [Fact]
    public void At_SetsTimeFromTimeSpan()
    {
        var value = new DateTime(2023, 6, 15, 10, 20, 30);

        Assert.Equal(new DateTime(2023, 6, 15, 14, 30, 45), value.At(new TimeSpan(14, 30, 45)));
    }

    [Theory]
    [InlineData("2023-06-15", "2023-06-10", "2023-06-20", true)]
    [InlineData("2023-06-15", "2023-06-15", "2023-06-20", true)]
    [InlineData("2023-06-15", "2023-06-10", "2023-06-15", true)]
    [InlineData("2023-06-15", "2023-06-16", "2023-06-20", false)]
    [InlineData("2023-06-15", "2023-06-10", "2023-06-14", false)]
    public void IsBetween_ReturnsExpectedResult(string dateText, string startText, string endText, bool expected)
    {
        Assert.Equal(
            expected,
            DateTime.Parse(dateText).IsBetween(DateTime.Parse(startText), DateTime.Parse(endText)));
    }
}
