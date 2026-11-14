using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ColetaCerta.Api.DTOs.Integracoes;
using ColetaCerta.Api.Entities.Enums;
using ColetaCerta.Api.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace ColetaCerta.Tests;

public partial class IntegracaoIotRotaTests
{
    private const string Rota = "/api/integracoes/iot/lixeiras/1";
    private const string Emissor = "ColetaCerta.Testes";
    private const string Publico = "ColetaCerta.Testes.Clientes";

    // Chave exclusiva dos testes automatizados.
    private const string Chave = "chave-jwt-exclusiva-dos-testes-coletacerta";

    [Theory]
    [InlineData("ADMIN")]
    [InlineData("USER")]
    public async Task Consultar_ComPerfilPermitido_RetornaOcupacao(string perfil)
    {
        using var api = new ApiDeTeste();
        using var cliente = api.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CriarToken(perfil));

        using var resposta = await cliente.GetAsync(Rota);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var json = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("lixeiraId").GetInt32());
        Assert.Equal(85m, json.GetProperty("ocupacaoPercentual").GetDecimal());
        Assert.Equal("QuaseCheia", json.GetProperty("situacaoOcupacao").GetString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("token-invalido")]
    public async Task Consultar_SemTokenValido_Retorna401(string? token)
    {
        using var api = new ApiDeTeste();
        using var cliente = api.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        if (token is not null)
        {
            cliente.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        using var resposta = await cliente.GetAsync(Rota);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    private static string CriarToken(string perfil)
    {
        var agora = DateTime.UtcNow;
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Chave));

        var token = new JwtSecurityToken(
            issuer: Emissor,
            audience: Publico,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "1"),
                new Claim("role", perfil)
            },
            notBefore: agora.AddSeconds(-5),
            expires: agora.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                chave, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class ApiDeTeste : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Program.cs lê essas configurações antes de builder.Build().
            var configuracoes = new Dictionary<string, string>
            {
                ["ConnectionStrings__DefaultConnection"] =
                    "Server=localhost;Database=coletacerta_testes;User=testes;",
                ["Jwt__Issuer"] = Emissor,
                ["Jwt__Audience"] = Publico,
                ["Jwt__Key"] = Chave,
                ["Jwt__ExpirationMinutes"] = "60"
            };

            var valoresAnteriores = new Dictionary<string, string?>();

            try
            {
                foreach (var (nome, valor) in configuracoes)
                {
                    valoresAnteriores[nome] = Environment.GetEnvironmentVariable(nome);
                    Environment.SetEnvironmentVariable(nome, valor);
                }

                return base.CreateHost(builder);
            }
            finally
            {
                foreach (var (nome, valor) in valoresAnteriores)
                {
                    Environment.SetEnvironmentVariable(nome, valor);
                }
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IIntegracaoIotService>();
                services.AddSingleton<IIntegracaoIotService>(
                    new IntegracaoIotSimulada());
            });
        }
    }

    private sealed class IntegracaoIotSimulada : IIntegracaoIotService
    {
        public Task<LixeiraOcupacaoResponse?> ConsultarAsync(
            int lixeiraId,
            CancellationToken cancellationToken)
        {
            var leitura = new LixeiraOcupacaoResponse(
                lixeiraId,
                85m,
                SituacaoOcupacao.QuaseCheia,
                DateTimeOffset.UtcNow,
                "ESP32_SIMULADO");

            return Task.FromResult<LixeiraOcupacaoResponse?>(leitura);
        }
    }
}