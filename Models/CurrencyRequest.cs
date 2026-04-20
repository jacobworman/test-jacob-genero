namespace CurrencyDeltaApi.Models;

public record CurrencyRequest(
    string Baseline,
    List<string> Currencies,
    string FromDate,
    string ToDate
);