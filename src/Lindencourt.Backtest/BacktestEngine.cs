using Lindencourt.Core.Indicators;
using Lindencourt.Core.Models;
using Lindencourt.Core.Strategy;

namespace Lindencourt.Backtest;

/// <summary>
/// Aggregated backtest results and performance metrics.
/// </summary>
public sealed class BacktestResult
{
    public double InitialCapital { get; init; }
    public double FinalCapital { get; set; }
    public double TotalReturnPercent => (FinalCapital - InitialCapital) / InitialCapital * 100;
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public double WinRate => TotalTrades > 0 ? 100.0 * WinningTrades / TotalTrades : 0;
    public double TotalPips { get; set; }
    public double TotalMoney { get; set; }
    public double AvgWinPips { get; set; }
    public double AvgLossPips { get; set; }
    public double MaxDrawdownPercent { get; set; }
    public double ProfitFactor { get; set; }
    public double SharpeRatio { get; set; }
    public List<Trade> Trades { get; } = new();

    public void Print()
    {
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  BACKTEST RESULT");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine($"  Initial capital:      {InitialCapital,10:N2}");
        Console.WriteLine($"  Final capital:        {FinalCapital,10:N2}");
        Console.WriteLine($"  Return:               {TotalReturnPercent,10:F2} %");
        Console.WriteLine();
        Console.WriteLine($"  Total trades:         {TotalTrades,10}");
        Console.WriteLine($"  Winners:              {WinningTrades,10}");
        Console.WriteLine($"  Losers:               {LosingTrades,10}");
        Console.WriteLine($"  Win rate:             {WinRate,10:F1} %");
        Console.WriteLine();
        Console.WriteLine($"  Total pips:           {TotalPips,10:F1}");
        Console.WriteLine($"  Total money:          {TotalMoney,10:N2}");
        Console.WriteLine($"  Avg win:              {AvgWinPips,10:F1} pips");
        Console.WriteLine($"  Avg loss:             {AvgLossPips,10:F1} pips");
        Console.WriteLine($"  Profit factor:        {ProfitFactor,10:F2}");
        Console.WriteLine($"  Sharpe ratio:         {SharpeRatio,10:F2}");
        Console.WriteLine($"  Max drawdown:         {MaxDrawdownPercent,10:F2} %");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
    }
}

/// <summary>
/// Bar-by-bar backtest simulator with stop loss and fixed-fractional position sizing.
/// </summary>
public sealed class BacktestEngine
{
    private readonly BacktestSettings _settings;
    private readonly SymbolInfo _symbol;

    public BacktestEngine(BacktestSettings settings, SymbolInfo symbol)
    {
        _settings = settings;
        _symbol = symbol;
    }

    public BacktestResult Run(IReadOnlyList<Bar> bars, LindencourtStrategy strategy)
    {
        var signals = strategy.Run(bars);
        var result = new BacktestResult { InitialCapital = _settings.InitialCapital };

        double capital = _settings.InitialCapital;
        double peakCapital = capital;
        double maxDrawdown = 0;
        Trade? openTrade = null;
        int tradeId = 0;
        var equityCurve = new List<double> { capital };

        var signalIndex = 0;
        var sortedSignals = signals.OrderBy(s => s.BarIndex).ToList();

        for (int i = 0; i < bars.Count; i++)
        {
            var bar = bars[i];

            // === 1. Process signals on this bar ===
            while (signalIndex < sortedSignals.Count && sortedSignals[signalIndex].BarIndex == i)
            {
                var signal = sortedSignals[signalIndex];

                if (signal.IsEntry && openTrade == null)
                {
                    // Open a new trade
                    tradeId++;
                    var dir = signal.Type == SignalType.Buy ? TradeDirection.Buy : TradeDirection.Sell;
                    double slPips = _settings.StopLossPips;
                    double sl = dir == TradeDirection.Buy
                        ? signal.Price - slPips * _settings.PipSize
                        : signal.Price + slPips * _settings.PipSize;

                    double lots = CalculateLots(capital, slPips);

                    openTrade = new Trade
                    {
                        Id = tradeId,
                        Symbol = _symbol.Symbol,
                        Direction = dir,
                        EntryTime = signal.Time,
                        EntryPrice = signal.Price,
                        StopLoss = sl,
                        InitialStopLoss = sl,
                        Lots = lots,
                        PipSize = _settings.PipSize,
                    };
                }
                else if (signal.IsExit && openTrade != null)
                {
                    // Close by strategy signal
                    CloseTrade(openTrade, signal.Time, signal.Price, TradeStatus.ClosedBySignal);
                    capital += openTrade.MoneyResult;
                    result.Trades.Add(openTrade);
                    openTrade = null;
                }

                signalIndex++;
            }

            // === 2. Intrabar stop-loss check ===
            if (openTrade != null && _settings.UseStopLoss)
            {
                bool slHit = openTrade.Direction == TradeDirection.Buy
                    ? bar.Low <= openTrade.StopLoss
                    : bar.High >= openTrade.StopLoss;

                if (slHit)
                {
                    CloseTrade(openTrade, bar.Time, openTrade.StopLoss, TradeStatus.ClosedByStopLoss);
                    capital += openTrade.MoneyResult;
                    result.Trades.Add(openTrade);
                    openTrade = null;
                }
            }

            // === 3. Update equity curve ===
            double equity = capital;
            if (openTrade != null)
            {
                double floating = (bar.Close - openTrade.EntryPrice) / _settings.PipSize
                    * (openTrade.Direction == TradeDirection.Buy ? 1 : -1)
                    * openTrade.Lots * _settings.PipValuePerLot;
                equity += floating;
            }
            equityCurve.Add(equity);

            if (equity > peakCapital) peakCapital = equity;
            double dd = peakCapital > 0 ? (peakCapital - equity) / peakCapital * 100 : 0;
            if (dd > maxDrawdown) maxDrawdown = dd;
        }

        // Close any trade left open at the end of the series
        if (openTrade != null)
        {
            var lastBar = bars[^1];
            CloseTrade(openTrade, lastBar.Time, lastBar.Close, TradeStatus.ClosedBySignal);
            capital += openTrade.MoneyResult;
            result.Trades.Add(openTrade);
        }

        // === Aggregate metrics ===
        result.FinalCapital = capital;
        result.TotalTrades = result.Trades.Count;
        result.WinningTrades = result.Trades.Count(t => t.PipsResult > 0);
        result.LosingTrades = result.Trades.Count(t => t.PipsResult <= 0);
        result.TotalPips = result.Trades.Sum(t => t.PipsResult);
        result.TotalMoney = result.Trades.Sum(t => t.MoneyResult);

        var wins = result.Trades.Where(t => t.PipsResult > 0).ToList();
        var losses = result.Trades.Where(t => t.PipsResult <= 0).ToList();
        result.AvgWinPips = wins.Count > 0 ? wins.Average(t => t.PipsResult) : 0;
        result.AvgLossPips = losses.Count > 0 ? losses.Average(t => t.PipsResult) : 0;

        double grossProfit = wins.Sum(t => t.MoneyResult);
        double grossLoss = Math.Abs(losses.Sum(t => t.MoneyResult));
        result.ProfitFactor = grossLoss > 0 ? grossProfit / grossLoss : 0;
        result.MaxDrawdownPercent = maxDrawdown;

        // Simplified annualized Sharpe ratio
        if (result.Trades.Count > 1)
        {
            var returns = result.Trades.Select(t => t.MoneyResult / _settings.InitialCapital).ToList();
            double avg = returns.Average();
            double std = Math.Sqrt(returns.Sum(r => (r - avg) * (r - avg)) / (returns.Count - 1));
            result.SharpeRatio = std > 0 ? avg / std * Math.Sqrt(252) : 0;
        }

        return result;
    }

    private void CloseTrade(Trade trade, DateTime time, double price, TradeStatus status)
    {
        trade.ExitTime = time;
        trade.ExitPrice = price;
        trade.Status = status;

        double pips = (price - trade.EntryPrice) / _settings.PipSize
            * (trade.Direction == TradeDirection.Buy ? 1 : -1);
        trade.MoneyResult = pips * trade.Lots * _settings.PipValuePerLot;
    }

    /// <summary>
    /// Fixed-fractional position sizing: lots = (capital * risk%) / (SL pips * pip value per lot).
    /// </summary>
    private double CalculateLots(double capital, double slPips)
    {
        double riskMoney = capital * _settings.RiskPercent / 100.0;
        double riskPerLot = slPips * _settings.PipValuePerLot;
        if (riskPerLot <= 0) return _settings.MinLots;

        double lots = riskMoney / riskPerLot;
        return Math.Max(_settings.MinLots, Math.Min(_settings.MaxLots, Math.Round(lots, 2)));
    }
}