using Lindencourt.Core.Models;

namespace Lindencourt.Backtest;

public enum TradeDirection { Buy, Sell }
public enum TradeStatus { Open, ClosedBySignal, ClosedByStopLoss, ClosedByTakeProfit }

/// <summary>
/// Represents a single trade with entry, exit, and result.
/// </summary>
public sealed class Trade
{
    public int Id { get; init; }
    public string Symbol { get; init; } = "";
    public TradeDirection Direction { get; init; }
    public DateTime EntryTime { get; init; }
    public double EntryPrice { get; init; }
    public double StopLoss { get; init; }
    public double InitialStopLoss { get; init; }
    public double Lots { get; init; }

    public DateTime? ExitTime { get; set; }
    public double? ExitPrice { get; set; }
    public TradeStatus Status { get; set; } = TradeStatus.Open;

    /// <summary>Pip size depends on the symbol (0.0001 for most, 0.01 for JPY).</summary>
    public double PipSize { get; init; } = 0.0001;

    public double PipsResult => ExitPrice.HasValue
        ? (ExitPrice.Value - EntryPrice) / PipSize * (Direction == TradeDirection.Buy ? 1 : -1)
        : 0;

    public double MoneyResult { get; set; }

    public TimeSpan Duration => ExitTime.HasValue ? ExitTime.Value - EntryTime : TimeSpan.Zero;

    public override string ToString() =>
        $"#{Id} {Direction} {Symbol} @ {EntryPrice:F5} " +
        (ExitPrice.HasValue
            ? $"→ {ExitPrice.Value:F5} ({PipsResult:+0.0;-0.0} pips, {Duration.TotalMinutes:F0}m, {Status})"
            : "OPEN");
}