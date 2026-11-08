using ColetaCerta.Api.Configurations;
using ColetaCerta.Api.Data;
using ColetaCerta.Api.DTOs.Auth;
using ColetaCerta.Api.DTOs.Integracoes;
using ColetaCerta.Api.DTOs.PontosColeta;
using ColetaCerta.Api.Entities;
using ColetaCerta.Api.Interfaces;
using ColetaCerta.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "A conexão DefaultConnection não foi configurada.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySQL(connectionString));



builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer(
        (document, context, cancellationToken) =>
        {
            document.Components ??= new OpenApiComponents();

            document.Components.SecuritySchemes ??=
                new Dictionary<string, IOpenApiSecurityScheme>();

            document.Components.SecuritySchemes["Bearer"] =
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Informe somente o token JWT."
                };

            foreach (var caminho in document.Paths)
            {
                if (caminho.Key.Equals(
                    "/api/auth/login",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (caminho.Value.Operations is null)
                {
                    continue;
                }

                foreach (var operacao in
                    caminho.Value.Operations.Values)
                {
                    operacao.Security ??= [];

                    operacao.Security.Add(
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecuritySchemeReference(
                                "Bearer", document)] = []
                        });
                }
            }

            return Task.CompletedTask;
        });
});

builder.Services.AddScoped<IPontoColetaService, PontoColetaService>();

builder.Services.AddScoped<ILixeiraService, LixeiraService>();

var jwt = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "A configuração JWT não foi encontrada.");

if (string.IsNullOrWhiteSpace(jwt.Key)
    || Encoding.UTF8.GetByteCount(jwt.Key) < 32
    || string.IsNullOrWhiteSpace(jwt.Issuer)
    || string.IsNullOrWhiteSpace(jwt.Audience)
    || jwt.ExpirationMinutes < 1
    || jwt.ExpirationMinutes > 1440)
{
    throw new InvalidOperationException(
        "Configuração JWT inválida. Confira a chave, " +
        "emissor, público e tempo de expiração.");
}

builder.Services.AddSingleton(jwt);

builder.Services.AddScoped<
    IPasswordHasher<Usuario>,
    PasswordHasher<Usuario>>();

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,

                ValidateAudience = true,
                ValidAudience = jwt.Audience,

                ValidateLifetime = true,
                RequireExpirationTime = true,

                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwt.Key)),

                ValidAlgorithms =
                    new[] { SecurityAlgorithms.HmacSha256 },

                NameClaimType = "name",
                RoleClaimType = "role",

                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services
    .AddHttpClient<IIntegracaoIotService, IntegracaoIotService>(
        client =>
        {
            var endereco = builder.Configuration[
                "Integracoes:Wokwi:BaseUrl"];

            if (!Uri.TryCreate(
                    endereco,
                    UriKind.Absolute,
                    out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp
                    && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException(
                    "Configure um endereço HTTP válido " +
                    "em Integracoes:Wokwi:BaseUrl.");
            }

            client.BaseAddress = uri;
            client.Timeout = TimeSpan.FromSeconds(10);
        });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    
    app.MapOpenApi().AllowAnonymous();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "ColetaCerta API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
