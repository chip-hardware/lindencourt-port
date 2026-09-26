namespace Lindencourt.Core.Models;

public enum TimeFrame
{
    M1 = 1,
    M5 = 5,
    M15 = 15,
    M30 = 30,
    H1 = 60,
    H4 = 240,
    D1 = 1440,
    W1 = 10080,
    MN1 = 43200
}

public static class TimeFrameExtensions
{
    public static int ToMinutes(this TimeFrame tf) => (int)tf;

    public static TimeSpan ToTimeSpan(this TimeFrame tf) => TimeSpan.FromMinutes((int)tf);

    /// <summary>EMA(21) period on the higher timeframe, converted to the current timeframe.</summary>
    public static int HigherTfEma21(this TimeFrame current, TimeFrame higher) =>
        21 * ((int)higher / (int)current);

    /// <summary>
    /// Relevant trend line period for the current timeframe
    /// (per SPEC: EMA(84) for 15M, EMA(126) for 4H, etc.).
    /// </summary>
    public static int RelevantTrendPeriod(this TimeFrame tf) => tf switch
    {
        TimeFrame.M1  => 105,   // 5M EMA(21)
        TimeFrame.M5  => 63,    // 15M EMA(21)
        TimeFrame.M15 => 84,    // 1H EMA(21)
        TimeFrame.M30 => 84,    // (not used by Lindencourt, kept for completeness)
        TimeFrame.H1  => 84,    // 4H EMA(21)
        TimeFrame.H4  => 126,   // Daily EMA(21)
        TimeFrame.D1  => 105,   // Weekly EMA(21)
        _ => throw new NotSupportedException($"Relevant trend period not defined for {tf}")
    };
}