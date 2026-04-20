using CurrencyDeltaApi.DataProviders;
using CurrencyDeltaApi.Models;

namespace CurrencyDeltaApi.Services;

public class CurrencyService : ICurrencyService
{
    private readonly IRiksbankenDataProvider _provider;

    public CurrencyService(IRiksbankenDataProvider provider)
    {
        _provider = provider;
    }

    public async Task<List<CurrencyDeltaResponse>> CalculateDelta(CurrencyRequest request)
    {
        Validate(request);

        var results = new List<CurrencyDeltaResponse>();

        foreach (var currency in request.Currencies)
        {
            List<RateDto> rates;

            // Special case: SEK som target (behöver inverteras)
            if (currency == "SEK" && request.Baseline != "SEK")
            {
                rates = await _provider.GetRates("SEK", request.Baseline, request.FromDate, request.ToDate);

                // invertera värden
                rates = rates
                    .Select(r => new RateDto(r.Date, 1 / r.Value))
                    .ToList();
            }
            else
            {
                rates = await _provider.GetRates(request.Baseline, currency, request.FromDate, request.ToDate);
            }

            var fromRate = rates.First().Value;
            var toRate = rates.Last().Value;

            var delta = (toRate / fromRate) - 1;
            delta = Math.Round(delta, 5);

            results.Add(new CurrencyDeltaResponse(currency, delta));
        }

        return results;
    }

    // =======================
    // Validation
    // =======================
    private void Validate(CurrencyRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Baseline))
            throw ErrorFactory.Currency("Baseline currency is required");

        if (req.Currencies == null || req.Currencies.Count == 0)
            throw ErrorFactory.Currency("At least one currency must be provided");

        if (req.Currencies.Distinct().Count() != req.Currencies.Count)
            throw ErrorFactory.Currency("Currencies must be unique");

        if (req.Baseline.Length != 3 || req.Currencies.Any(c => c.Length != 3))
            throw ErrorFactory.Currency("Currencies must be 3-letter ISO codes");

        if (req.Currencies.Contains(req.Baseline))
            throw ErrorFactory.Currency("Currencies must not contain the baseline");

        if (!DateTime.TryParse(req.FromDate, out var from))
            throw ErrorFactory.Date("Invalid fromDate format (yyyy-MM-dd)");

        if (!DateTime.TryParse(req.ToDate, out var to))
            throw ErrorFactory.Date("Invalid toDate format (yyyy-MM-dd)");

        if (to <= from)
            throw ErrorFactory.Date("To date must be greater than from date");

        if (from.Year < 2023)
            throw ErrorFactory.Date("From date cannot be earlier than 2023");
    }
}