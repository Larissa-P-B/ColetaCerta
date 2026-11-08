using System.Net.Http.Json;
using ColetaCerta.Api.Data;
using ColetaCerta.Api.DTOs.Integracoes;
using ColetaCerta.Api.Entities.Enums;
using ColetaCerta.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ColetaCerta.Api.Services;

public class IntegracaoIotService : IIntegracaoIotService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;

    public IntegracaoIotService(
        HttpClient httpClient,
        AppDbContext context)
    {
        _httpClient = httpClient;
        _context = context;
    }

    public async Task<LixeiraOcupacaoResponse?> ConsultarAsync(
        int lixeiraId,
        CancellationToken cancellationToken)
    {
        var existe = await _context.Lixeiras
            .AnyAsync(
                l => l.Id == lixeiraId,
                cancellationToken);

        if (!existe)
        {
            return null;
        }

        var leitura = await _httpClient
            .GetFromJsonAsync<WokwiLeituraResponse>(
                "leitura",
                cancellationToken);

        if (leitura is null
            || leitura.LixeiraId != lixeiraId
            || leitura.OcupacaoPercentual is null)
        {
            throw new InvalidDataException(
                "O dispositivo não retornou uma leitura " +
                "válida para a lixeira solicitada.");
        }

        var percentual = leitura.OcupacaoPercentual.Value;

        if (percentual < 0 || percentual > 100)
        {
            throw new InvalidDataException(
                "O percentual recebido deve estar entre 0 e 100.");
        }

        var situacao = percentual switch
        {
            <= 10 => SituacaoOcupacao.Vazia,
            < 80 => SituacaoOcupacao.ParcialmenteCheia,
            < 95 => SituacaoOcupacao.QuaseCheia,
            _ => SituacaoOcupacao.Cheia
        };

        return new LixeiraOcupacaoResponse(
            LixeiraId: lixeiraId,
            OcupacaoPercentual: percentual,
            SituacaoOcupacao: situacao,
            ConsultadoEm: DateTimeOffset.UtcNow,
            Origem: "ESP32_WOKWI");
    }
}