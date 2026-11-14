using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ColetaCerta.Api.Data;
using ColetaCerta.Api.Entities;
using ColetaCerta.Api.Entities.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace ColetaCerta.Tests;

public partial class IntegracaoIotRotaTests
{
    private const string SenhaDeTeste = "ColetaCerta@Teste123";
    private const string RotaLogin = "/api/auth/login";
    private const string RotaPontosColeta = "/api/pontos-coleta";

    [Theory]
    [InlineData("admin", PerfilUsuario.ADMIN, 201)]
    [InlineData("user", PerfilUsuario.USER, 403)]
    public async Task CadastrarPonto_ComTokenDoLogin_RespeitaPerfil(
        string login,
        PerfilUsuario perfil,
        int statusEsperado)
    {
        using var fabrica = new ApiDeTeste();
        using var api = ConfigurarBancoDeAutenticacao(fabrica);
        using var cliente = api.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        await CadastrarUsuarioDeTesteAsync(api.Services, login, perfil);

        using var respostaLogin = await cliente.PostAsJsonAsync(
            RotaLogin, new { login, senha = SenhaDeTeste });

        Assert.Equal(HttpStatusCode.OK, respostaLogin.StatusCode);

        var dadosLogin = await respostaLogin.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(login, dadosLogin.GetProperty("login").GetString());
        Assert.Equal(perfil.ToString(), dadosLogin.GetProperty("perfil").GetString());

        var token = dadosLogin.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var respostaCadastro = await cliente.PostAsJsonAsync(
            RotaPontosColeta,
            new
            {
                nome = "Ponto de teste JWT",
                endereco = "Rua Exemplo, 100",
                bairro = "Ipiranga",
                cidade = "São Paulo"
            });

        Assert.Equal(statusEsperado, (int)respostaCadastro.StatusCode);

        using var respostaConsulta = await cliente.GetAsync(RotaPontosColeta);
        Assert.Equal(HttpStatusCode.OK, respostaConsulta.StatusCode);

        var pontos = await respostaConsulta.Content
            .ReadFromJsonAsync<JsonElement>();

        // Cada teste começa com a tabela de pontos vazia.
        var quantidadeEsperada = perfil == PerfilUsuario.ADMIN ? 1 : 0;
        Assert.Equal(quantidadeEsperada, pontos.GetArrayLength());
    }

    [Fact]
    public async Task Login_ComSenhaIncorreta_Retorna401()
    {
        using var fabrica = new ApiDeTeste();
        using var api = ConfigurarBancoDeAutenticacao(fabrica);
        using var cliente = api.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        await CadastrarUsuarioDeTesteAsync(
            api.Services, "user", PerfilUsuario.USER);

        using var resposta = await cliente.PostAsJsonAsync(
            RotaLogin,
            new { login = "user", senha = "SenhaIncorreta123" });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    private static WebApplicationFactory<Program> ConfigurarBancoDeAutenticacao(
        WebApplicationFactory<Program> fabrica)
    {
        var nomeBanco = $"coletacerta-auth-{Guid.NewGuid()}";

        return fabrica.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(nomeBanco));
            });
        });
    }

    private static async Task CadastrarUsuarioDeTesteAsync(
        IServiceProvider services,
        string login,
        PerfilUsuario perfil)
    {
        using var scope = services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();

        var usuario = new Usuario(login, "hash-temporario", perfil);
        usuario.AtualizarSenhaHash(hasher.HashPassword(usuario, SenhaDeTeste));

        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();
    }
}