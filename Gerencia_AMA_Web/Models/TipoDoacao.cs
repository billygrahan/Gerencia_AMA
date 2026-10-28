using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiTesourariaAMA.Models;

[Table("TiposDoacao")]
public class TipoDoacao
{
    private DateTime _dataExpiracao = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
    private DateTime? _mesAbertura;

    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome do tipo de doação é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    /// <summary>Mês em que a campanha começa a aceitar doações. Nulo em campanhas antigas.</summary>
    public DateTime? MesAbertura
    {
        get => _mesAbertura;
        set => _mesAbertura = value.HasValue
            ? new DateTime(value.Value.Year, value.Value.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            : null;
    }

    /// 
    /// Armazena no formato Ano-Mês (truncado no primeiro dia do mês à meia-noite).
    /// 
    [Required]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:MM/yyyy}", ApplyFormatInEditMode = true)]
    public DateTime Data_Expiracao
    {
        get => _dataExpiracao;
        set => _dataExpiracao = new DateTime(value.Year, value.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    [StringLength(255, ErrorMessage = "A descrição não pode exceder 255 caracteres.")]
    public string? Descricao { get; set; }

    /// <summary>Valor mensal sugerido usado na geração automática dos débitos.</summary>
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 999999.99)]
    public decimal ValorPadrao { get; set; }

    /// <summary>Quando falso, somente os membros da coleção recebem esta campanha.</summary>
    public bool TodosUsuarios { get; set; } = true;

    public ICollection<Usuario> UsuariosEspecificos { get; set; } = new List<Usuario>();
}
