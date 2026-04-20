namespace CurrencyDeltaApi.DataProviders;

using CurrencyDeltaApi.Models;

public interface IRiksbankenDataProvider
{
    Task<List<RateDto>> GetRates(
        string baseCurrency,
        string targetCurrency,
        string fromDate,
        string toDate);
}