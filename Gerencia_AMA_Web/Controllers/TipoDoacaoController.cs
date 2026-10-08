using ApiTesourariaAMA.context;
using ApiTesourariaAMA.Dtos;
using ApiTesourariaAMA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiTesourariaAMA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] 
public class TiposDoacaoController : ControllerBase
{
    private readonly AppDbContext _context;

    public TiposDoacaoController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lista todos os tipos de doação cadastrados.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TipoDoacaoResponseDto>>> ObterTodos()
    {
        var tipos = await _context.TiposDoacao
            .AsNoTracking()
            .Select(t => new TipoDoacaoResponseDto(
                t.Id,
                t.Nome,
                t.DataCriacao,
                t.Data_Expiracao,
                t.Descricao
            ))
            .ToListAsync();

        return Ok(tipos);
    }

    /// <summary>
    /// Obtém os detalhes de um tipo de doação pelo ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TipoDoacaoResponseDto>> ObterPorId(int id)
    {
        var tipo = await _context.TiposDoacao
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tipo == null)
            return NotFound("Tipo de doação não encontrado.");

        var response = new TipoDoacaoResponseDto(
            tipo.Id,
            tipo.Nome,
            tipo.DataCriacao,
            tipo.Data_Expiracao,
            tipo.Descricao
        );

        return Ok(response);
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
            DataCriacao = DateTime.UtcNow
        };

        if (dto.DataExpiracao.HasValue)
        {
            novoTipo.Data_Expiracao = dto.DataExpiracao.Value;
        }

        _context.TiposDoacao.Add(novoTipo);
        await _context.SaveChangesAsync();

        var response = new TipoDoacaoResponseDto(
            novoTipo.Id,
            novoTipo.Nome,
            novoTipo.DataCriacao,
            novoTipo.Data_Expiracao,
            novoTipo.Descricao
        );

        return CreatedAtAction(nameof(ObterPorId), new { id = novoTipo.Id }, response);
    }

    /// <summary>
    /// Atualiza um tipo de doação existente.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Tesoureiro")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarTipoDoacaoDto dto)
    {
        var tipo = await _context.TiposDoacao.FindAsync(id);

        if (tipo == null)
            return NotFound("Tipo de doação não encontrado.");

        tipo.Nome = dto.Nome;
        tipo.Descricao = dto.Descricao;
        tipo.Data_Expiracao = dto.DataExpiracao;

        await _context.SaveChangesAsync();

        return NoContent();
    }

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

        _context.TiposDoacao.Remove(tipo);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}