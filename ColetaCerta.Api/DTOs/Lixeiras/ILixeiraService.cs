using ColetaCerta.Api.DTOs.Lixeiras;

namespace ColetaCerta.Api.Interfaces;

public interface ILixeiraService
{
    Task<LixeiraResponse> CriarAsync(
        LixeiraRequest request,
        CancellationToken cancellationToken);

    Task<List<LixeiraResponse>> ListarAsync(
        CancellationToken cancellationToken);

    Task<LixeiraResponse?> ObterPorIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<bool> AtualizarAsync(
        int id,
        LixeiraRequest request,
        CancellationToken cancellationToken);

    Task<bool> ExcluirAsync(
        int id,
        CancellationToken cancellationToken);
}