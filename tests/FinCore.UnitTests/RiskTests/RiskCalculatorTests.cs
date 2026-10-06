using FinCore.Transaction.Application.Risk;
using FluentAssertions;

namespace FinCore.UnitTests.RiskTests;

public class RiskCalculatorTests
{
    [Theory]
    [InlineData(100, 0, false, 0)]       // nothing unusual
    [InlineData(2499, 0, false, 0)]      // just below the elevated limit
    [InlineData(2500, 0, false, 20)]     // elevated amount
    [InlineData(7499, 0, false, 20)]     // just below the high limit
    [InlineData(7500, 0, false, 40)]     // high amount
    [InlineData(100, 2, false, 0)]       // two recent transfers are fine
    [InlineData(100, 3, false, 30)]      // rapid transfers
    [InlineData(100, 0, true, 20)]       // new receiver
    [InlineData(7500, 0, true, 60)]      // high amount + new receiver
    [InlineData(7500, 3, true, 90)]      // all three rules
    public void Calculate_ReturnsTheExpectedScore(int amount, int recentTransfers, bool isNewReceiver, int expectedScore)
    {
        var result = RiskCalculator.Calculate(new RiskContext(amount, recentTransfers, isNewReceiver));

        result.Score.Should().Be(expectedScore);
    }

    [Theory]
    [InlineData(7500, 0, true, true)]    // 60
    [InlineData(2500, 3, true, true)]    // 70
    [InlineData(7500, 0, false, false)]  // 40
    [InlineData(100, 3, true, false)]    // 50
    public void IsSuspicious_IsTrueOnlyFromScore60(int amount, int recentTransfers, bool isNewReceiver, bool expected)
    {
        var result = RiskCalculator.Calculate(new RiskContext(amount, recentTransfers, isNewReceiver));

        result.IsSuspicious.Should().Be(expected);
    }

    [Fact]
    public void Calculate_ListsEveryReasonThatApplies()
    {
        var result = RiskCalculator.Calculate(new RiskContext(8000m, 5, true));

        result.Reasons.Should().BeEquivalentTo(new[] { "HighAmount", "RapidTransfers", "NewReceiver" });
    }

    [Fact]
    public void Calculate_DoesNotCountElevatedAmountTwice_WhenAmountIsHigh()
    {
        var result = RiskCalculator.Calculate(new RiskContext(8000m, 0, false));

        result.Reasons.Should().ContainSingle().Which.Should().Be("HighAmount");
    }
}