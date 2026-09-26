using Lindencourt.Core.Indicators;
using Lindencourt.Core.Models;

namespace Lindencourt.Tests;

public class EmaTests
{
    [Fact]
    public void Calculate_WithKnownValues_ReturnsExpected()
    {
        // Simple test: 5 bars, period=3
        var bars = new List<Bar>
        {
            new(new DateTime(2024, 1, 1), 1, 1, 1, 10, 0),
            new(new DateTime(2024, 1, 2), 1, 1, 1, 11, 0),
            new(new DateTime(2024, 1, 3), 1, 1, 1, 12, 0),
            new(new DateTime(2024, 1, 4), 1, 1, 1, 13, 0),
            new(new DateTime(2024, 1, 5), 1, 1, 1, 14, 0),
        };

        var ema = Ema.Calculate(bars, 3);

        // SMA(3) over first 3 bars: (10+11+12)/3 = 11
        Assert.Equal(11.0, ema[2], precision: 10);

        // alpha = 2/(3+1) = 0.5
        // EMA[3] = 0.5*13 + 0.5*11 = 12
        Assert.Equal(12.0, ema[3], precision: 10);

        // EMA[4] = 0.5*14 + 0.5*12 = 13
        Assert.Equal(13.0, ema[4], precision: 10);
    }

    [Fact]
    public void Calculate_WithInsufficientBars_ReturnsNaN()
    {
        var bars = new List<Bar>
        {
            new(new DateTime(2024, 1, 1), 1, 1, 1, 10, 0),
            new(new DateTime(2024, 1, 2), 1, 1, 1, 11, 0),
        };

        var ema = Ema.Calculate(bars, 5);

        Assert.All(ema, v => Assert.True(double.IsNaN(v)));
    }

    [Fact]
    public void Calculate_FirstPeriodMinus1_AreNaN()
    {
        var bars = Enumerable.Range(0, 10)
            .Select(i => new Bar(new DateTime(2024, 1, 1).AddDays(i), 1, 1, 1, 10 + i, 0))
            .ToList();

        var ema = Ema.Calculate(bars, 5);

        for (int i = 0; i < 4; i++)
            Assert.True(double.IsNaN(ema[i]), $"ema[{i}] should be NaN");
        Assert.False(double.IsNaN(ema[4]));
    }
}