using ApiTesourariaAMA.context;
using ApiTesourariaAMA.DTOs;
using ApiTesourariaAMA.Enums;
using ApiTesourariaAMA.Models;
using ApiTesourariaAMA.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ApiTesourariaAMA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembrosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;

    public MembrosController(AppDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Retorna as informações do próprio usuário autenticado via Token JWT.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> ObterMeuPerfil()
    {
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(usuarioIdClaim, out int usuarioId))
            return Unauthorized();

        var usuario = await _context.Usuarios.FindAsync(usuarioId);
        if (usuario == null)
            return NotFound(new { erro = "Usuário não encontrado." });

        return Ok(new UsuarioResponseDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Perfil));
    }

    /// <summary>
    /// Permite ao usuário logado atualizar seus próprios dados (sem alterar seu Perfil/Role).
    /// </summary>
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> AtualizarMeuPerfil([FromBody] AtualizarMeuPerfilDto dto)
    {
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(usuarioIdClaim, out int usuarioId))
            return Unauthorized();

        var usuario = await _context.Usuarios.FindAsync(usuarioId);
        if (usuario == null)
            return NotFound(new { erro = "Usuário não encontrado." });

        // Valida se o e-mail não pertence a outro usuário cadastrado
        if (usuario.Email != dto.Email && await _context.Usuarios.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { erro = "O e-mail informado já está em uso por outro usuário." });

        usuario.Nome = dto.Nome;
        usuario.Email = dto.Email;

        if (!string.IsNullOrWhiteSpace(dto.NovaSenha))
            usuario.Senha = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha);

        await _context.SaveChangesAsync();
        return NoContent();
    }
}