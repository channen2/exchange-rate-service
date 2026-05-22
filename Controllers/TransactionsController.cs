using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok("API is working");
    }
}