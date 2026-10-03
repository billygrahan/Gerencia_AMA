using ApiTesourariaAMA.context;
using ApiTesourariaAMA.DTOs;
using ApiTesourariaAMA.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiTesourariaAMA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;

    public AuthController(AppDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task< IActionResult> Login([FromBody] LoginDto dto)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (usuario == null || !BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.Senha))
            return Unauthorized("E-mail ou senha inválidos.");

        var accessToken = _tokenService.GerarAccessToken(usuario);
        var refreshToken = _tokenService.GerarRefreshToken();

        usuario.RefreshToken = refreshToken;
        usuario.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Validade do Refresh Token (7 dias)
        await _context.SaveChangesAsync();

        var usuarioResponse = new UsuarioResponseDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Perfil);

        return Ok(new AuthResponseDto(accessToken, refreshToken, DateTime.UtcNow.AddMinutes(15), usuarioResponse));
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        try
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(dto.AccessToken);
            var email = principal.Identity?.Name ?? principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuario == null || usuario.RefreshToken != dto.RefreshToken || usuario.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return BadRequest("Refresh Token inválido ou expirado.");

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
            return BadRequest($"Erro ao renovar token: {ex.Message}");
        }
    }
}