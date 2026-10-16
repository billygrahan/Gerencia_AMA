namespace Gerencia_AMA;

public sealed class ApiException(string message) : Exception(message);
public sealed record LoginResult(string AccessToken, string RefreshToken, DateTime Expiration, Usuario Usuario);
public sealed record Usuario(int Id, string Nome, string Email, object? Perfil)
{
    public string RoleName => Perfil?.ToString() is "2" or "Tesoureiro" ? "Tesoureiro" : "Membro";
}
public sealed record Debito(int Id, int UsuarioId, string? NomeUsuario, int TipoDoacaoId, string NomeTipoDoacao, decimal Valor, object Status, DateTime DataCriacao, DateTime? MesReferencia, int? ComprovanteId, string? UrlComprovante)
{
    public string StatusName => Status.ToString() switch { "2" or "Pago" => "Pago", "3" or "Rejeitado" => "Rejeitado", _ => "Em aberto" };
    public DateTime ReferenceMonth => MesReferencia ?? new DateTime(DataCriacao.Year, DataCriacao.Month, 1);
}
public sealed record TipoDoacao(int Id, string Nome, DateTime DataCriacao, DateTime DataExpiracao, string? Descricao)
{
    public DateTime? MesAbertura { get; init; }
    public DateTime OpeningMonth => MesAbertura ?? new DateTime(DataCriacao.Year, DataCriacao.Month, 1);
}
