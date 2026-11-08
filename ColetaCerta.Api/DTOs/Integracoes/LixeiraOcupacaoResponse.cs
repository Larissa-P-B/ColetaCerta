using ColetaCerta.Api.Entities.Enums;

namespace ColetaCerta.Api.DTOs.Integracoes;

public record LixeiraOcupacaoResponse(
    int LixeiraId,
    decimal OcupacaoPercentual,
    SituacaoOcupacao SituacaoOcupacao,
    DateTimeOffset ConsultadoEm,
    string Origem);