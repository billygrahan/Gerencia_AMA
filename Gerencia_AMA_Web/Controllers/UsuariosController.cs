using ApiTesourariaAMA.context;
using ApiTesourariaAMA.DTOs;
using ApiTesourariaAMA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApiTesourariaAMA.Enums;
using ApiTesourariaAMA.Dtos;
using System.Security.Claims;

namespace ApiTesourariaAMA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Tesoureiro")] // Apenas Tesoureiros podem acessar este controller
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsuariosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsuarios()
    {
        var usuarios = await _context.Usuarios
            .Select(u => new UsuarioResponseDto(u.Id, u.Nome, u.Email, u.Perfil))
            .ToListAsync();

        return Ok(usuarios);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUsuario(int id)
    {
        var u = await _context.Usuarios.FindAsync(id);
        if (u == null) return NotFound("Usuário não encontrado.");

        return Ok(new UsuarioResponseDto(u.Id, u.Nome, u.Email, u.Perfil));
    }

    [HttpGet("TipoDoacoes/{UsuarioId}")]
    public async Task<IActionResult> GetTipoDoacaoByUsuarioId(int UsuarioId)
    {
        if (!await _context.Usuarios.AnyAsync(u => u.Id == UsuarioId)) return NotFound("Usuário não encontrado.");

        var tipos = await _context.TiposDoacao.Include(t => t.UsuariosEspecificos).AsNoTracking().ToListAsync();
        var tiposDoacao = tipos
            .Where(t => t.TodosUsuarios || t.UsuariosEspecificos.Any(u => u.Id == UsuarioId))
            .Select(t => new TipoDoacaoResponseDto(
                t.Id, t.Nome, t.DataCriacao,
                t.MesAbertura ?? new DateTime(t.DataCriacao.Year, t.DataCriacao.Month, 1),
                t.Data_Expiracao, t.Descricao, t.ValorPadrao, t.TodosUsuarios,
                t.UsuariosEspecificos.Select(u => u.Id).ToList()))
            .ToList();

        return Ok(tiposDoacao);
    }

    [HttpPost]
    public async Task<IActionResult> CriarUsuario([FromBody] CriarUsuarioDto dto)
    {
        if (await _context.Usuarios.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("E-mail já cadastrado.");

        var usuario = new Usuario
        {
            Nome = dto.Nome,
            Email = dto.Email,
            Senha = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
            Perfil = dto.Perfil
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        var response = new UsuarioResponseDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Perfil);
        return CreatedAtAction(nameof(GetUsuario), new { id = usuario.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> AtualizarUsuario(int id, [FromBody] AtualizarUsuarioDto dto)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound("Usuário não encontrado.");

        if (await _context.Usuarios.AnyAsync(u => u.Id != id && u.Email.ToLower() == dto.Email.ToLower()))
            return BadRequest("E-mail já cadastrado por outro usuário.");

        usuario.Nome = dto.Nome;
        usuario.Email = dto.Email;
        usuario.Perfil = dto.Perfil;

        if (!string.IsNullOrWhiteSpace(dto.NovaSenha))
            usuario.Senha = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletarUsuario(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound("Usuário não encontrado.");

        if (int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var currentUserId) && currentUserId == id)
            return BadRequest("Não é possível excluir a própria conta por esta tela.");

        if (await _context.DebitosMembros.AnyAsync(d => d.UsuarioId == id))
            return Conflict(new { erro = "Este usuário possui histórico financeiro. Altere o perfil ou mantenha o cadastro para preservar os registros." });

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
