using ExchangeRateService.DTOs.Responses;
using ExchangeRateService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ExchangeRateService.Controllers
{
    [EnableRateLimiting("standard")]
    [ApiController]
    [Route("api/v1/currencies")]
    [Tags("Currencies")]
    public class CurrencyController(ICurrencyService currencyService) : ControllerBase
    {
        private readonly ICurrencyService _currencyService = currencyService;

        /// <summary>
        /// Retrieves currencies supported for conversion.
        /// </summary>
        /// <returns>
        /// A list of supported currency codes and their display names.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(
            typeof(IReadOnlyList<SupportedCurrencyResponse>),
            StatusCodes.Status200OK
        )]
        public ActionResult<IReadOnlyList<SupportedCurrencyResponse>> GetSupported()
        {
            var result = _currencyService.GetSupportedCurrencies();
            return Ok(result);
        }
    }
}
