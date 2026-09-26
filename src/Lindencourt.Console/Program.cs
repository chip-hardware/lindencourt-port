using Lindencourt.Backtest;
using Lindencourt.Core.Indicators;
using Lindencourt.Core.Models;
using Lindencourt.Core.Strategy;
using Lindencourt.Data;

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (args.Length == 0)
{
    Console.WriteLine("Usage: Lindencourt.Console <path-to-csv> [ema-periods...]");
    Console.WriteLine("Example: Lindencourt.Console EURUSD15.csv 7 21 84 336");
    return;
}

var path = args[0];
var periods = args.Length > 1
    ? args.Skip(1).Select(int.Parse).ToArray()
    : new[] { 7, 21, 84, 336 };

Console.WriteLine($"Loading: {path}");
var bars = CsvLoader.Load(path);
Console.WriteLine($"Loaded {bars.Count} bars");
Console.WriteLine($"Range: {bars[0].Time:yyyy-MM-dd HH:mm} — {bars[^1].Time:yyyy-MM-dd HH:mm}");
Console.WriteLine();

// === EMA ===
var emas = new Dictionary<int, double[]>();
foreach (var period in periods)
{
    emas[period] = Ema.Calculate(bars, period);
    Console.WriteLine($"EMA({period}) calculated");
}

// === RSI ===
var rsiRaw   = Rsi.Calculate(bars, period: 13, shift: 50.0, multiplier: 1.0);
var rsiHisto = Rsi.Calculate(bars, period: 13, shift: 50.0, multiplier: 1.5);
Console.WriteLine("RSI(13) calculated");

// === CCI ===
var cci5 = Cci.Calculate(bars, period: 5);
var cci8 = Cci.Calculate(bars, period: 8);
Console.WriteLine("CCI(5) and CCI(8) calculated");

// === T3 CCI ===
var t3cci = T3Cci.Calculate(bars, cciPeriod: 5, t3Period: 5, b: 0.618);
Console.WriteLine("T3 CCI calculated");

// === Fibonacci Pivots ===
var pivots = FibonacciPivots.CalculateDaily(bars, newYorkOffsetHours: -5);
Console.WriteLine($"Fibonacci Pivots calculated for {pivots.Count} trading days");
Console.WriteLine();

// === Print last 3 trading days' pivots ===
var lastPivotDays = pivots.Keys.OrderByDescending(d => d).Take(3).ToList();
Console.WriteLine("Last 3 trading days' pivots:");
foreach (var day in lastPivotDays)
{
    var pl = pivots[day];
    Console.WriteLine($"  {day:yyyy-MM-dd} (start):");
    Console.WriteLine($"    P={pl.Pivot:F5}  R1={pl.R1:F5}  R2={pl.R2:F5}  R3={pl.R3:F5}");
    Console.WriteLine($"    MR1={pl.MR1:F5}  MR2={pl.MR2:F5}  MR3={pl.MR3:F5}");
    Console.WriteLine($"    S1={pl.S1:F5}  S2={pl.S2:F5}  S3={pl.S3:F5}");
}
Console.WriteLine();

// === Print last 10 bars with indicators ===
Console.WriteLine("Last 10 bars:");
var header = $"{"Time",-20} {"Close",10} " +
             string.Join(" ", periods.Select(p => $"EMA({p}),10")) +
             " RSI-50 RSIHisto CCI(5) CCI(8) T3CCI";
Console.WriteLine(header);
Console.WriteLine(new string('-', header.Length));

for (int i = Math.Max(0, bars.Count - 10); i < bars.Count; i++)
{
    var line = $"{bars[i].Time,-20:yyyy-MM-dd HH:mm} {bars[i].Close,10:F5} ";

    foreach (var period in periods)
    {
        var v = emas[period][i];
        line += double.IsNaN(v) ? $"{"—",10} " : $"{v,10:F5} ";
    }

    line += $" {rsiRaw[i],7:F2} {rsiHisto[i],9:F2} {cci5[i],7:F2} {cci8[i],7:F2} {t3cci[i],7:F2}";
    Console.WriteLine(line);
}

// === Nearest pivot level ===
Console.WriteLine();
Console.WriteLine("Nearest pivot level to last close:");
var lastBar = bars[^1];
var lastPivot = FibonacciPivots.ForBar(lastBar, pivots, newYorkOffsetHours: -5);
if (lastPivot != null)
{
    var (name, level, dist) = lastPivot.FindNearest(lastBar.Close);
    Console.WriteLine($"  Close={lastBar.Close:F5}");
    Console.WriteLine($"  Nearest: {name} = {level:F5} (distance: {dist:F5} = {dist / 0.0001:F1} pips)");
}
else
{
    Console.WriteLine("  No pivot found for last bar");
}

// === Strategy ===
Console.WriteLine();
Console.WriteLine("=== Lindencourt Strategy ===");
var strategy = new LindencourtStrategy();
var signals = strategy.Run(bars);
Console.WriteLine($"Total signals: {signals.Count}");

int buys  = signals.Count(s => s.Type == SignalType.Buy);
int sells = signals.Count(s => s.Type == SignalType.Sell);
int exitsBuy  = signals.Count(s => s.Type == SignalType.ExitBuy);
int exitsSell = signals.Count(s => s.Type == SignalType.ExitSell);

Console.WriteLine($"  BUY entries:  {buys}");
Console.WriteLine($"  SELL entries: {sells}");
Console.WriteLine($"  BUY exits:    {exitsBuy}");
Console.WriteLine($"  SELL exits:   {exitsSell}");
Console.WriteLine();

// === Backtest (with SL) ===
Console.WriteLine("=== Backtest WITH SL ===");

var symbol = new SymbolInfo(
    Symbol: "EURUSD",
    Digits: 5,
    Point: 0.00001,
    TickSize: 0.00001,
    TickValue: 1.0,
    ContractSize: 100_000,
    Spread: 0.0001);

var backtestSettings = new BacktestSettings
{
    InitialCapital = 10_000,
    RiskPercent = 1.0,
    StopLossPips = 40,
    PipSize = 0.0001,
    PipValuePerLot = 10.0,
    UseStopLoss = true,
    MaxConcurrentTrades = 1,
};

var engine = new BacktestEngine(backtestSettings, symbol);
var backtestResult = engine.Run(bars, strategy);
backtestResult.Print();

// === Backtest (without SL, reference) ===
Console.WriteLine();
Console.WriteLine("=== Backtest WITHOUT SL (reference) ===");

var noSlSettings = new BacktestSettings
{
    InitialCapital = 10_000,
    RiskPercent = 1.0,
    StopLossPips = 25,
    PipSize = 0.0001,
    PipValuePerLot = 10.0,
    UseStopLoss = false,
    MaxConcurrentTrades = 1,
};

var engineNoSl = new BacktestEngine(noSlSettings, symbol);
var resultNoSl = engineNoSl.Run(bars, strategy);
resultNoSl.Print();

// === Last 20 trades ===
Console.WriteLine();
Console.WriteLine("=== Last 20 trades (with SL) ===");
Console.WriteLine($"{"#",4} {"Dir",-5} {"Entry",-20} {"Entry Price",11} {"Exit",-20} {"Exit Price",11} {"Pips",7} {"Dur",6} {"Status",-18}");
foreach (var t in backtestResult.Trades.TakeLast(20))
{
    Console.WriteLine($"{t.Id,4} {t.Direction,-5} {t.EntryTime,-20:yyyy-MM-dd HH:mm} {t.EntryPrice,11:F5} " +
                      $"{t.ExitTime,-20:yyyy-MM-dd HH:mm} {t.ExitPrice ?? 0,11:F5} {t.PipsResult,7:F1} " +
                      $"{t.Duration.TotalMinutes,6:F0} {t.Status,-18}");
}