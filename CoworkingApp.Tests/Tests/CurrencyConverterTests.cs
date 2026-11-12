namespace CoworkingApp.Tests.Tests;

using MyTodoApi.Services;
using Xunit;

public class CurrencyConverterTests
{
    private readonly CurrencyConverter _converter = new();

    [Fact]
    public void Convert_StandardInput_ValidatesEqualityAndRange()
    {
        // Arrange
        decimal amount = 200m;
        decimal rate = 40m;

        // Act
        decimal result = _converter.Convert(amount, rate);

        // Assert: Equality
        Assert.Equal(8000m, result);
        Assert.NotEqual(0m, result);

        // Assert: Range
        Assert.InRange(result, 1000m, 10000m);
        Assert.NotInRange(result, 0m, 500m);
    }

    [Fact]
    public void References_CheckMemoryAndNullability()
    {
        // Arrange
        var ref1 = _converter.GetSupportedCurrenciesReference();
        var ref2 = _converter.GetSupportedCurrenciesReference();
        string? nullString = null;

        // Assert: References
        Assert.NotNull(ref1);
        Assert.Null(nullString);
        Assert.Same(ref1, ref2);
        Assert.NotSame(ref1, new HashSet<string>());
    }

    [Fact]
    public void FormatTransactionReceipt_ValidData_ValidatesStringRules()
    {
        // Act
        string receipt = _converter.FormatTransactionReceipt("USD", "UAH", 100m, 4150m);

        // Assert: Strings
        Assert.StartsWith("EXCHANGE:", receipt);
        Assert.Contains("USD -> 4150.00 UAH", receipt);
        Assert.Matches(@"^EXCHANGE:\s\d+\.\d{2}\sUSD\s->\s\d+\.\d{2}\sUAH\s\[PROCESSED\]$", receipt);
    }

    // 2. [Theory] + [InlineData] — Booleans & Exceptions
    [Theory]
    [InlineData("USD", true)]
    [InlineData("uah", true)]
    [InlineData("JPY", false)]
    [InlineData("", false)]
    public void IsCurrencySupported_Codes_ReturnsExpectedBoolean(string code, bool expected)
    {
        bool result = _converter.IsCurrencySupported(code);

        if (expected)
            Assert.True(result);
        else
            Assert.False(result);
    }

    [Theory]
    [InlineData(-10, 40)]
    [InlineData(100, 0)]
    [InlineData(100, -5)]
    public void Convert_InvalidArguments_ThrowsArgumentException(decimal amount, decimal rate)
    {
        Assert.Throws<ArgumentException>(() => _converter.Convert(amount, rate));
    }

    // 3. [Theory] + [MemberData]
    public static IEnumerable<object[]> GetMemberTestData()
    {
        yield return new object[] { 10m, 40m, 400m };
        yield return new object[] { 0m, 41.2m, 0m };
    }

    [Theory]
    [MemberData(nameof(GetMemberTestData))]
    public void Convert_MemberDataInput_ReturnsAccurateMultiplication(decimal amount, decimal rate, decimal expected)
    {
        Assert.Equal(expected, _converter.Convert(amount, rate));
    }

    // 4. [Theory] + [ClassData]
    [Theory]
    [ClassData(typeof(CurrencyTestData))]
    public void Convert_ClassDataInput_CalculatesCorrectSum(decimal amount, decimal rate, decimal expected)
    {
        Assert.Equal(expected, _converter.Convert(amount, rate));
    }
}