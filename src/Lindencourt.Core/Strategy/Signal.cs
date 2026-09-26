namespace Lindencourt.Core.Strategy;

public enum SignalType
{
    None,
    Buy,
    Sell,
    ExitBuy,
    ExitSell
}

/// <summary>Strategy signal.</summary>
public sealed record Signal(
    SignalType Type,
    int BarIndex,
    DateTime Time,
    double Price,
    string Reason)
{
    public bool IsEntry => Type == SignalType.Buy || Type == SignalType.Sell;
    public bool IsExit  => Type == SignalType.ExitBuy || Type == SignalType.ExitSell;

    public override string ToString() =>
        $"{Time:yyyy-MM-dd HH:mm} {Type,-8} @ {Price:F5} — {Reason}";
}