using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using ColetaCerta.Api.Services;
using Xunit;

namespace ColetaCerta.Tests;

public class IntegracaoIaServiceTests
{
    [Fact]
    public async Task ConsultarPrevisaoAsync_RespostaValida_RetornaPrevisao()
    {
        var handler = new RespostaSimulada(new
        {
            regiao = "São Paulo",
            periodoInicio = "2026-11-16",
            periodoFim = "2026-11-22",
            solicitacoesPrevistas = 28,
            modelo = "RandomForest"
        });

        using var httpClient = CriarCliente(handler);
        var service = new IntegracaoIaService(httpClient);

        var resultado = await service.ConsultarPrevisaoAsync(
            "  São Paulo  ", CancellationToken.None);

        Assert.Equal("São Paulo", resultado.Regiao);
        Assert.Equal(28m, resultado.SolicitacoesPrevistas);
        Assert.Equal("RandomForest", resultado.Modelo);
        Assert.Equal(new DateOnly(2026, 11, 16), resultado.PeriodoInicio);
        Assert.Equal(new DateOnly(2026, 11, 22), resultado.PeriodoFim);

        Assert.Equal("/previsao", handler.UriSolicitada?.AbsolutePath);
        Assert.Equal(
            "?regiao=S%C3%A3o%20Paulo",
            handler.UriSolicitada?.Query);
    }

    [Theory]
    [InlineData("Ipiranga", "2026-11-22", "2026-11-16", 28)]
    [InlineData("Ipiranga", "2026-11-16", "2026-11-22", -1)]
    [InlineData("Mooca", "2026-11-16", "2026-11-22", 28)]
    public async Task ConsultarPrevisaoAsync_RespostaInvalida_RejeitaPrevisao(
        string regiaoRecebida,
        string inicio,
        string fim,
        int quantidade)
    {
        var handler = new RespostaSimulada(new
        {
            regiao = regiaoRecebida,
            periodoInicio = inicio,
            periodoFim = fim,
            solicitacoesPrevistas = quantidade,
            modelo = "RandomForest"
        });

        using var httpClient = CriarCliente(handler);
        var service = new IntegracaoIaService(httpClient);

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            service.ConsultarPrevisaoAsync(
                "Ipiranga", CancellationToken.None));
    }

    private static HttpClient CriarCliente(HttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://ia.test/")
        };
    }

    private sealed class RespostaSimulada(object dados) : HttpMessageHandler
    {
        public Uri? UriSolicitada { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            UriSolicitada = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(dados)
            });
        }
    }
}
