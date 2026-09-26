using Lindencourt.Core.Indicators;

namespace Lindencourt.Tests;

public class T3Tests
{
    [Fact]
    public void Calculate_WithConstantInput_ReturnsSameConstant()
    {
        // Constant input → T3 output is also constant (all EMAs converge)
        var source = Enumerable.Repeat(100.0, 100).ToArray();
        var t3 = T3.Calculate(source, period: 4, b: 0.618);

        // After warm-up all values should be ≈ 100
        for (int i = 50; i < 100; i++)
            Assert.Equal(100.0, t3[i], precision: 6);
    }

    [Fact]
    public void Calculate_ConvergesToConstantInput()
    {
        // T3 is a 6-fold EMA — it needs many bars to converge to a constant input.
        var source = Enumerable.Repeat(100.0, 100).ToArray();
        var t3 = T3.Calculate(source, period: 4, b: 0.618);

        // First value is not exactly 100 (e1..e6 start at 0)
        Assert.NotEqual(100.0, t3[0]);

        // After ~30 bars it should be very close to 100
        for (int i = 30; i < 100; i++)
            Assert.Equal(100.0, t3[i], precision: 3);
    }
}