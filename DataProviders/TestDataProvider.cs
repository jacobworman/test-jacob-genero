namespace CurrencyDeltaApi.DataProviders;

using CurrencyDeltaApi.Models;

public class TestDataProvider : IRiksbankenDataProvider
{
    public Task<List<RateDto>> GetRates(string baseCurrency, string targetCurrency, string fromDate, string toDate)
    {
        return Task.FromResult(new List<RateDto>
        {
            new RateDto(DateTime.Parse(fromDate), 1.0m),
            new RateDto(DateTime.Parse(toDate), 2.0m)
        });
    }
}