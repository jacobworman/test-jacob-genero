using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<ICurrencyService, CurrencyService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

// =======================
// Models
// =======================
public record CurrencyRequest(
    string Baseline,
    List<string> Currencies,
    string FromDate,
    string ToDate
);

public record CurrencyDeltaResponse(string Currency, decimal Delta);

public record ErrorResponse(string ErrorCode, string ErrorDetails);

// =======================
// Controller
// =======================
[ApiController]
[Route("currencydelta")]
public class CurrencyController : ControllerBase
{
    private readonly ICurrencyService _service;

    public CurrencyController(ICurrencyService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CurrencyRequest request)
    {
        try
        {
            var result = await _service.CalculateDelta(request);
            return Ok(result);
        }
        catch (ApiException ex)
        {
            return BadRequest(new ErrorResponse(ex.Code, ex.Message));
        }
    }
}

// =======================
// Service Interface
// =======================
public interface ICurrencyService
{
    Task<List<CurrencyDeltaResponse>> CalculateDelta(CurrencyRequest request);
}

// =======================
// Error Factory (NEW)
// =======================
public static class ErrorFactory
{
    public static ApiException Date(string message)
        => new ApiException("dateproblem", message);

    public static ApiException Currency(string message)
        => new ApiException("currencyproblem", message);
}

// =======================
// Service Implementation
// =======================
public class CurrencyService : ICurrencyService
{
    private readonly HttpClient _http;

    public CurrencyService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<CurrencyDeltaResponse>> CalculateDelta(CurrencyRequest request)
    {
        Validate(request);

        var results = new List<CurrencyDeltaResponse>();

        foreach (var currency in request.Currencies)
        {
            decimal fromRate, toRate;

            // ✅ Special case SEK
            if (currency == "SEK" && request.Baseline != "SEK")
            {
                var rates = await GetRates("SEK", request.Baseline, request.FromDate, request.ToDate);

                fromRate = 1 / rates.fromRate;
                toRate = 1 / rates.toRate;
            }
            else
            {
                var rates = await GetRates(request.Baseline, currency, request.FromDate, request.ToDate);
                fromRate = rates.fromRate;
                toRate = rates.toRate;
            }

            var delta = (toRate / fromRate) - 1;
            delta = Math.Round(delta, 5);

            results.Add(new CurrencyDeltaResponse(currency, delta));
        }

        return results;
    }

    // =======================
    // Validation (IMPROVED)
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

    // =======================
    // API CALL
    // =======================
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

        List<decimal> values = new();

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(content);

            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                throw ErrorFactory.Currency("Unexpected API response format");

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.TryGetProperty("value", out var val))
                {
                    decimal parsed;

                    if (val.ValueKind == System.Text.Json.JsonValueKind.Number)
                    {
                        parsed = val.GetDecimal();
                    }
                    else if (val.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        decimal.TryParse(
                            val.GetString(),
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out parsed);
                    }
                    else
                    {
                        continue;
                    }

                    values.Add(parsed);
                }
            }
        }
        catch
        {
            throw ErrorFactory.Currency("Invalid data format from API");
        }

        if (values.Count == 0)
            throw ErrorFactory.Currency("Currency does not exist or has no data for given dates");

        return (values.First(), values.Last());
    }
}

// =======================
// Custom Exception
// =======================
public class ApiException : Exception
{
    public string Code { get; }

    public ApiException(string code, string message) : base(message)
    {
        Code = code;
    }
}