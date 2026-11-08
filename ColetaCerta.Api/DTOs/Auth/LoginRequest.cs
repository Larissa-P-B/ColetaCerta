using System.ComponentModel.DataAnnotations;

namespace ColetaCerta.Api.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "O login é obrigatório.")]
    [StringLength(50)]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [StringLength(128)]
    public string Senha { get; set; } = string.Empty;
}