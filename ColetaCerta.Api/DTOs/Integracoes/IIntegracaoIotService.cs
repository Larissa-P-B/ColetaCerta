namespace ColetaCerta.Api.DTOs.Integracoes;

public interface IIntegracaoIotService
{
    Task<LixeiraOcupacaoResponse?> ConsultarAsync(
        int lixeiraId,
        CancellationToken cancellationToken);
}