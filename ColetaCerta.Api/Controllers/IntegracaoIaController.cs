using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ColetaCerta.Api.DTOs.Integracoes;
using ColetaCerta.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColetaCerta.Api.Controllers;

[ApiController]
[Authorize(Roles = "ADMIN,USER")]
[Route("api/integracoes/ia")]
public class IntegracaoIaController : ControllerBase
{
    private readonly IIntegracaoIaService _service;

    public IntegracaoIaController(
        IIntegracaoIaService service)
    {
        _service = service;
    }

    [HttpGet("previsao")]
    [ProducesResponseType(
        typeof(IaPrevisaoResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<IaPrevisaoResponse>> Consultar(
        [FromQuery, Required, StringLength(100)] string regiao,
        CancellationToken cancellationToken)
    {
        try
        {
            var resposta = await _service.ConsultarPrevisaoAsync(
                regiao,
                cancellationToken);

            return Ok(resposta);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "Tempo de consulta esgotado.",
                detail: "O serviço de IA demorou para responder.");
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode.HasValue)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Falha na resposta da IA.",
                detail: "O serviço de IA retornou um erro HTTP.");
        }
        catch (HttpRequestException)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Serviço de IA indisponível.",
                detail: "Não foi possível conectar ao serviço de IA.");
        }
        catch (JsonException)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Resposta inválida.",
                detail: "O serviço de IA retornou um JSON inválido.");
        }
        catch (InvalidDataException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Resultado inválido.",
                detail: ex.Message);
        }
    }
}