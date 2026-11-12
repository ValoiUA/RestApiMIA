namespace CoworkingApp.Tests.Tests;

using System.Collections;
using System.Collections.Generic;

public class CurrencyTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { 100m, 41.5m, 4150.0m };
        yield return new object[] { 50m, 1.08m, 54.0m };
        yield return new object[] { 250m, 0.25m, 62.5m };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}