namespace Lindencourt.Core.Models;

/// <summary>
/// Instrument metadata — needed for pip conversion, SL/TP sizing, and commission.
/// </summary>
public sealed record SymbolInfo(
    string Symbol,
    int Digits,
    double Point,
    double TickSize,
    double TickValue,
    double ContractSize,
    double Spread = 0.0,
    double SwapLong = 0.0,
    double SwapShort = 0.0,
    double Commission = 0.0)
{
    /// <summary>Pip size in price units (10 × Point for 5-digit, 100 × Point for 3-digit).</summary>
    public double PipSize => Digits == 5 || Digits == 3 ? Point * 10 : Point;

    /// <summary>Convert pips to price.</summary>
    public double PipsToPrice(double pips) => pips * PipSize;

    /// <summary>Convert price to pips.</summary>
    public double PriceToPips(double price) => price / PipSize;

    /// <summary>Value of 1 pip for 1 lot in deposit currency.</summary>
    public double PipValue => PipSize / TickSize * TickValue;
}