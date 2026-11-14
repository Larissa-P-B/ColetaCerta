using ColetaCerta.Api.Entities.Enums;
using ColetaCerta.Api.Services;
using Xunit;

namespace ColetaCerta.Tests;

public class ClassificadorOcupacaoTests
{
    [Theory]
    [InlineData(0, SituacaoOcupacao.Vazia)]
    [InlineData(1000, SituacaoOcupacao.Vazia)]
    [InlineData(1001, SituacaoOcupacao.ParcialmenteCheia)]
    [InlineData(7999, SituacaoOcupacao.ParcialmenteCheia)]
    [InlineData(8000, SituacaoOcupacao.QuaseCheia)]
    [InlineData(9499, SituacaoOcupacao.QuaseCheia)]
    [InlineData(9500, SituacaoOcupacao.Cheia)]
    [InlineData(10000, SituacaoOcupacao.Cheia)]
    public void Classificar_RetornaSituacaoEsperada(
        int centesimos,
        SituacaoOcupacao situacaoEsperada)
    {
        decimal percentual = centesimos / 100m;

        var resultado = ClassificadorOcupacao.Classificar(percentual);

        Assert.Equal(situacaoEsperada, resultado);
    }
}