namespace ApiTesourariaAMA.DTOs;

public record AtualizarMeuPerfilDto(
    string Nome,
    string Email,
    string? NovaSenha
);