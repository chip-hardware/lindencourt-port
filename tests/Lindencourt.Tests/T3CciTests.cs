using Lindencourt.Core.Indicators;
using Lindencourt.Core.Models;

namespace Lindencourt.Tests;

public class T3CciTests
{
    [Fact]
    public void Calculate_ReturnsArrayOfSameLength()
    {
        var bars = Enumerable.Range(0, 200)
            .Select(i => new Bar(
                new DateTime(2024, 1, 1).AddMinutes(i * 15),
                1.1000 + i * 0.0001,
                1.1005 + i * 0.0001,
                1.0995 + i * 0.0001,
                1.1002 + i * 0.0001,
                100))
            .ToList();

        var t3cci = T3Cci.Calculate(bars);
        Assert.Equal(bars.Count, t3cci.Length);
    }

    [Fact]
    public void Calculate_IsSmootherThanRawCci()
    {
        // Generate "noisy" bars
        var rnd = new Random(42);
        var bars = new List<Bar>();
        double price = 1.1000;
        for (int i = 0; i < 500; i++)
        {
            price += (rnd.NextDouble() - 0.5) * 0.002;
            bars.Add(new Bar(
                new DateTime(2024, 1, 1).AddMinutes(i * 15),
                price, price + 0.0005, price - 0.0005, price, 100));
        }

        var raw = Cci.Calculate(bars, 5);
        var t3 = T3Cci.Calculate(bars, 5, 4, 0.618);

        // T3 should be smoother — smaller sum of absolute changes
        double rawDiff = 0, t3Diff = 0;
        for (int i = 20; i < bars.Count; i++)
        {
            rawDiff += Math.Abs(raw[i] - raw[i - 1]);
            t3Diff  += Math.Abs(t3[i]  - t3[i - 1]);
        }

        Assert.True(t3Diff < rawDiff,
            $"T3 should be smoother: raw diff={rawDiff:F2}, t3 diff={t3Diff:F2}");
    }
}