namespace ColetaCerta.Api.Entities;

public class PontoColeta
{
    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public string Endereco { get; private set; } = string.Empty;

    public string Bairro { get; private set; } = string.Empty;

    public string Cidade { get; private set; } = string.Empty;

    public ICollection<Lixeira> Lixeiras { get; private set; }
        = new List<Lixeira>();

    // Utilizado pelo Entity Framework.
    private PontoColeta()
    {
    }

    public PontoColeta(
        string nome,
        string endereco,
        string bairro,
        string cidade)
    {
        AtualizarDados(nome, endereco, bairro, cidade);
    }

    public void AtualizarDados(
        string nome,
        string endereco,
        string bairro,
        string cidade)
    {
        // Validamos tudo antes de alterar o objeto.
        var nomeValidado = ValidarTexto(nome, 100, "Nome");
        var enderecoValidado = ValidarTexto(endereco, 200, "Endereço");
        var bairroValidado = ValidarTexto(bairro, 100, "Bairro");
        var cidadeValidada = ValidarTexto(cidade, 100, "Cidade");

        Nome = nomeValidado;
        Endereco = enderecoValidado;
        Bairro = bairroValidado;
        Cidade = cidadeValidada;
    }

    private static string ValidarTexto(
        string valor,
        int tamanhoMaximo,
        string campo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(
                $"{campo} é obrigatório.");
        }

        var texto = valor.Trim();

        if (texto.Length > tamanhoMaximo)
        {
            throw new ArgumentException(
                $"{campo} deve ter até {tamanhoMaximo} caracteres.");
        }

        return texto;
    }
}