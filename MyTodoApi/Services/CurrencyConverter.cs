namespace MyTodoApi.Services;

public class CurrencyConverter
{
    private static readonly HashSet<string> AvailableCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD", "EUR", "PLN", "UAH"
    };

    public decimal Convert(decimal amount, decimal rate)
    {
        if (amount < 0)
            throw new ArgumentException("Сума не може бути від'ємною.", nameof(amount));

        if (rate <= 0)
            throw new ArgumentException("Курс валюти має бути строго більшим за 0.", nameof(rate));

        return Math.Round(amount * rate, 2);
    }

    public bool IsCurrencySupported(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        return AvailableCurrencies.Contains(code.Trim());
    }

    public string FormatTransactionReceipt(string fromCode, string toCode, decimal amount, decimal converted)
    {
        if (string.IsNullOrWhiteSpace(fromCode) || string.IsNullOrWhiteSpace(toCode))
            throw new ArgumentException("Коди валют обов'язкові.");

        return $"EXCHANGE: {amount:F2} {fromCode.ToUpper()} -> {converted:F2} {toCode.ToUpper()} [PROCESSED]";
    }

    public HashSet<string> GetSupportedCurrenciesReference() => AvailableCurrencies;
}
