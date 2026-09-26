using Lindencourt.Core.Models;

namespace Lindencourt.Core.Indicators;

/// <summary>
/// Exponential Moving Average.
/// alpha = 2 / (period + 1)
/// EMA[i] = alpha * price[i] + (1 - alpha) * EMA[i-1]
///
/// Initialization: EMA[period-1] = SMA(period) over the first `period` bars.
/// This is the standard MQL4/MT4 convention.
/// </summary>
public static class Ema
{
    /// <summary>
    /// Computes EMA over the Close prices of the given bars.
    /// Returns an array of the same length; first (period-1) values are NaN.
    /// </summary>
    public static double[] Calculate(IReadOnlyList<Bar> bars, int period)
    {
        if (bars == null || bars.Count == 0)
            throw new ArgumentException("Bars is empty", nameof(bars));
        if (period < 1)
            throw new ArgumentOutOfRangeException(nameof(period), "Period must be >= 1");

        var result = new double[bars.Count];
        var alpha = 2.0 / (period + 1.0);

        // Initialization: SMA over the first `period` bars
        if (bars.Count < period)
        {
            for (int i = 0; i < bars.Count; i++) result[i] = double.NaN;
            return result;
        }

        double sum = 0;
        for (int i = 0; i < period; i++) sum += bars[i].Close;
        double sma = sum / period;

        for (int i = 0; i < period - 1; i++) result[i] = double.NaN;
        result[period - 1] = sma;

        // Recursive part
        for (int i = period; i < bars.Count; i++)
        {
            result[i] = alpha * bars[i].Close + (1 - alpha) * result[i - 1];
        }

        return result;
    }

    /// <summary>
    /// Computes EMA over an arbitrary value series (not just Close).
    /// </summary>
    public static double[] Calculate(IReadOnlyList<double> values, int period)
    {
        if (values == null || values.Count == 0)
            throw new ArgumentException("Values is empty", nameof(values));
        if (period < 1)
            throw new ArgumentOutOfRangeException(nameof(period), "Period must be >= 1");

        var result = new double[values.Count];
        var alpha = 2.0 / (period + 1.0);

        if (values.Count < period)
        {
            for (int i = 0; i < values.Count; i++) result[i] = double.NaN;
            return result;
        }

        double sum = 0;
        for (int i = 0; i < period; i++) sum += values[i];
        double sma = sum / period;

        for (int i = 0; i < period - 1; i++) result[i] = double.NaN;
        result[period - 1] = sma;

        for (int i = period; i < values.Count; i++)
        {
            result[i] = alpha * values[i] + (1 - alpha) * result[i - 1];
        }

        return result;
    }
}