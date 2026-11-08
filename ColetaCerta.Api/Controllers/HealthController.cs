using ColetaCerta.Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace ColetaCerta.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _context;

    public HealthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("banco")]
    public async Task<IActionResult> VerificarBanco(
        CancellationToken cancellationToken)
    {
        var conectado = await _context.Database
            .CanConnectAsync(cancellationToken);

        if (!conectado)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { status = "indisponivel" });
        }

        return Ok(new { status = "conectado" });
    }
}