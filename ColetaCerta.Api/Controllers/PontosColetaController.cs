using ColetaCerta.Api.DTOs.PontosColeta;
using ColetaCerta.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
namespace ColetaCerta.Api.Controllers;

[ApiController]
[Route("api/pontos-coleta")]
public class PontosColetaController : ControllerBase
{
    private readonly IPontoColetaService _service;

    public PontosColetaController(IPontoColetaService service)
    {
        _service = service;
    }
    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [ProducesResponseType(
        typeof(PontoColetaResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PontoColetaResponse>> Criar(
        [FromBody] PontoColetaRequest request,
        CancellationToken cancellationToken)
    {
        var resposta = await _service.CriarAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(ObterPorId),
            new { id = resposta.Id },
            resposta);
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(List<PontoColetaResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PontoColetaResponse>>> Listar(
        CancellationToken cancellationToken)
    {
        var resposta = await _service.ListarAsync(
            cancellationToken);

        return Ok(resposta);
    }

    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType(
        typeof(PontoColetaResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PontoColetaResponse>> ObterPorId(
        int id,
        CancellationToken cancellationToken)
    {
        var resposta = await _service.ObterPorIdAsync(
            id,
            cancellationToken);

        if (resposta is null)
        {
            return NotFound();
        }

        return Ok(resposta);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id:int:min(1)}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(
    typeof(ValidationProblemDetails),
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(
    int id,
    [FromBody] PontoColetaRequest request,
    CancellationToken cancellationToken)
    {
        var atualizado = await _service.AtualizarAsync(
            id,
            request,
            cancellationToken);

        if (!atualizado)
        {
            return NotFound();
        }

        return NoContent();
    }

    [Authorize(Roles = "ADMIN")]
    [HttpDelete("{id:int:min(1)}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Excluir(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var excluido = await _service.ExcluirAsync(
                id,
                cancellationToken);

            if (!excluido)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (ConflitoException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Não foi possível excluir o ponto de coleta.",
                detail: ex.Message);
        }
    }
}