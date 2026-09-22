#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class DateTimeExtensions
{
    extension(DateTime dateTime)
    {
        public DateTime ClearTime() => dateTime.Date;

        public DateTime SetKindUtc()
            => dateTime.Kind == DateTimeKind.Utc
                ? dateTime
                : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

        public int CalculateAge()
            => dateTime.CalculateAge(DateTime.Today);

        public int CalculateAge(DateTime referenceDate)
        {
            var age = referenceDate.Year - dateTime.Year;

            if (referenceDate.Month < dateTime.Month ||
                (referenceDate.Month == dateTime.Month && referenceDate.Day < dateTime.Day))
                age--;

            return age;
        }

        public DateTime StartOfMonth()
            => dateTime.Date.AddDays(1 - dateTime.Day);

        public DateTime EndOfMonth()
            => dateTime.StartOfMonth().AddMonths(1).AddTicks(-1);

        public DateTime StartOfYear()
            => new(dateTime.Year, 1, 1, 0, 0, 0, 0, dateTime.Kind);

        public DateTime EndOfYear()
            => dateTime.StartOfYear().AddYears(1).AddTicks(-1);

        public DateTime StartOfWeek(DayOfWeek startOfWeek = DayOfWeek.Monday)
        {
            var diff = (7 + (dateTime.DayOfWeek - startOfWeek)) % 7;
            return dateTime.Date.AddDays(-diff);
        }

        public DateTime EndOfWeek(DayOfWeek startOfWeek = DayOfWeek.Monday)
            => dateTime.StartOfWeek(startOfWeek).AddDays(7).AddTicks(-1);

        public DateTime AddBusinessDays(int businessDays)
        {
            var sign = Math.Sign(businessDays);
            var remaining = Math.Abs((long)businessDays);
            var result = dateTime;

            while (remaining-- > 0)
            {
                do
                {
                    result = result.AddDays(sign);
                }
                while (result.DayOfWeek.IsWeekend());
            }

            return result;
        }

        public bool IsWeekday() => dateTime.DayOfWeek.IsWeekday();

        public bool IsWeekend() => dateTime.DayOfWeek.IsWeekend();

        public bool IsToday() => dateTime.Date == DateTime.Today;

        public bool IsPast() => dateTime < DateTime.Now;

        public bool IsPast(DateTime referenceDate) => dateTime < referenceDate;

        public bool IsFuture() => dateTime > DateTime.Now;

        public bool IsFuture(DateTime referenceDate) => dateTime > referenceDate;

        public DateTime SetTime(int hour, int minute = 0, int second = 0, int millisecond = 0)
            => new(dateTime.Year, dateTime.Month, dateTime.Day, hour, minute, second, millisecond, dateTime.Kind);

        public DateTime At(TimeSpan time) => dateTime.ClearTime().Add(time);

        public bool IsBetween(DateTime startDate, DateTime endDate)
            => dateTime >= startDate && dateTime <= endDate;
    }

    extension(DateTime? dateTime)
    {
        public DateTime? SetKindUtc()
            => dateTime.HasValue ? dateTime.Value.SetKindUtc() : null;
    }

    extension(DayOfWeek dayOfWeek)
    {
        public bool IsWeekday()
            => dayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday;

        public bool IsWeekend()
            => dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }
}
