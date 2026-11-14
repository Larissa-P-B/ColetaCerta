using ColetaCerta.Api.Entities.Enums;

namespace ColetaCerta.Api.Services;

public static class ClassificadorOcupacao
{
    public static SituacaoOcupacao Classificar(decimal percentual)
    {
        return percentual switch
        {
            <= 10m => SituacaoOcupacao.Vazia,
            < 80m => SituacaoOcupacao.ParcialmenteCheia,
            < 95m => SituacaoOcupacao.QuaseCheia,
            _ => SituacaoOcupacao.Cheia
        };
    }
}