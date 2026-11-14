using System.Net.Http.Json;
using ColetaCerta.Api.DTOs.Integracoes;
using ColetaCerta.Api.Interfaces;

namespace ColetaCerta.Api.Services;

public class IntegracaoIaService : IIntegracaoIaService
{
    private readonly HttpClient _httpClient;

    public IntegracaoIaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IaPrevisaoResponse> ConsultarPrevisaoAsync(
        string regiao,
        CancellationToken cancellationToken)
    {
        var regiaoNormalizada = regiao.Trim();

        var caminho =
            $"previsao?regiao={Uri.EscapeDataString(regiaoNormalizada)}";

        var resposta = await _httpClient
            .GetFromJsonAsync<IaPrevisaoExternaResponse>(
                caminho,
                cancellationToken);

        if (resposta is null
            || string.IsNullOrWhiteSpace(resposta.Regiao)
            || string.IsNullOrWhiteSpace(resposta.Modelo)
            || resposta.PeriodoInicio is null
            || resposta.PeriodoFim is null
            || resposta.SolicitacoesPrevistas is null)
        {
            throw new InvalidDataException(
                "O serviço de IA retornou informações incompletas.");
        }

        if (!string.Equals(
            resposta.Regiao.Trim(),
            regiaoNormalizada,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "A previsão recebida pertence a outra região.");
        }

        var inicio = resposta.PeriodoInicio.Value;
        var fim = resposta.PeriodoFim.Value;
        var quantidade = resposta.SolicitacoesPrevistas.Value;

        if (fim < inicio)
        {
            throw new InvalidDataException(
                "O período da previsão é inválido.");
        }

        if (quantidade < 0)
        {
            throw new InvalidDataException(
                "A previsão não pode ser negativa.");
        }

        return new IaPrevisaoResponse(
            Regiao: resposta.Regiao.Trim(),
            PeriodoInicio: inicio,
            PeriodoFim: fim,
            SolicitacoesPrevistas: quantidade,
            Modelo: resposta.Modelo.Trim(),
            ConsultadoEm: DateTimeOffset.UtcNow);
    }
}