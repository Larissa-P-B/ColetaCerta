using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using ColetaCerta.Api.Controllers;
using ColetaCerta.Api.Data;
using ColetaCerta.Api.DTOs.Integracoes;
using ColetaCerta.Api.Entities;
using ColetaCerta.Api.Entities.Enums;
using ColetaCerta.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ColetaCerta.Tests;

public class IntegracaoIotControllerTests
{
    [Theory]
    [InlineData(true, true, 200, 1)]
    [InlineData(true, false, 502, 1)]
    [InlineData(false, true, 404, 0)]
    public async Task Consultar_ValidaCadastroEIdentidadeDaLeitura(
        bool lixeiraExiste,
        bool leituraCorresponde,
        int statusEsperado,
        int chamadasEsperadas)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var ponto = new PontoColeta(
            "Ponto de teste", "Rua Exemplo, 100", "Ipiranga", "São Paulo");

        context.PontosColeta.Add(ponto);
        await context.SaveChangesAsync();

        var lixeira = new Lixeira("LIX-TESTE", 120, ponto.Id);
        context.Lixeiras.Add(lixeira);
        await context.SaveChangesAsync();

        var idSolicitado = lixeiraExiste ? lixeira.Id : lixeira.Id + 1;
        var idRecebido = leituraCorresponde ? idSolicitado : idSolicitado + 1;

        var dispositivo = new Esp32Simulado(idRecebido);
        using var httpClient = new HttpClient(dispositivo)
        {
            BaseAddress = new Uri("http://esp32.test/")
        };

        var service = new IntegracaoIotService(httpClient, context);
        var controller = new IntegracaoIotController(service);

        var resposta = await controller.Consultar(
            idSolicitado, CancellationToken.None);

        var resultado = Assert.IsAssignableFrom<ObjectResult>(resposta.Result);
        Assert.Equal<int?>(statusEsperado, resultado.StatusCode);
        Assert.Equal(chamadasEsperadas, dispositivo.TotalChamadas);

        if (statusEsperado == 200)
        {
            var leitura = Assert.IsType<LixeiraOcupacaoResponse>(resultado.Value);
            Assert.Equal(idSolicitado, leitura.LixeiraId);
            Assert.Equal(85m, leitura.OcupacaoPercentual);
            Assert.Equal(SituacaoOcupacao.QuaseCheia, leitura.SituacaoOcupacao);
        }
    }

    private sealed class Esp32Simulado(int lixeiraId) : HttpMessageHandler
    {
        public int TotalChamadas { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            TotalChamadas++;

            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/leitura", request.RequestUri?.AbsolutePath);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    lixeiraId,
                    ocupacaoPercentual = 85
                })
            });
        }
    }
}