namespace Lindencourt.Core.Indicators;

/// <summary>
/// Daily Fibonacci Pivot levels used by Lindencourt for partial exits.
///
/// Formulas:
///   P = (H + L + C) / 3
///   R1 = P + (H−L) × 0.382
///   R2 = P + (H−L) × 0.618
///   R3 = P + (H−L) × 1.0
///   R4 = P + (H−L) × 1.618
///   R5 = P + (H−L) × 2.618
///   S1–S5 — mirrored
///   MR1 = (P + R1) / 2, MR2 = (R1 + R2) / 2, ...
///   MS1 = (P + S1) / 2, ...
/// </summary>
public sealed record PivotLevels(
    DateTime DayStart,
    double Pivot,
    double R1, double R2, double R3, double R4, double R5,
    double MR1, double MR2, double MR3, double MR4,
    double S1, double S2, double S3, double S4, double S5,
    double MS1, double MS2, double MS3, double MS4)
{
    /// <summary>All levels as a name → value dictionary.</summary>
    public IReadOnlyDictionary<string, double> AllLevels => new Dictionary<string, double>
    {
        ["P"]   = Pivot,
        ["R1"]  = R1, ["R2"] = R2, ["R3"] = R3, ["R4"] = R4, ["R5"] = R5,
        ["MR1"] = MR1, ["MR2"] = MR2, ["MR3"] = MR3, ["MR4"] = MR4,
        ["S1"]  = S1, ["S2"] = S2, ["S3"] = S3, ["S4"] = S4, ["S5"] = S5,
        ["MS1"] = MS1, ["MS2"] = MS2, ["MS3"] = MS3, ["MS4"] = MS4,
    };

    /// <summary>Returns the nearest level to the given price (name, level, distance).</summary>
    public (string Name, double Level, double Distance) FindNearest(double price)
    {
        string bestName = "P";
        double bestLevel = Pivot;
        double bestDist = Math.Abs(price - Pivot);

        foreach (var (name, level) in AllLevels)
        {
            double d = Math.Abs(price - level);
            if (d < bestDist) { bestDist = d; bestName = name; bestLevel = level; }
        }

        return (bestName, bestLevel, bestDist);
    }

    public override string ToString() =>
        $"P={Pivot:F5} | R1={R1:F5} R2={R2:F5} R3={R3:F5} R4={R4:F5} R5={R5:F5} | " +
        $"S1={S1:F5} S2={S2:F5} S3={S3:F5} S4={S4:F5} S5={S5:F5}";
}