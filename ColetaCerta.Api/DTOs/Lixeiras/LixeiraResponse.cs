namespace ColetaCerta.Api.DTOs.Lixeiras;

public record LixeiraResponse(
    int Id,
    string Codigo,
    int CapacidadeLitros,
    int PontoColetaId);

