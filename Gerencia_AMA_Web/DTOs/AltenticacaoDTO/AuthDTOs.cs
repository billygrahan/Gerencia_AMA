using System.ComponentModel.DataAnnotations;

namespace ApiTesourariaAMA.DTOs;

public record LoginDto(
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    string Email,

    [Required(ErrorMessage = "A senha é obrigatória.")]
    string Senha
);

public record RefreshTokenDto(
    [Required] string AccessToken,
    [Required] string RefreshToken
);

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime Expiration,
    UsuarioResponseDto Usuario
);