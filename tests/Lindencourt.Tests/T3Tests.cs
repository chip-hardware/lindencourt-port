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
    public void Calculate_FirstValue_IsSmoothedStart()
    {
        var source = new double[] { 100, 100, 100, 100, 100 };
        var t3 = T3.Calculate(source, period: 4, b: 0.618);

        // First value is not exactly 100 (because e1..e6 start at 0)
        Assert.NotEqual(100.0, t3[0]);
        // But after a few bars it converges to 100
        Assert.Equal(100.0, t3[4], precision: 3);
    }
}