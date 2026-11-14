using ColetaCerta.Api.DTOs.Integracoes;

namespace ColetaCerta.Api.Interfaces;

public interface IIntegracaoIaService
{
    Task<IaPrevisaoResponse> ConsultarPrevisaoAsync(
        string regiao,
        CancellationToken cancellationToken);
}