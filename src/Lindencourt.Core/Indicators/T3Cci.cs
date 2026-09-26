using Lindencourt.Core.Models;

namespace Lindencourt.Core.Indicators;

/// <summary>
/// T3-smoothed CCI (FX Sniper).
/// Pipeline: CCI → T3.
///
/// Parameters from MQL4:
///   CCI_Period = 5
///   T3_Period = 5   (matches FXST3CCI(5, 5) on MT4)
///   b = 0.618
/// </summary>
public static class T3Cci
{
    public static double[] Calculate(
        IReadOnlyList<Bar> bars,
        int cciPeriod = 5,
        int t3Period = 5,
        double b = 0.618)
    {
        // 1. Raw CCI
        var cci = Cci.Calculate(bars, cciPeriod);

        // 2. Replace NaN with 0 for the warm-up region (mirrors MQL4 where e1..e6 start at 0)
        int firstValid = cciPeriod - 1;
        var cleanedCci = new double[cci.Length];
        for (int i = 0; i < cci.Length; i++)
            cleanedCci[i] = i < firstValid || double.IsNaN(cci[i]) ? 0.0 : cci[i];

        // 3. T3 smoothing
        return T3.Calculate(cleanedCci, t3Period, b);
    }
}