using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ApiTesourariaAMA.Enums;

namespace ApiTesourariaAMA.Models;

[Table("DebitosMembros")]
public class DebitoMembro
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;

    [Required]
    public int TipoDoacaoId { get; set; }

    [ForeignKey(nameof(TipoDoacaoId))]
    public TipoDoacao TipoDoacao { get; set; } = null!;

    // Mantido com Data e Hora completa de criação
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    [Required(ErrorMessage = "O valor é obrigatório.")]
    [Range(0.01, 999999.99, ErrorMessage = "O valor deve ser maior que zero.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Valor { get; set; }

    [Required]
    public StatusDebito Status { get; set; } = StatusDebito.Pendente;

    // Tornando opcional (nullable), já que na criação o débito não possui comprovante registrado
    public int? ComprovanteId { get; set; }

    [ForeignKey(nameof(ComprovanteId))]
    public Comprovante? Comprovante { get; set; }
}