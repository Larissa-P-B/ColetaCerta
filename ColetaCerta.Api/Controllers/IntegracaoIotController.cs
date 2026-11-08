using System.Text.Json;
using ColetaCerta.Api.DTOs.Integracoes;
using ColetaCerta.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColetaCerta.Api.Controllers;

[ApiController]
[Authorize(Roles = "ADMIN,USER")]
[Route("api/integracoes/iot")]
public class IntegracaoIotController : ControllerBase
{
    private readonly IIntegracaoIotService _service;

    public IntegracaoIotController(
        IIntegracaoIotService service)
    {
        _service = service;
    }

    [HttpGet("lixeiras/{id:int:min(1)}")]
    [ProducesResponseType(
        typeof(LixeiraOcupacaoResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<LixeiraOcupacaoResponse>> Consultar(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var resposta = await _service.ConsultarAsync(
                id,
                cancellationToken);

            if (resposta is null)
            {
                return NotFound(new
                {
                    mensagem = "Lixeira não encontrada."
                });
            }

            return Ok(resposta);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "Tempo de consulta esgotado.",
                detail: "O dispositivo demorou para responder.");
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode.HasValue)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Falha na resposta do dispositivo.",
                detail: "O dispositivo retornou um erro HTTP.");
        }
        catch (HttpRequestException)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Dispositivo indisponível.",
                detail: "Não foi possível conectar ao ESP32.");
        }
        catch (JsonException)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Resposta inválida.",
                detail: "O dispositivo retornou um JSON inválido.");
        }
        catch (InvalidDataException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Leitura inválida.",
                detail: ex.Message);
        }
    }
}