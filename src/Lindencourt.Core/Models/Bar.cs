namespace Lindencourt.Core.Models;

/// <summary>
/// A single price bar (candle).
/// Immutable record — suitable for parallel computation.
/// </summary>
public sealed record Bar(
    DateTime Time,
    double Open,
    double High,
    double Low,
    double Close,
    double Volume)
{
    public double Range => High - Low;
    public double Body => Math.Abs(Close - Open);
    public bool IsBullish => Close > Open;
    public bool IsBearish => Close < Open;
    public double Midpoint => (High + Low) / 2.0;

    public override string ToString() =>
        $"{Time:yyyy-MM-dd HH:mm:ss} O={Open} H={High} L={Low} C={Close} V={Volume}";
}