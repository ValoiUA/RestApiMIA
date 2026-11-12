namespace MyTodoApi.Services;

public class ExchangeCommissionCalculator
{
    private const decimal StandardFeeRate = 0.015m;
    private const decimal VipFeeRate = 0.005m;

    public decimal CalculateFee(decimal amount, bool isVip)
    {
        if (amount <= 0)
            throw new ArgumentException("Сума має бути більшою за нуль.", nameof(amount));

        decimal rate = isVip ? VipFeeRate : StandardFeeRate;
        return Math.Round(amount * rate, 2);
    }
}