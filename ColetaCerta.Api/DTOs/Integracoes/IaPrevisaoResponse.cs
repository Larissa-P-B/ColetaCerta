namespace ColetaCerta.Api.DTOs.Integracoes;

public record IaPrevisaoResponse(
    string Regiao,
    DateOnly PeriodoInicio,
    DateOnly PeriodoFim,
    decimal SolicitacoesPrevistas,
    string Modelo,
    DateTimeOffset ConsultadoEm);