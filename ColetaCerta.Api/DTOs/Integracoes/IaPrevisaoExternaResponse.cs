namespace ColetaCerta.Api.DTOs.Integracoes;

public class IaPrevisaoExternaResponse
{
    public string? Regiao { get; set; }

    public DateOnly? PeriodoInicio { get; set; }

    public DateOnly? PeriodoFim { get; set; }

    public decimal? SolicitacoesPrevistas { get; set; }

    public string? Modelo { get; set; }
}