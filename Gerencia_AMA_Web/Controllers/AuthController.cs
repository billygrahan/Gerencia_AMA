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
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;
    private readonly MonthlyDebitGenerator _monthlyDebitGenerator;

    public AuthController(AppDbContext context, TokenService tokenService, MonthlyDebitGenerator monthlyDebitGenerator)
    {
        _context = context;
        _tokenService = tokenService;
        _monthlyDebitGenerator = monthlyDebitGenerator;
    }

    /// <summary>
    /// Permite que um novo usuário se cadastre sem estar logado.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Registrar([FromBody] CriarUsuarioDto dto)
    {
        if (await _context.Usuarios.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { erro = "E-mail já cadastrado." });

        var usuario = new Usuario
        {
            Nome = dto.Nome,
            Email = dto.Email,
            Senha = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
            Perfil = PerfilUsuario.Membro // Garante que novos usuários entrem sempre como Membros por padrão
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        var response = new UsuarioResponseDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Perfil);
        return CreatedAtAction(nameof(ObterMeuPerfil), response);
    }

    /// <summary>
    /// Autentica o usuário e gera o par AccessToken/RefreshToken.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (usuario == null || !BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.Senha))
            return Unauthorized(new { erro = "E-mail ou senha inválidos." });

        await _monthlyDebitGenerator.TryEnsureDueDebitsAsync();

        var accessToken = _tokenService.GerarAccessToken(usuario);
        var refreshToken = _tokenService.GerarRefreshToken();

        usuario.RefreshToken = refreshToken;
        usuario.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Validade do Refresh Token (7 dias)
        await _context.SaveChangesAsync();

        var usuarioResponse = new UsuarioResponseDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Perfil);

        return Ok(new AuthResponseDto(accessToken, refreshToken, DateTime.UtcNow.AddMinutes(15), usuarioResponse));
    }

    /// <summary>
    /// Renova o AccessToken expirado utilizando um RefreshToken válido.
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        try
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(dto.AccessToken);
            var email = principal.Identity?.Name ?? principal.FindFirst(ClaimTypes.Email)?.Value;

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuario == null || usuario.RefreshToken != dto.RefreshToken || usuario.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return BadRequest(new { erro = "Refresh Token inválido ou expirado." });

            var newAccessToken = _tokenService.GerarAccessToken(usuario);
            var newRefreshToken = _tokenService.GerarRefreshToken();

            usuario.RefreshToken = newRefreshToken;
            usuario.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            var usuarioResponse = new UsuarioResponseDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Perfil);

            return Ok(new AuthResponseDto(newAccessToken, newRefreshToken, DateTime.UtcNow.AddMinutes(15), usuarioResponse));
        }
        catch (Exception ex)
        {
            return BadRequest(new { erro = $"Erro ao renovar token: {ex.Message}" });
        }
    }

    /// <summary>
    /// Retorna as informações do próprio usuário autenticado via Token JWT.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> ObterMeuPerfil()
    {
        await _monthlyDebitGenerator.TryEnsureDueDebitsAsync();
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
