using Lindencourt.Core.Models;

namespace Lindencourt.Core.Indicators;

/// <summary>
/// Relative Strength Index (Wilder's smoothing).
/// RSI = 100 - 100 / (1 + RS), where RS = avgGain / avgLoss
///
/// Supports:
///   - shift: constant offset subtracted (default 50, per Lindencourt)
///   - multiplier: scale factor (default 1.0; Lindencourt uses 1.5)
/// </summary>
public static class Rsi
{
    /// <summary>
    /// Computes RSI over the given bars.
    /// Returns an array; first `period` values are NaN.
    /// </summary>
    /// <param name="bars">Input bars</param>
    /// <param name="period">RSI period (13 for Lindencourt)</param>
    /// <param name="shift">Constant to subtract (50 for Lindencourt)</param>
    /// <param name="multiplier">Scale factor (1.0 or 1.5)</param>
    public static double[] Calculate(
        IReadOnlyList<Bar> bars,
        int period = 13,
        double shift = 50.0,
        double multiplier = 1.0)
    {
        if (bars == null || bars.Count == 0)
            throw new ArgumentException("Bars is empty", nameof(bars));
        if (period < 2)
            throw new ArgumentOutOfRangeException(nameof(period), "Period must be >= 2");

        var result = new double[bars.Count];
        for (int i = 0; i < bars.Count; i++) result[i] = double.NaN;

        if (bars.Count <= period)
            return result;

        // First value: simple average over the first `period` changes
        double sumGain = 0, sumLoss = 0;
        for (int i = 1; i <= period; i++)
        {
            double change = bars[i].Close - bars[i - 1].Close;
            if (change > 0) sumGain += change;
            else sumLoss -= change; // loss as a positive number
        }
        double avgGain = sumGain / period;
        double avgLoss = sumLoss / period;

        result[period] = ComputeRsiValue(avgGain, avgLoss, shift, multiplier);

        // Wilder smoothing for the rest
        for (int i = period + 1; i < bars.Count; i++)
        {
            double change = bars[i].Close - bars[i - 1].Close;
            double gain = change > 0 ? change : 0;
            double loss = change < 0 ? -change : 0;

            avgGain = (avgGain * (period - 1) + gain) / period;
            avgLoss = (avgLoss * (period - 1) + loss) / period;

            result[i] = ComputeRsiValue(avgGain, avgLoss, shift, multiplier);
        }

        return result;
    }

    private static double ComputeRsiValue(double avgGain, double avgLoss, double shift, double multiplier)
    {
        double rs = avgLoss == 0 ? double.PositiveInfinity : avgGain / avgLoss;
        double rsi = avgLoss == 0 ? 100.0 : 100.0 - 100.0 / (1.0 + rs);
        return (rsi - shift) * multiplier;
    }
}