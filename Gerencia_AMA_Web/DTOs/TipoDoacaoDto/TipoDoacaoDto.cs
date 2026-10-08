using System.ComponentModel.DataAnnotations;

namespace ApiTesourariaAMA.Dtos;

public record CriarTipoDoacaoDto
{
    [Required(ErrorMessage = "O nome do tipo de doação é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
    public string Nome { get; init; } = string.Empty;

    /// <summary>
    /// Data de expiração (opcional no cadastro, padrão para o 1º dia do próximo mês).
    /// </summary>
    public DateTime? DataExpiracao { get; init; }

    [StringLength(255, ErrorMessage = "A descrição não pode exceder 255 caracteres.")]
    public string? Descricao { get; init; }
}

public record AtualizarTipoDoacaoDto
{
    [Required(ErrorMessage = "O nome do tipo de doação é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
    public string Nome { get; init; } = string.Empty;

    public DateTime DataExpiracao { get; init; }

    [StringLength(255, ErrorMessage = "A descrição não pode exceder 255 caracteres.")]
    public string? Descricao { get; init; }
}

public record TipoDoacaoResponseDto(
    int Id,
    string Nome,
    DateTime DataCriacao,
    DateTime DataExpiracao,
    string? Descricao
);