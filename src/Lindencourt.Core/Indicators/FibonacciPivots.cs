using Lindencourt.Core.Models;

namespace Lindencourt.Core.Indicators;

/// <summary>
/// Daily Fibonacci Pivot levels calculator.
///
/// Important: a "trading day" starts at 17:00 NY (not 00:00 GMT).
/// The pivot for day D is computed from bars in the interval
/// [17:00 NY of the previous day, 17:00 NY of day D).
///
/// Parameter newYorkOffsetHours is the NY offset relative to GMT
/// (-5 for EST, -4 for EDT).
/// </summary>
public static class FibonacciPivots
{
    /// <summary>Computes pivot levels from a single day's H/L/C.</summary>
    public static PivotLevels Calculate(double high, double low, double close, DateTime dayStart)
    {
        double p = (high + low + close) / 3.0;
        double range = high - low;

        double r1 = p + range * 0.382;
        double r2 = p + range * 0.618;
        double r3 = p + range * 1.0;
        double r4 = p + range * 1.618;
        double r5 = p + range * 2.618;

        double s1 = p - range * 0.382;
        double s2 = p - range * 0.618;
        double s3 = p - range * 1.0;
        double s4 = p - range * 1.618;
        double s5 = p - range * 2.618;

        double mr1 = (p + r1) / 2;
        double mr2 = (r1 + r2) / 2;
        double mr3 = (r2 + r3) / 2;
        double mr4 = (r3 + r4) / 2;

        double ms1 = (p + s1) / 2;
        double ms2 = (s1 + s2) / 2;
        double ms3 = (s2 + s3) / 2;
        double ms4 = (s3 + s4) / 2;

        return new PivotLevels(dayStart, p,
            r1, r2, r3, r4, r5,
            mr1, mr2, mr3, mr4,
            s1, s2, s3, s4, s5,
            ms1, ms2, ms3, ms4);
    }

    /// <summary>
    /// Computes daily pivot levels for the full bar history.
    /// Returns a dictionary: trading day (in data time) → PivotLevels.
    ///
    /// Algorithm:
    ///   1. For each bar, determine the trading day (if hour >= dayStartHour, it belongs to the next day).
    ///   2. Group bars by day, compute H/L/C.
    ///   3. Compute levels for each day.
    /// </summary>
    /// <param name="bars">Input bars</param>
    /// <param name="newYorkOffsetHours">-5 (EST) or -4 (EDT). Default -5.</param>
    public static Dictionary<DateTime, PivotLevels> CalculateDaily(
        IReadOnlyList<Bar> bars,
        int newYorkOffsetHours = -5)
    {
        if (bars == null || bars.Count == 0)
            throw new ArgumentException("Bars is empty", nameof(bars));

        // Trading day start hour in data time:
        // 17:00 NY = 17:00 - offset (offset is negative)
        // EST (-5): 17:00 - (-5) = 22:00
        // EDT (-4): 17:00 - (-4) = 21:00
        int dayStartHour = 17 - newYorkOffsetHours;

        // Group bars by trading day
        var dayBars = new Dictionary<DateTime, (double H, double L, double C, DateTime DayStart)>();

        foreach (var bar in bars)
        {
            DateTime tradingDay;
            if (bar.Time.Hour >= dayStartHour)
                tradingDay = bar.Time.Date.AddDays(1);
            else
                tradingDay = bar.Time.Date;

            if (!dayBars.TryGetValue(tradingDay, out var agg))
            {
                agg = (bar.High, bar.Low, bar.Close, tradingDay);
            }
            else
            {
                agg.H = Math.Max(agg.H, bar.High);
                agg.L = Math.Min(agg.L, bar.Low);
                agg.C = bar.Close; // last close
            }
            dayBars[tradingDay] = agg;
        }

        // Compute levels for each day
        var result = new Dictionary<DateTime, PivotLevels>();
        foreach (var (day, agg) in dayBars)
        {
            result[day] = Calculate(agg.H, agg.L, agg.C, day);
        }

        return result;
    }

    /// <summary>
    /// Returns the pivot levels in effect for a specific bar.
    /// Uses the PREVIOUS trading day's pivot (that is the one currently valid).
    /// </summary>
    public static PivotLevels? ForBar(
        Bar bar,
        IReadOnlyDictionary<DateTime, PivotLevels> allPivots,
        int newYorkOffsetHours = -5)
    {
        int dayStartHour = 17 - newYorkOffsetHours;

        DateTime currentTradingDay;
        if (bar.Time.Hour >= dayStartHour)
            currentTradingDay = bar.Time.Date.AddDays(1);
        else
            currentTradingDay = bar.Time.Date;

        // The pivot for the current day is computed from the PREVIOUS day's H/L/C
        DateTime previousDay = currentTradingDay.AddDays(-1);

        // Skip weekends
        while (previousDay.DayOfWeek == DayOfWeek.Saturday ||
               previousDay.DayOfWeek == DayOfWeek.Sunday)
        {
            previousDay = previousDay.AddDays(-1);
        }

        return allPivots.TryGetValue(previousDay, out var levels) ? levels : null;
    }
}