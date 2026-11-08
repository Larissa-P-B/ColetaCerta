namespace ColetaCerta.Api.DTOs.PontosColeta;

public record PontoColetaResponse(
    int Id,
    string Nome,
    string Endereco,
    string Bairro,
    string Cidade);