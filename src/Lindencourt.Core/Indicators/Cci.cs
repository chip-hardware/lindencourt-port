using Lindencourt.Core.Models;

namespace Lindencourt.Core.Indicators;

/// <summary>
/// Commodity Channel Index (standard Lambert formula).
///
/// TP = (H + L + C) / 3
/// SMA_TP = SMA(TP, period)
/// MD = mean absolute deviation of TP from SMA_TP
/// CCI = (TP - SMA_TP) / (0.015 * MD)
///
/// Lindencourt uses T3-smoothed CCI(5).
/// Standard CCI(8) is the documented fallback.
/// </summary>
public static class Cci
{
    public static double[] Calculate(
        IReadOnlyList<Bar> bars,
        int period = 5,
        double constant = 0.015)
    {
        if (bars == null || bars.Count == 0)
            throw new ArgumentException("Bars is empty", nameof(bars));
        if (period < 1)
            throw new ArgumentOutOfRangeException(nameof(period), "Period must be >= 1");

        var result = new double[bars.Count];
        for (int i = 0; i < bars.Count; i++) result[i] = double.NaN;

        if (bars.Count < period)
            return result;

        // Typical Price
        var tp = new double[bars.Count];
        for (int i = 0; i < bars.Count; i++)
            tp[i] = (bars[i].High + bars[i].Low + bars[i].Close) / 3.0;

        // Rolling window
        for (int i = period - 1; i < bars.Count; i++)
        {
            // SMA over the window
            double sum = 0;
            for (int j = i - period + 1; j <= i; j++) sum += tp[j];
            double sma = sum / period;

            // Mean absolute deviation
            double mad = 0;
            for (int j = i - period + 1; j <= i; j++) mad += Math.Abs(tp[j] - sma);
            mad /= period;

            // CCI
            result[i] = mad == 0 ? 0 : (tp[i] - sma) / (constant * mad);
        }

        return result;
    }
}