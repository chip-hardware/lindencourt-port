namespace Lindencourt.Backtest;

/// <summary>
/// Backtest configuration: capital, risk, stop loss, lot sizing.
/// </summary>
public sealed class BacktestSettings
{
    /// <summary>Initial account capital.</summary>
    public double InitialCapital { get; init; } = 10_000;

    /// <summary>Risk per trade in percent (1% per Lindencourt PDF).</summary>
    public double RiskPercent { get; init; } = 1.0;

    /// <summary>Maximum risk per trade in percent (2% per PDF).</summary>
    public double MaxRiskPercent { get; init; } = 2.0;

    /// <summary>Stop loss in pips. Fast pairs: 35, slow pairs: 25.</summary>
    public double StopLossPips { get; init; } = 25;

    /// <summary>Enable stop loss simulation.</summary>
    public bool UseStopLoss { get; init; } = true;

    /// <summary>Pip size (0.0001 for most pairs, 0.01 for JPY).</summary>
    public double PipSize { get; init; } = 0.0001;

    /// <summary>Value of 1 pip for 1 standard lot in account currency.</summary>
    public double PipValuePerLot { get; init; } = 10.0;

    /// <summary>Minimum lot size (0.01).</summary>
    public double MinLots { get; init; } = 0.01;

    /// <summary>Maximum lot size.</summary>
    public double MaxLots { get; init; } = 100.0;

    /// <summary>Maximum concurrent open trades.</summary>
    public int MaxConcurrentTrades { get; init; } = 1;

    /// <summary>Commission per lot in account currency.</summary>
    public double CommissionPerLot { get; init; } = 0.0;

    /// <summary>Spread in pips (added to entry price).</summary>
    public double SpreadPips { get; init; } = 0.0;
}