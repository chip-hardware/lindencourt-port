[![Build & Test](https://github.com/chip-hardware/lindencourt-port/actions/workflows/build.yml/badge.svg)](https://github.com/chip-hardware/lindencourt-port/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![C#](https://img.shields.io/badge/C%23-12.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![GitHub last commit](https://img.shields.io/github/last-commit/chip-hardware/lindencourt-port)](https://github.com/chip-hardware/lindencourt-port/commits/main)

# Lindencourt FX System — C# Port

C# (.NET 8) port of the Lindencourt FX System indicators and strategy from MQL4.

## Purpose

This is an **educational project** — a case study in porting MQL4 indicators to C#,
building a backtest engine, and validating a well-known retail trading system.

**Not** financial advice. **Not** a trading bot.

## Motivation

I wanted to check the actual performance of the Lindencourt FX System — a system
that is often described as "well-thought-out and logical" in trading circles.

The results were disappointing. Which raises a bigger question:

> If even a carefully designed, multi-condition system like Lindencourt
> produces only modest returns, what does that say about the vast majority
> of retail systems built on 2–3 indicators and moving averages?

This repository documents that experiment.

## Components

### Indicators
- `Ema` — exponential moving average (MQL4-style SMA seeding)
- `Rsi` — RSI(13) with shift and multiplier support
- `Cci` — Commodity Channel Index (standard Lambert)
- `T3` — 6-fold EMA smoothing with b=0.618
- `T3Cci` — T3-smoothed CCI (FX Sniper)
- `FibonacciPivots` — daily Fibonacci pivots, day starts at 17:00 NY

### Strategy
- `LindencourtStrategy` — 4 entry conditions + system exit
  - EMA(7)/EMA(21) cross
  - Close beyond EMA(84)
  - T3 CCI(5, 5) confirmation
  - RSI Histo(13) ±10 filter
  - Time-of-day filter

### Backtest
- `BacktestEngine` — bar-by-bar simulation with SL, position sizing, drawdown, Sharpe
- `BacktestSettings` — initial capital, risk %, SL pips, pip value
- `Trade` — trade model

### Data
- `CsvLoader` — supports MT4 export and forexsb.com formats, auto-detects separator
## Project structure

    Lindencourt.sln
    ├── src/
    │   ├── Lindencourt.Core/          # indicators, models, strategy
    │   ├── Lindencourt.Data/          # CSV loader
    │   ├── Lindencourt.Backtest/      # backtest engine
    │   └── Lindencourt.Console/       # CLI entry point
    └── tests/
        └── Lindencourt.Tests/         # xUnit tests

## Requirements

- .NET 8.0 SDK
- Historical CSV data (MT4 export or forexsb.com)
## Usage

    dotnet build
    dotnet test
    dotnet run --project src/Lindencourt.Console -- data/EURUSD.fx15.csv 7 21 84 336

## Data formats

### MT4 export

    2024.08.21,20:45,1.11529,1.11580,1.11529,1.11553,300

Comma-separated, Date and Time in separate columns.

### forexsb.com

    2024-08-21 20:45:00	1.11529	1.11580	1.11529	1.11553	300

Tab-separated, Time combined.

## Results

### EURUSD M15, InstaForex MT4, 2024-08-21 → 2026-09-18 (2 years)

| Metric          | Without SL | With SL (40 pips) |
|-----------------|------------|-------------------|
| Total trades    | 887        | 887               |
| Win rate        | 38.8%      | 38.8%             |
| Total pips      | +378.5     | +362.7            |
| Return          | +14.38%    | +8.95%            |
| Profit factor   | 1.08       | 1.08              |
| Sharpe ratio    | 0.38       | 0.39              |
| Max drawdown    | 19.96%     | 13.23%            |

### Observations

1. **Modest return.** ~4.5% per year on EURUSD M15 — before spread, commission, and slippage.
2. **Low Sharpe.** 0.39 is well below any professional threshold.
3. **High drawdown.** ~13–20% for a ~9–14% return over two years.
4. **Tiny edge.** Avg win 14 pips vs avg loss 8 pips, win rate 39% → thin margin.
5. **Many trades, small profits.** ~1.2 trades/day, most closing in the −10…+10 pips range.

### Conclusion

The Lindencourt FX System is **not a profitable system** in its published form
on recent EURUSD data. It may have worked in 2009 on GBPUSD under different
market conditions, but the edge does not survive on modern 15M data.

## What this tells us

If a system with:
- 4 simultaneous entry conditions
- Multi-timeframe trend alignment
- Momentum confirmation (CCI)
- Overbought/oversold filter (RSI)
- Time-of-day filter

…still produces a Sharpe below 0.5, then most retail systems built on
2–3 indicators are almost certainly not viable in the long run.

The retail trading education industry rarely publishes honest, multi-year,
out-of-sample statistics. This project is an attempt to do exactly that.

## Limitations

- **No spread, swap, or commission** in the current backtest.
- **Single pair, single timeframe.** Other pairs and TFs were not tested.
- **No partial exits or break-even logic** (described in the PDF but not implemented).
- **No walk-forward analysis** in this repository.
- **One data vendor.** Results may differ on other brokers' feeds.

## Sources

- PDF: "The Lindencourt FX System" © 2009 Lindencourt Foreign Exchange Trading Co, v1.2
- MQL4 indicators: LCSignal_wAlert, LC-FX-Snipers-T3-CCI_Alert, LC-RSI_Histo3,
  LC-Pivots, SDX-TzPivots_v4.9, LC-i-Sessions-pst, LC-b-clock
- Data: forexsb.com (DukasCopy) and MT4 InstaForex

## License

Educational project. Use at your own risk.