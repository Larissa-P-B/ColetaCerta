using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ColetaCerta.Api.Configurations;
using ColetaCerta.Api.Data;
using ColetaCerta.Api.DTOs.Auth;
using ColetaCerta.Api.Entities;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ColetaCerta.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly JwtSettings _jwt;

    public AuthService(
        AppDbContext context,
        IPasswordHasher<Usuario> passwordHasher,
        JwtSettings jwt)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
    }

    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var login = request.Login.Trim().ToLowerInvariant();

        var usuario = await _context.Usuarios
            .SingleOrDefaultAsync(
                u => u.Login == login,
                cancellationToken);

        if (usuario is null || !usuario.Ativo)
        {
            return null;
        }

        var verificacao = _passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.SenhaHash,
            request.Senha);

        if (verificacao == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (verificacao ==
            PasswordVerificationResult.SuccessRehashNeeded)
        {
            var novoHash = _passwordHasher.HashPassword(
                usuario,
                request.Senha);

            usuario.AtualizarSenhaHash(novoHash);

            await _context.SaveChangesAsync(cancellationToken);
        }

        var agora = DateTimeOffset.UtcNow;
        var expiracao = agora.AddMinutes(_jwt.ExpirationMinutes);

        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                usuario.Id.ToString()),

            new Claim("name", usuario.Login),

            new Claim("role", usuario.Perfil.ToString()),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        var chave = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwt.Key));

        var credenciais = new SigningCredentials(
            chave,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: agora.UtcDateTime,
            expires: expiracao.UtcDateTime,
            signingCredentials: credenciais);

        return new LoginResponse(
            Token: new JwtSecurityTokenHandler().WriteToken(token),
            Tipo: "Bearer",
            ExpiraEm: expiracao,
            Login: usuario.Login,
            Perfil: usuario.Perfil.ToString());
    }
}