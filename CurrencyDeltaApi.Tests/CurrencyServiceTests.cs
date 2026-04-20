using Xunit;
using CurrencyDeltaApi.Services;
using CurrencyDeltaApi.DataProviders;
using CurrencyDeltaApi.Models;

public class CurrencyServiceTests
{

    [Fact]
    public async Task Should_Throw_When_ToDate_Is_Before_FromDate()
    {
        var provider = new TestDataProvider();
        var _service = new CurrencyService(provider);
        var request = new CurrencyRequest(
            "GBP",
            new List<string> { "USD" },
            "2025-01-10",
            "2025-01-01"
        );

        await Assert.ThrowsAsync<ApiException>(() =>
            _service.CalculateDelta(request));
    }

    [Fact]
    public async Task Should_Throw_When_Currencies_Not_Unique()
    {
        var provider = new TestDataProvider();
        var _service = new CurrencyService(provider);
        var request = new CurrencyRequest(
            "GBP",
            new List<string> { "USD", "USD" },
            "2025-01-01",
            "2025-01-10"
        );

        await Assert.ThrowsAsync<ApiException>(() =>
            _service.CalculateDelta(request));
    }

    [Fact]
    public async Task Should_Throw_When_Currency_Equals_Baseline()
    {
        var provider = new TestDataProvider();
        var _service = new CurrencyService(provider);
        var request = new CurrencyRequest(
            "GBP",
            new List<string> { "GBP" },
            "2025-01-01",
            "2025-01-10"
        );

        await Assert.ThrowsAsync<ApiException>(() =>
            _service.CalculateDelta(request));
    }
}