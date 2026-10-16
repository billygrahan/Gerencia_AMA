using System.ComponentModel.DataAnnotations;
using ApiTesourariaAMA.Enums;

namespace ApiTesourariaAMA.Dtos;

public record CriarDebitoMembroDto
{
    [Required(ErrorMessage = "O ID do usuário é obrigatório.")]
    public int UsuarioId { get; init; }

    [Required(ErrorMessage = "O ID do tipo de doação é obrigatório.")]
    public int TipoDoacaoId { get; init; }

    [Required(ErrorMessage = "O valor é obrigatório.")]
    [Range(0.01, 999999.99, ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Valor { get; init; }
}
public record CriarMeuDebitoDto(
    int TipoDoacaoId,
    decimal Valor
);

public record AnexarComprovanteDto
{
    [Required(ErrorMessage = "O arquivo do comprovante é obrigatório.")]
    public IFormFile Arquivo { get; init; } = null!;
}

public record DebitoMembroResponseDto(
    int Id,
    int UsuarioId,
    string NomeUsuario,
    int TipoDoacaoId,
    string NomeTipoDoacao,
    decimal Valor,
    StatusDebito Status,
    DateTime DataCriacao,
    int? ComprovanteId,
    string? UrlComprovante
);