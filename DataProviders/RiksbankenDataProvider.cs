namespace CurrencyDeltaApi.DataProviders;

using System.Globalization;
using System.Text.Json;
using CurrencyDeltaApi.Models;

public class RiksbankenDataProvider : IRiksbankenDataProvider
{
    private readonly HttpClient _http;

    public RiksbankenDataProvider(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<RateDto>> GetRates(
        string baseCurrency,
        string targetCurrency,
        string fromDate,
        string toDate)
    {
        string url;

        if (baseCurrency == "SEK")
        {
            url = $"https://api.riksbank.se/swea/v1/observations/sek{targetCurrency.ToLower()}pmi/{fromDate}/{toDate}";
        }
        else
        {
            url = $"https://api.riksbank.se/swea/v1/CrossRates/sek{baseCurrency.ToLower()}pmi/sek{targetCurrency.ToLower()}pmi/{fromDate}/{toDate}";
        }

        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw ErrorFactory.Currency("Failed to fetch currency data from external API");

        var content = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(content))
            throw ErrorFactory.Currency("No data returned from API");

        var result = new List<RateDto>();

        try
        {
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw ErrorFactory.Currency("Unexpected API response format");

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.TryGetProperty("value", out var val) &&
                    item.TryGetProperty("date", out var date))
                {
                    decimal parsedValue;

                    if (val.ValueKind == JsonValueKind.Number)
                    {
                        parsedValue = val.GetDecimal();
                    }
                    else if (val.ValueKind == JsonValueKind.String)
                    {
                        decimal.TryParse(
                            val.GetString(),
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out parsedValue);
                    }
                    else
                    {
                        continue;
                    }

                    var parsedDate = DateTime.Parse(date.GetString()!);

                    result.Add(new RateDto(parsedDate, parsedValue));
                }
            }
        }
        catch
        {
            throw ErrorFactory.Currency("Invalid data format from API");
        }

        if (result.Count == 0)
            throw ErrorFactory.Currency("No valid data found");

        return result.OrderBy(r => r.Date).ToList();
    }
}