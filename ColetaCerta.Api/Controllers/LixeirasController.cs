using ColetaCerta.Api.DTOs.Lixeiras;
using ColetaCerta.Api.Exceptions;
using ColetaCerta.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
namespace ColetaCerta.Api.Controllers;

[ApiController]
[Route("api/lixeiras")]
public class LixeirasController : ControllerBase
{
    private readonly ILixeiraService _service;

    public LixeirasController(ILixeiraService service)
    {
        _service = service;
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [ProducesResponseType(
        typeof(LixeiraResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LixeiraResponse>> Criar(
        [FromBody] LixeiraRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var resposta = await _service.CriarAsync(
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(ObterPorId),
                new { id = resposta.Id },
                resposta);
        }
        catch (ArgumentException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Dados inválidos.",
                detail: ex.Message);
        }
        catch (ConflitoException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflito no cadastro.",
                detail: ex.Message);
        }
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(List<LixeiraResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LixeiraResponse>>> Listar(
        CancellationToken cancellationToken)
    {
        var resposta = await _service.ListarAsync(
            cancellationToken);

        return Ok(resposta);
    }

    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType(
        typeof(LixeiraResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LixeiraResponse>> ObterPorId(
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
    typeof(ProblemDetails),
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Atualizar(
    int id,
    [FromBody] LixeiraRequest request,
    CancellationToken cancellationToken)
    {
        try
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
        catch (ArgumentException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Dados inválidos.",
                detail: ex.Message);
        }
        catch (ConflitoException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflito na atualização.",
                detail: ex.Message);
        }
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
                title: "Não foi possível excluir a lixeira.",
                detail: ex.Message);
        }
    }
}