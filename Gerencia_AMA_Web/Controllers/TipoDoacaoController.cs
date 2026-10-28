using ApiTesourariaAMA.context;
using ApiTesourariaAMA.Dtos;
using ApiTesourariaAMA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApiTesourariaAMA.Services;

namespace ApiTesourariaAMA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] 
public class TiposDoacaoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly MonthlyDebitGenerator _monthlyDebitGenerator;

    public TiposDoacaoController(AppDbContext context, MonthlyDebitGenerator monthlyDebitGenerator)
    {
        _context = context;
        _monthlyDebitGenerator = monthlyDebitGenerator;
    }

    /// <summary>
    /// Lista todos os tipos de doação cadastrados.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TipoDoacaoResponseDto>>> ObterTodos()
    {
        var query = _context.TiposDoacao.Include(t => t.UsuariosEspecificos).AsNoTracking();
        var entities = await query.ToListAsync();
        if (!User.IsInRole("Tesoureiro"))
        {
            if (!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
                return Unauthorized();
            entities = entities.Where(t => t.TodosUsuarios || t.UsuariosEspecificos.Any(u => u.Id == userId)).ToList();
        }
        var includeRecipients = User.IsInRole("Tesoureiro");
        var tipos = entities.Select(type => ToResponse(type, includeRecipients)).ToList();

        return Ok(tipos);
    }

    /// <summary>
    /// Obtém os detalhes de um tipo de doação pelo ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TipoDoacaoResponseDto>> ObterPorId(int id)
    {
        var tipo = await _context.TiposDoacao.Include(t => t.UsuariosEspecificos)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tipo == null)
            return NotFound("Tipo de doação não encontrado.");

        if (!User.IsInRole("Tesoureiro") && !TipoDisponivelParaUsuario(tipo, User))
            return NotFound("Tipo de doação não encontrado.");

        return Ok(ToResponse(tipo, User.IsInRole("Tesoureiro")));
    }

    /// <summary>
    /// Cadastra um novo tipo de doação.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Tesoureiro")]
    public async Task<ActionResult<TipoDoacaoResponseDto>> Criar([FromBody] CriarTipoDoacaoDto dto)
    {
        var novoTipo = new TipoDoacao
        {
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            DataCriacao = DateTime.UtcNow,
            MesAbertura = NormalizarMes(dto.MesAbertura ?? DateTime.UtcNow),
            ValorPadrao = dto.ValorPadrao,
            TodosUsuarios = dto.TodosUsuarios
        };

        if (dto.DataExpiracao.HasValue)
        {
            novoTipo.Data_Expiracao = dto.DataExpiracao.Value;
        }

        if (novoTipo.MesAbertura!.Value > novoTipo.Data_Expiracao)
            return BadRequest(new { erro = "O mês de abertura precisa ser anterior ou igual ao mês de encerramento." });

        var destinatarios = await ObterDestinatariosSelecionados(dto.TodosUsuarios, dto.UsuariosIds);
        if (destinatarios == null)
            return BadRequest(new { erro = "A seleção contém usuários inexistentes." });
        novoTipo.UsuariosEspecificos = destinatarios;

        _context.TiposDoacao.Add(novoTipo);
        await _context.SaveChangesAsync();
        await _monthlyDebitGenerator.TryEnsureDueDebitsAsync();

        var response = ToResponse(novoTipo);

        return CreatedAtAction(nameof(ObterPorId), new { id = novoTipo.Id }, response);
    }

    /// <summary>
    /// Atualiza um tipo de doação existente.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Tesoureiro")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarTipoDoacaoDto dto)
    {
        var tipo = await _context.TiposDoacao.Include(t => t.UsuariosEspecificos).FirstOrDefaultAsync(t => t.Id == id);

        if (tipo == null)
            return NotFound("Tipo de doação não encontrado.");

        var openingMonth = NormalizarMes(dto.MesAbertura ?? tipo.MesAbertura ?? tipo.DataCriacao);
        var expirationMonth = NormalizarMes(dto.DataExpiracao);
        if (openingMonth > expirationMonth)
            return BadRequest("O mês de abertura precisa ser anterior ou igual ao mês de encerramento.");

        var destinatarios = await ObterDestinatariosSelecionados(dto.TodosUsuarios, dto.UsuariosIds);
        if (destinatarios == null)
            return BadRequest("A seleção contém usuários inexistentes.");

        tipo.Nome = dto.Nome;
        tipo.Descricao = dto.Descricao;
        tipo.MesAbertura = openingMonth;
        tipo.Data_Expiracao = expirationMonth;
        tipo.ValorPadrao = dto.ValorPadrao;
        tipo.TodosUsuarios = dto.TodosUsuarios;
        tipo.UsuariosEspecificos.Clear();
        foreach (var destinatario in destinatarios) tipo.UsuariosEspecificos.Add(destinatario);

        await _context.SaveChangesAsync();
        await _monthlyDebitGenerator.TryEnsureDueDebitsAsync();

        return NoContent();
    }

    private static DateTime NormalizarMes(DateTime data) => new(data.Year, data.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    private async Task<List<Usuario>?> ObterDestinatariosSelecionados(bool todosUsuarios, List<int> usuariosIds)
    {
        if (todosUsuarios) return [];
        var ids = (usuariosIds ?? []).Distinct().ToList();
        if (ids.Count == 0) return null;
        var destinatarios = await _context.Usuarios.Where(u => ids.Contains(u.Id)).ToListAsync();
        return destinatarios.Count == ids.Count ? destinatarios : null;
    }

    private static bool TipoDisponivelParaUsuario(TipoDoacao tipo, System.Security.Claims.ClaimsPrincipal user) =>
        tipo.TodosUsuarios || (int.TryParse(user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId) && tipo.UsuariosEspecificos.Any(u => u.Id == userId));

    private static TipoDoacaoResponseDto ToResponse(TipoDoacao tipo, bool includeRecipientIds = true) => new(
        tipo.Id,
        tipo.Nome,
        tipo.DataCriacao,
        tipo.MesAbertura ?? new DateTime(tipo.DataCriacao.Year, tipo.DataCriacao.Month, 1),
        tipo.Data_Expiracao,
        tipo.Descricao,
        tipo.ValorPadrao,
        tipo.TodosUsuarios,
        includeRecipientIds ? tipo.UsuariosEspecificos.Select(u => u.Id).ToList() : []);

    /// <summary>
    /// Remove um tipo de doação pelo ID.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Tesoureiro")]
    public async Task<IActionResult> Deletar(int id)
    {
        var tipo = await _context.TiposDoacao.FindAsync(id);

        if (tipo == null)
            return NotFound("Tipo de doação não encontrado.");

        if (await _context.DebitosMembros.AnyAsync(d => d.TipoDoacaoId == id))
            return Conflict(new { erro = "Este tipo já possui doações registradas; encerre a campanha em vez de excluí-la para preservar o histórico." });

        _context.TiposDoacao.Remove(tipo);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
