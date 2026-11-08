namespace ColetaCerta.Api.Entities;

public class Lixeira
{
    public int Id { get; private set; }

    public string Codigo { get; private set; } = string.Empty;

    public int CapacidadeLitros { get; private set; }

    public int PontoColetaId { get; private set; }

    public PontoColeta PontoColeta { get; private set; } = null!;

    // Utilizado pelo Entity Framework.
    private Lixeira()
    {
    }

    public Lixeira(
        string codigo,
        int capacidadeLitros,
        int pontoColetaId)
    {
        AtualizarDados(codigo, capacidadeLitros, pontoColetaId);
    }

    public void AtualizarDados(
        string codigo,
        int capacidadeLitros,
        int pontoColetaId)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ArgumentException(
                "O código da lixeira é obrigatório.");
        }

        var codigoValidado = codigo.Trim();

        if (codigoValidado.Length > 50)
        {
            throw new ArgumentException(
                "O código deve ter até 50 caracteres.");
        }

        if (capacidadeLitros <= 0)
        {
            throw new ArgumentException(
                "A capacidade deve ser maior que zero.");
        }

        if (pontoColetaId <= 0)
        {
            throw new ArgumentException(
                "Informe um ponto de coleta válido.");
        }

        Codigo = codigoValidado;
        CapacidadeLitros = capacidadeLitros;
        PontoColetaId = pontoColetaId;
    }
}