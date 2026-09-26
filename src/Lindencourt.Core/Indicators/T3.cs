namespace Lindencourt.Core.Indicators;

/// <summary>
/// T3 — triple (actually 6-fold) EMA smoothing with coefficient b.
/// Used by FX Sniper T3 CCI.
///
/// Formula:
///   n = 1 + 0.5*(period - 1)
///   w1 = 2/(n+1)
///   w2 = 1 - w1
///   e1 = w1*x + w2*e1
///   e2 = w1*e1 + w2*e2
///   ... (up to e6)
///   T3 = c1*e6 + c2*e5 + c3*e4 + c4*e3
/// </summary>
public static class T3
{
    /// <summary>Applies T3 smoothing to an arbitrary value series.</summary>
    public static double[] Calculate(
        IReadOnlyList<double> source,
        int period = 4,
        double b = 0.618)
    {
        if (source == null || source.Count == 0)
            throw new ArgumentException("Source is empty", nameof(source));
        if (period < 1)
            throw new ArgumentOutOfRangeException(nameof(period), "Period must be >= 1");

        int n = source.Count;
        var result = new double[n];

        // Coefficients
        double b2 = b * b;
        double b3 = b2 * b;
        double c1 = -b3;
        double c2 = 3 * (b2 + b3);
        double c3 = -3 * (2 * b2 + b + b3);
        double c4 = 1 + 3 * b + b3 + 3 * b2;

        double nPeriod = 1 + 0.5 * (period - 1);
        double w1 = 2.0 / (nPeriod + 1);
        double w2 = 1.0 - w1;

        // 6-fold EMA state — accumulates across all bars
        double e1 = 0, e2 = 0, e3 = 0, e4 = 0, e5 = 0, e6 = 0;

        for (int i = 0; i < n; i++)
        {
            e1 = w1 * source[i] + w2 * e1;
            e2 = w1 * e1       + w2 * e2;
            e3 = w1 * e2       + w2 * e3;
            e4 = w1 * e3       + w2 * e4;
            e5 = w1 * e4       + w2 * e5;
            e6 = w1 * e5       + w2 * e6;

            result[i] = c1 * e6 + c2 * e5 + c3 * e4 + c4 * e3;
        }

        return result;
    }
}