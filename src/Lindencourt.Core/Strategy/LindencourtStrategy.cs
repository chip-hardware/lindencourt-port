using Lindencourt.Core.Indicators;
using Lindencourt.Core.Models;

namespace Lindencourt.Core.Strategy;

/// <summary>
/// Lindencourt FX System strategy (v2 — corrected).
///
/// Key changes from v1:
///   1. EMA(7)/EMA(21) cross — must have occurred "recently" (lookback N bars), not exactly on the current bar
///   2. RSI Histo — "at least one bar >= +10" within the last N bars (N=4, 1 hour)
///   3. Exit — CCI cross 0 AND candle closed against EMA(7), but NOT necessarily on the same bar
///   4. Minimum time in position — 1 bar
///   5. Entry on the open of the next bar
/// </summary>
public sealed class LindencourtStrategy
{
    // Indicator parameters
    public int EmaFast { get; init; } = 7;
    public int EmaSlow { get; init; } = 21;
    public int EmaTrend { get; init; } = 84;
    public int CciPeriod { get; init; } = 5;
    public int T3Period { get; init; } = 5;
    public double T3B { get; init; } = 0.618;
    public int RsiPeriod { get; init; } = 13;
    public double RsiThreshold { get; init; } = 10.0;
    public double RsiMultiplier { get; init; } = 1.5; // Lindencourt original

    // Lookback parameters
    /// <summary>How many bars back the EMA(7)/EMA(21) cross may have occurred.</summary>
    public int EmaCrossLookback { get; init; } = 8; // 2 hours

    /// <summary>How many bars back the RSI threshold may have been reached.</summary>
    public int RsiLookback { get; init; } = 4; // 1 hour

    /// <summary>Minimum bars in position before exit is allowed.</summary>
    public int MinBarsInPosition { get; init; } = 1;

    // Time filter
    public bool UseTimeFilter { get; init; } = true;
    public int EarliestEntryHourGmt { get; init; } = 6;
    public int EarliestEntryMinuteGmt { get; init; } = 45;
    public int LatestEntryHourGmt { get; init; } = 16;

    public IReadOnlyList<Signal> Run(IReadOnlyList<Bar> bars)
    {
        if (bars == null || bars.Count == 0)
            throw new ArgumentException("Bars is empty", nameof(bars));

        // Compute indicators
        var ema7  = Ema.Calculate(bars, EmaFast);
        var ema21 = Ema.Calculate(bars, EmaSlow);
        var ema84 = Ema.Calculate(bars, EmaTrend);
        var t3cci = T3Cci.Calculate(bars, CciPeriod, T3Period, T3B);
        var rsi   = Rsi.Calculate(bars, RsiPeriod, 50.0, RsiMultiplier);

        var signals = new List<Signal>();

        int position = 0; // 0 = flat, +1 = long, -1 = short
        int entryBarIndex = -1;
        bool priceClosedAgainstEma7 = false; // whether a candle has already closed against EMA(7) since entry

        int startIdx = Math.Max(EmaTrend, Math.Max(RsiPeriod + 1, CciPeriod + T3Period + 10));

        for (int i = startIdx; i < bars.Count - 1; i++) // -1 because entry happens on i+1
        {
            // === 1. EXIT CHECK (if position is open) ===
            if (position != 0)
            {
                int barsInPosition = i - entryBarIndex;

                // Update "candle closed against EMA(7)" state
                if (!priceClosedAgainstEma7 && barsInPosition >= MinBarsInPosition)
                {
                    if (position == 1 && bars[i].Close < ema7[i])
                        priceClosedAgainstEma7 = true;
                    else if (position == -1 && bars[i].Close > ema7[i])
                        priceClosedAgainstEma7 = true;
                }

                // Check CCI cross 0
                bool cciCrossed = position == 1
                    ? (t3cci[i] < 0 && t3cci[i - 1] >= 0)
                    : (t3cci[i] > 0 && t3cci[i - 1] <= 0);

                // Exit only when BOTH conditions are true:
                //   1) a candle has closed against EMA(7) (may have happened on a previous bar)
                //   2) CCI crossed 0 (on the current bar)
                if (priceClosedAgainstEma7 && cciCrossed)
                {
                    signals.Add(new Signal(
                        position == 1 ? SignalType.ExitBuy : SignalType.ExitSell,
                        i + 1,
                        bars[i + 1].Time,
                        bars[i + 1].Open,
                        position == 1
                            ? "BUY exit: close<EMA7 + T3CCI cross 0 down"
                            : "SELL exit: close>EMA7 + T3CCI cross 0 up"));

                    position = 0;
                    entryBarIndex = -1;
                    priceClosedAgainstEma7 = false;
                    continue;
                }

                // Position is open — do not look for a new entry
                continue;
            }

            // === 2. ENTRY CHECK (only if flat) ===
            if (UseTimeFilter && !IsWithinTradingHours(bars[i].Time))
                continue;

            if (i < 1 || double.IsNaN(ema7[i]) || double.IsNaN(ema21[i]) ||
                double.IsNaN(ema84[i]) || double.IsNaN(t3cci[i]) || double.IsNaN(rsi[i]))
                continue;

            // Condition 1: EMA(7) crossed EMA(21) within the last N bars
            bool emaCrossUp   = HasEmaCrossedUp(ema7, ema21, i, EmaCrossLookback);
            bool emaCrossDown = HasEmaCrossedDown(ema7, ema21, i, EmaCrossLookback);

            // Condition 2: candle crossed and closed beyond EMA(84)
            bool closedAbove84 = bars[i].Close > ema84[i] && bars[i].High >= ema84[i];
            bool closedBelow84 = bars[i].Close < ema84[i] && bars[i].Low <= ema84[i];

            // Condition 3: T3 CCI >= 0 (BUY) or <= 0 (SELL) at the close of bar i
            //              (= at the open of bar i+1, where entry will occur)
            bool cciAboveZero = t3cci[i] >= 0;
            bool cciBelowZero = t3cci[i] <= 0;

            // Condition 4: RSI Histo >= +10 (BUY) or <= -10 (SELL) within the last N bars
            bool rsiAbove = HasRsiReached(rsi, i, +RsiThreshold, RsiLookback);
            bool rsiBelow = HasRsiReached(rsi, i, -RsiThreshold, RsiLookback);

            // === BUY ===
            if (emaCrossUp && closedAbove84 && cciAboveZero && rsiAbove)
            {
                signals.Add(new Signal(
                    SignalType.Buy, i + 1,
                    bars[i + 1].Time, bars[i + 1].Open,
                    $"BUY: EMA7x21 up (last {EmaCrossLookback}) + close>84 + T3CCI>=0 + RSI>={RsiThreshold} (last {RsiLookback})"));

                position = 1;
                entryBarIndex = i + 1;
                priceClosedAgainstEma7 = false;
                continue;
            }

            // === SELL ===
            if (emaCrossDown && closedBelow84 && cciBelowZero && rsiBelow)
            {
                signals.Add(new Signal(
                    SignalType.Sell, i + 1,
                    bars[i + 1].Time, bars[i + 1].Open,
                    $"SELL: EMA7x21 down (last {EmaCrossLookback}) + close<84 + T3CCI<=0 + RSI<={RsiThreshold} (last {RsiLookback})"));

                position = -1;
                entryBarIndex = i + 1;
                priceClosedAgainstEma7 = false;
                continue;
            }
        }

        return signals;
    }

    /// <summary>Has EMA7 crossed above EMA21 within the last N bars?</summary>
    private static bool HasEmaCrossedUp(double[] ema7, double[] ema21, int i, int lookback)
    {
        for (int j = Math.Max(1, i - lookback + 1); j <= i; j++)
        {
            if (double.IsNaN(ema7[j]) || double.IsNaN(ema21[j]) ||
                double.IsNaN(ema7[j - 1]) || double.IsNaN(ema21[j - 1]))
                continue;

            if (ema7[j] > ema21[j] && ema7[j - 1] <= ema21[j - 1])
                return true;
        }
        return false;
    }

    /// <summary>Has EMA7 crossed below EMA21 within the last N bars?</summary>
    private static bool HasEmaCrossedDown(double[] ema7, double[] ema21, int i, int lookback)
    {
        for (int j = Math.Max(1, i - lookback + 1); j <= i; j++)
        {
            if (double.IsNaN(ema7[j]) || double.IsNaN(ema21[j]) ||
                double.IsNaN(ema7[j - 1]) || double.IsNaN(ema21[j - 1]))
                continue;

            if (ema7[j] < ema21[j] && ema7[j - 1] >= ema21[j - 1])
                return true;
        }
        return false;
    }

    /// <summary>Has RSI reached the threshold (positive or negative) within the last N bars?</summary>
    private static bool HasRsiReached(double[] rsi, int i, double threshold, int lookback)
    {
        int start = Math.Max(0, i - lookback + 1);
        for (int j = start; j <= i; j++)
        {
            if (double.IsNaN(rsi[j])) continue;
            if (threshold > 0 && rsi[j] >= threshold) return true;
            if (threshold < 0 && rsi[j] <= threshold) return true;
        }
        return false;
    }

    private static bool IsWithinTradingHours(DateTime timeGmt)
    {
        var t = timeGmt.TimeOfDay;
        var earliest = new TimeSpan(6, 45, 0);
        var latest   = new TimeSpan(16, 0, 0);
        return t >= earliest && t <= latest;
    }
}