using CurrencyDeltaApi.Models;

public interface ICurrencyService
{
    Task<List<CurrencyDeltaResponse>> CalculateDelta(CurrencyRequest request);
}