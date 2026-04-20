using Microsoft.AspNetCore.Mvc;
using CurrencyDeltaApi.Models;

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