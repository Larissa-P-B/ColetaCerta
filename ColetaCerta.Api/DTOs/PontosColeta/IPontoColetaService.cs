namespace ColetaCerta.Api.DTOs.PontosColeta
{
    public interface IPontoColetaService
    {
        Task<PontoColetaResponse> CriarAsync(
        PontoColetaRequest request,
        CancellationToken cancellationToken);

        Task<List<PontoColetaResponse>> ListarAsync(
            CancellationToken cancellationToken);

        Task<PontoColetaResponse?> ObterPorIdAsync(
            int id,
            CancellationToken cancellationToken);

        Task<bool> AtualizarAsync(
            int id,
            PontoColetaRequest request,
            CancellationToken cancellationToken);

        Task<bool> ExcluirAsync(
            int id,
            CancellationToken cancellationToken);
    }
}
