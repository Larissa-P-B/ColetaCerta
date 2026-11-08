using System.ComponentModel.DataAnnotations;

namespace ColetaCerta.Api.DTOs.Lixeiras;

public class LixeiraRequest
{
    [Required(ErrorMessage = "O código é obrigatório.")]
    [StringLength(50)]
    public string Codigo { get; set; } = string.Empty;

    [Range(1, int.MaxValue,
        ErrorMessage = "A capacidade deve ser maior que zero.")]
    public int CapacidadeLitros { get; set; }

    [Range(1, int.MaxValue,
        ErrorMessage = "Informe um ponto de coleta válido.")]
    public int PontoColetaId { get; set; }
}