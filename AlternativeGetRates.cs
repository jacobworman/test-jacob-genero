private async Task<(decimal fromRate, decimal toRate)> GetRates(
    string baseCurrency,
    string target,
    string fromDate,
    string toDate)
{
    string url;

    if (baseCurrency == "SEK")
    {
        url = $"https://api.riksbank.se/swea/v1/observations/sek{target.ToLower()}pmi/{fromDate}/{toDate}";
    }
    else
    {
        url = $"https://api.riksbank.se/swea/v1/CrossRates/sek{baseCurrency.ToLower()}pmi/sek{target.ToLower()}pmi/{fromDate}/{toDate}";
    }

    var response = await _http.GetAsync(url);

    if (!response.IsSuccessStatusCode)
        throw ErrorFactory.Currency("Failed to fetch currency data from external API");

    var content = await response.Content.ReadAsStringAsync();

    if (string.IsNullOrWhiteSpace(content))
        throw ErrorFactory.Currency("No data returned for given currency and date range");

    List<RateDto> rates;

    try
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        rates = JsonSerializer.Deserialize<List<RateDto>>(content, options)
                 ?? new List<RateDto>();
    }
    catch
    {
        throw ErrorFactory.Currency("Invalid data format from API");
    }

    if (rates.Count == 0)
        throw ErrorFactory.Currency("Currency does not exist or has no data for given dates");

    var ordered = rates
        .Where(r => r.Value != 0) // valfri safeguard
        .OrderBy(r => r.Date)
        .ToList();

    if (ordered.Count == 0)
        throw ErrorFactory.Currency("No valid rate values found");

    return (ordered.First().Value, ordered.Last().Value);
}