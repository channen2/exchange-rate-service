using ExchangeRateService.DTOs.Responses;
using ExchangeRateService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeRateService.Controllers
{
    [ApiController]
    [Route("api/currencies")]
    public class CurrencyController(ICurrencyService currencyService) : ControllerBase
    {
        private readonly ICurrencyService _currencyService = currencyService;

        [HttpGet]
        public ActionResult<IReadOnlyList<SupportedCurrencyResponse>> GetSupported()
        {
            var result = _currencyService.GetSupportedCurrencies();
            return Ok(result);
        }
    }
}
