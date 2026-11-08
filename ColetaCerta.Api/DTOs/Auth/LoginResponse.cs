namespace ColetaCerta.Api.DTOs.Auth;

public record LoginResponse(
    string Token,
    string Tipo,
    DateTimeOffset ExpiraEm,
    string Login,
    string Perfil);