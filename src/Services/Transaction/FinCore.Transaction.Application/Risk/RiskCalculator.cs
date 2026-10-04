namespace FinCore.Transaction.Application.Risk;

public record RiskContext(decimal Amount, int RecentTransferCount, bool IsNewReceiver);

public record RiskResult(int Score, List<string> Reasons)
{
    public bool IsSuspicious => Score >= RiskCalculator.SuspiciousThreshold;
}

public static class RiskCalculator
{
    public const int SuspiciousThreshold = 60;
    public const int VelocityWindowMinutes = 10;

    private const decimal HighAmountLimit = 7_500m;
    private const decimal ElevatedAmountLimit = 2_500m;
    private const int RapidTransferCount = 3;

    public static RiskResult Calculate(RiskContext context)
    {
        var score = 0;
        var reasons = new List<string>();

        if (context.Amount >= HighAmountLimit)
        {
            score += 40;
            reasons.Add("HighAmount");
        }
        else if (context.Amount >= ElevatedAmountLimit)
        {
            score += 20;
            reasons.Add("ElevatedAmount");
        }

        if (context.RecentTransferCount >= RapidTransferCount)
        {
            score += 30;
            reasons.Add("RapidTransfers");
        }

        if (context.IsNewReceiver)
        {
            score += 20;
            reasons.Add("NewReceiver");
        }

        return new RiskResult(Math.Min(score, 100), reasons);
    }
}