using ColetaCerta.Api.Entities.Enums;

namespace ColetaCerta.Api.Entities;

public class Usuario
{
    public int Id { get; private set; }

    public string Login { get; private set; } = string.Empty;

    public string SenhaHash { get; private set; } = string.Empty;

    public PerfilUsuario Perfil { get; private set; }

    public bool Ativo { get; private set; } = true;

    private Usuario()
    {
    }

    public Usuario(
        string login,
        string senhaHash,
        PerfilUsuario perfil)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            throw new ArgumentException("O login é obrigatório.");
        }

        var loginNormalizado = login.Trim().ToLowerInvariant();

        if (loginNormalizado.Length > 50)
        {
            throw new ArgumentException(
                "O login deve ter até 50 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(senhaHash)
            || senhaHash.Length > 512)
        {
            throw new ArgumentException(
                "Informe um hash de senha válido.");
        }

        if (!Enum.IsDefined(perfil))
        {
            throw new ArgumentException("O perfil é inválido.");
        }

        Login = loginNormalizado;
        SenhaHash = senhaHash;
        Perfil = perfil;
    }

    public void AtualizarSenhaHash(string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash)
            || senhaHash.Length > 512)
        {
            throw new ArgumentException(
                "Informe um hash de senha válido.");
        }

        SenhaHash = senhaHash;
    }

    public void Desativar()
    {
        Ativo = false;
    }
}