using ApiTesourariaAMA.context;
using ApiTesourariaAMA.Dtos;
using ApiTesourariaAMA.Enums;
using ApiTesourariaAMA.Models;
using ApiTesourariaAMA.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System.Security.Claims;

namespace ApiTesourariaAMA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DebitosMembrosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CloudinaryService _cloudinaryService;

    public DebitosMembrosController(AppDbContext context, CloudinaryService cloudinaryService)
    {
        _context = context;
        _cloudinaryService = cloudinaryService;
    }

    /// <summary>
    /// Lista os débitos. Tesoureiro vê todos; Membros/Usuários comuns vêem apenas os seus.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Tesoureiro")]
    public async Task<ActionResult<IEnumerable<DebitoMembroResponseDto>>> ObterTodos()
    {
        var query = _context.DebitosMembros
            .Include(d => d.Usuario)
            .Include(d => d.TipoDoacao)
            .Include(d => d.Comprovante)
            .AsNoTracking();

        if (!User.IsInRole("Tesoureiro"))
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int usuarioId))
            {
                query = query.Where(d => d.UsuarioId == usuarioId);
            }
        }

        var debitos = await query
            .Select(d => new DebitoMembroResponseDto(
                d.Id,
                d.UsuarioId,
                d.Usuario.Nome,
                d.TipoDoacaoId,
                d.TipoDoacao.Nome,
                d.Valor,
                d.Status,
                d.DataCriacao,
                d.MesReferencia,
                d.ComprovanteId,
                d.Comprovante != null ? d.Comprovante.UrlCloudinary : null
            ))
            .ToListAsync();

        return Ok(debitos);
    }

    /// <summary>
    /// Detalhes de um débito pelo ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Tesoureiro")]
    public async Task<ActionResult<DebitoMembroResponseDto>> ObterPorId(int id)
    {
        var debito = await _context.DebitosMembros
            .Include(d => d.Usuario)
            .Include(d => d.TipoDoacao)
            .Include(d => d.Comprovante)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (debito == null)
            return NotFound(new { erro = "Débito não encontrado." });

        return Ok(new DebitoMembroResponseDto(
            debito.Id,
            debito.UsuarioId,
            debito.Usuario.Nome,
            debito.TipoDoacaoId,
            debito.TipoDoacao.Nome,
            debito.Valor,
            debito.Status,
            debito.DataCriacao,
            debito.MesReferencia,
            debito.ComprovanteId,
            debito.Comprovante?.UrlCloudinary
        ));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> ObterMeusDebitos()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int usuarioId))
            return Unauthorized();

        var debitos = await _context.DebitosMembros
            .Where(d => d.UsuarioId == usuarioId)
            .Select(d => new DebitoMembroResponseDto(
                d.Id,
                d.UsuarioId,
                d.Usuario.Nome,
                d.TipoDoacaoId,
                d.TipoDoacao.Nome,
                d.Valor,
                d.Status,
                d.DataCriacao,
                d.MesReferencia,
                d.ComprovanteId,
                d.Comprovante != null ? d.Comprovante.UrlCloudinary : null
            ))
            .ToListAsync();

        return Ok(debitos);
    }

    /// <summary>
    /// Cria um novo débito em aberto (Apenas Tesoureiro).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Tesoureiro")]
    public async Task<ActionResult<DebitoMembroResponseDto>> Criar([FromBody] CriarDebitoMembroDto dto)
    {
        var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == dto.UsuarioId);
        if (!usuarioExiste)
            return BadRequest(new { erro = $"Usuário com ID {dto.UsuarioId} não existe." });

        var tipo = await _context.TiposDoacao.Include(t => t.UsuariosEspecificos).FirstOrDefaultAsync(t => t.Id == dto.TipoDoacaoId);
        if (tipo == null)
            return BadRequest(new { erro = $"Tipo de doação com ID {dto.TipoDoacaoId} não existe." });

        if (!TipoDisponivelParaUsuario(tipo, dto.UsuarioId))
            return BadRequest(new { erro = "Este tipo de doação não está atribuído a este membro." });

        var mes = NormalizarMes(dto.MesReferencia);
        if (!TipoAbertoNoMes(tipo, mes))
            return BadRequest(new { erro = "Este tipo de doação não está aberto para o mês selecionado." });
        if (await JaExisteNoMes(dto.UsuarioId, dto.TipoDoacaoId, mes))
            return Conflict(new { erro = "Já existe uma doação deste tipo para este usuário neste mês." });

        var debito = new DebitoMembro
        {
            UsuarioId = dto.UsuarioId,
            TipoDoacaoId = dto.TipoDoacaoId,
            Valor = dto.Valor,
            Status = StatusDebito.EmAberto,
            DataCriacao = DateTime.UtcNow,
            MesReferencia = mes,
            ComprovanteId = null
        };

        _context.DebitosMembros.Add(debito);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            return Conflict(new { erro = "Já existe uma doação deste tipo para este usuário neste mês." });
        }

        var usuario = await _context.Usuarios.FindAsync(dto.UsuarioId);
        var tipoDoacao = await _context.TiposDoacao.Include(t => t.UsuariosEspecificos).FirstOrDefaultAsync(t => t.Id == dto.TipoDoacaoId);

        var response = new DebitoMembroResponseDto(
            debito.Id,
            debito.UsuarioId,
            usuario?.Nome ?? string.Empty,
            debito.TipoDoacaoId,
            tipoDoacao?.Nome ?? string.Empty,
            debito.Valor,
            debito.Status,
            debito.DataCriacao,
            mes,
            null,
            null
        );

        return CreatedAtAction(nameof(ObterPorId), new { id = debito.Id }, response);
    }

    /// <summary>
    /// Cria um novo débito em aberto associado ao usuário atualmente logado.
    /// </summary>
    [HttpPost("meus-debitos")]
    public async Task<ActionResult<DebitoMembroResponseDto>> CriarMeuDebito([FromBody] CriarMeuDebitoDto dto)
    {
        // Extrai o ID do usuário logado diretamente a partir do Token JWT
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(usuarioIdClaim, out int usuarioId))
            return Unauthorized(new { erro = "Usuário não autenticado ou token inválido." });

        var usuario = await _context.Usuarios.FindAsync(usuarioId);
        if (usuario == null)
            return NotFound(new { erro = "Usuário não encontrado." });

        var tipoDoacao = await _context.TiposDoacao.Include(t => t.UsuariosEspecificos).FirstOrDefaultAsync(t => t.Id == dto.TipoDoacaoId);
        if (tipoDoacao == null)
            return BadRequest(new { erro = $"Tipo de doação com ID {dto.TipoDoacaoId} não existe." });

        if (!TipoDisponivelParaUsuario(tipoDoacao, usuarioId))
            return BadRequest(new { erro = "Este tipo de doação não está atribuído a você." });

        var mes = NormalizarMes(dto.MesReferencia);
        if (!TipoAbertoNoMes(tipoDoacao, mes))
            return BadRequest(new { erro = "Este tipo de doação não está aberto para o mês selecionado." });
        if (await JaExisteNoMes(usuarioId, dto.TipoDoacaoId, mes))
            return Conflict(new { erro = "Você já possui uma doação deste tipo neste mês." });

        var debito = new DebitoMembro
        {
            UsuarioId = usuarioId,
            TipoDoacaoId = dto.TipoDoacaoId,
            Valor = dto.Valor,
            Status = StatusDebito.EmAberto,
            DataCriacao = DateTime.UtcNow,
            MesReferencia = mes,
            ComprovanteId = null
        };

        _context.DebitosMembros.Add(debito);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            return Conflict(new { erro = "Você já possui uma doação deste tipo neste mês." });
        }

        var response = new DebitoMembroResponseDto(
            debito.Id,
            debito.UsuarioId,
            usuario.Nome,
            debito.TipoDoacaoId,
            tipoDoacao.Nome,
            debito.Valor,
            debito.Status,
            debito.DataCriacao,
            mes,
            null,
            null
        );

        return CreatedAtAction(nameof(ObterPorId), new { id = debito.Id }, response);
    }

    private static DateTime NormalizarMes(DateTime? valor)
    {
        var data = valor?.ToUniversalTime() ?? DateTime.UtcNow;
        return new DateTime(data.Year, data.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private static bool TipoAbertoNoMes(TipoDoacao tipo, DateTime mes) =>
        (tipo.MesAbertura ?? new DateTime(tipo.DataCriacao.Year, tipo.DataCriacao.Month, 1)) <= mes &&
        mes <= new DateTime(tipo.Data_Expiracao.Year, tipo.Data_Expiracao.Month, 1);

    private static bool TipoDisponivelParaUsuario(TipoDoacao tipo, int usuarioId) =>
        tipo.TodosUsuarios || tipo.UsuariosEspecificos.Any(u => u.Id == usuarioId);

    private async Task<bool> JaExisteNoMes(int usuarioId, int tipoId, DateTime mes) =>
        await _context.DebitosMembros.AnyAsync(d => d.UsuarioId == usuarioId && d.TipoDoacaoId == tipoId &&
            (d.MesReferencia == mes || (d.MesReferencia == null && d.DataCriacao.Year == mes.Year && d.DataCriacao.Month == mes.Month)));

    /// <summary>
    /// Permite ao Tesoureiro enviar ou substituir o comprovante de qualquer débito.
    /// </summary>
    [HttpPost("{id:int}/anexar-comprovante")]
    [Authorize(Roles = "Tesoureiro")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AnexarComprovanteTesoureiro(int id, [FromForm] AnexarComprovanteDto dto)
    {
        var debito = await _context.DebitosMembros
            .Include(d => d.Comprovante)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (debito == null)
            return NotFound(new { erro = "Débito não encontrado para vinculação do comprovante." });

        return await ProcessarAnexoComprovante(debito, dto);
    }

    /// <summary>
    /// Permite ao usuário enviar ou substituir o comprovante do próprio débito.
    /// </summary>
    [HttpPost("meus-debitos/{id:int}/anexar-comprovante")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AnexarMeuComprovante(int id, [FromForm] AnexarComprovanteDto dto)
    {
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(usuarioIdClaim, out int usuarioId))
            return Unauthorized(new { erro = "Usuário não autenticado ou token inválido." });

        var debito = await _context.DebitosMembros
            .Include(d => d.Comprovante)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (debito == null)
            return NotFound(new { erro = "Débito não encontrado para vinculação do comprovante." });

        if (debito.UsuarioId != usuarioId)
            return Forbid();

        return await ProcessarAnexoComprovante(debito, dto);
    }

    private async Task<IActionResult> ProcessarAnexoComprovante(
        DebitoMembro debito,
        AnexarComprovanteDto dto)
    {
        if (debito.Status == StatusDebito.Pago)
            return BadRequest(new { erro = "O débito já se encontra PAGO e não permite envio de novos comprovantes." });

        if (dto.Arquivo == null || dto.Arquivo.Length == 0)
            return BadRequest(new { erro = "O arquivo enviado é inválido ou está vazio." });

        // Validação de extensão
        var extensao = Path.GetExtension(dto.Arquivo.FileName).ToLowerInvariant();
        var extensoesValidas = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
        if (!extensoesValidas.Contains(extensao))
            return BadRequest(new { erro = "Formato de arquivo não suportado. Envie imagens (JPG, PNG) ou PDF." });

        try
        {
            // Chama a função auxiliar da service que lida com upload e reupload (limpeza do antigo se houver)
            var comprovante = await _cloudinaryService.ProcessarComprovanteAsync(dto.Arquivo, debito.Comprovante);

            if (debito.Comprovante == null)
            {
                _context.Comprovantes.Add(comprovante);
                await _context.SaveChangesAsync();
                debito.ComprovanteId = comprovante.Id;
            }

            debito.Status = StatusDebito.EmAberto;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Comprovante anexado. A tesouraria deve revisar e aprovar o pagamento.",
                debitoId = debito.Id,
                comprovanteId = comprovante.Id,
                status = debito.Status.ToString(),
                url = comprovante.UrlCloudinary
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { erro = $"Falha ao processar o comprovante no Cloudinary: {ex.Message}" });
        }
    }

    [HttpPatch("{id:int}/aprovar")]
    [Authorize(Roles = "Tesoureiro")]
    public async Task<IActionResult> Aprovar(int id)
    {
        var debito = await _context.DebitosMembros.FindAsync(id);
        if (debito == null) return NotFound(new { erro = "Doação não encontrada." });
        if (debito.ComprovanteId == null)
            return BadRequest(new { erro = "Anexe um comprovante antes de aprovar esta doação." });
        debito.Status = StatusDebito.Pago;
        await _context.SaveChangesAsync();
        return Ok(new { mensagem = "Comprovante revisado e pagamento aprovado." });
    }

    /// <summary>
    /// Rejeita o comprovante alterando o status do débito para Rejeitado (Apenas Tesoureiro).
    /// </summary>
    [HttpPatch("{id:int}/rejeitar")]
    [Authorize(Roles = "Tesoureiro")]
    public async Task<IActionResult> Rejeitar(int id)
    {
        var debito = await _context.DebitosMembros.FindAsync(id);

        if (debito == null)
            return NotFound(new { erro = "Débito não encontrado." });

        debito.Status = StatusDebito.Rejeitado;
        await _context.SaveChangesAsync();

        return Ok(new { mensagem = "Débito marcado como REJEITADO." });
    }
}
