using System.ComponentModel.DataAnnotations;
using ApiTesourariaAMA.Enums;

namespace ApiTesourariaAMA.DTOs;

public record CriarUsuarioDto(
    [Required][StringLength(100, MinimumLength = 3)] string Nome,
    [Required][EmailAddress] string Email,
    [Required][StringLength(100, MinimumLength = 6)] string Senha,
    [Required] PerfilUsuario Perfil
);

public record AtualizarUsuarioDto(
    [Required][StringLength(100, MinimumLength = 3)] string Nome,
    [Required][EmailAddress] string Email,
    string? NovaSenha,
    [Required] PerfilUsuario Perfil
);

public record UsuarioResponseDto(
    int Id,
    string Nome,
    string Email,
    PerfilUsuario Perfil
);