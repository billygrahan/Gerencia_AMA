using ApiTesourariaAMA.context;
using ApiTesourariaAMA.Dtos;
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

        var tipoExiste = await _context.TiposDoacao.AnyAsync(t => t.Id == dto.TipoDoacaoId);
        if (!tipoExiste)
            return BadRequest(new { erro = $"Tipo de doação com ID {dto.TipoDoacaoId} não existe." });

        var debito = new DebitoMembro
        {
            UsuarioId = dto.UsuarioId,
            TipoDoacaoId = dto.TipoDoacaoId,
            Valor = dto.Valor,
            Status = StatusDebito.EmAberto,
            DataCriacao = DateTime.UtcNow,
            ComprovanteId = null
        };

        _context.DebitosMembros.Add(debito);
        await _context.SaveChangesAsync();

        var usuario = await _context.Usuarios.FindAsync(dto.UsuarioId);
        var tipoDoacao = await _context.TiposDoacao.FindAsync(dto.TipoDoacaoId);

        var response = new DebitoMembroResponseDto(
            debito.Id,
            debito.UsuarioId,
            usuario?.Nome ?? string.Empty,
            debito.TipoDoacaoId,
            tipoDoacao?.Nome ?? string.Empty,
            debito.Valor,
            debito.Status,
            debito.DataCriacao,
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

        var tipoDoacao = await _context.TiposDoacao.FindAsync(dto.TipoDoacaoId);
        if (tipoDoacao == null)
            return BadRequest(new { erro = $"Tipo de doação com ID {dto.TipoDoacaoId} não existe." });

        var debito = new DebitoMembro
        {
            UsuarioId = usuarioId,
            TipoDoacaoId = dto.TipoDoacaoId,
            Valor = dto.Valor,
            Status = StatusDebito.EmAberto,
            DataCriacao = DateTime.UtcNow,
            ComprovanteId = null
        };

        _context.DebitosMembros.Add(debito);
        await _context.SaveChangesAsync();

        var response = new DebitoMembroResponseDto(
            debito.Id,
            debito.UsuarioId,
            usuario.Nome,
            debito.TipoDoacaoId,
            tipoDoacao.Nome,
            debito.Valor,
            debito.Status,
            debito.DataCriacao,
            null,
            null
        );

        return CreatedAtAction(nameof(ObterPorId), new { id = debito.Id }, response);
    }

    /// <summary>
    /// Envia ou substitui o comprovante no Cloudinary.
    /// Permite upload apenas se o débito estiver EmAberto ou Rejeitado, alterando o status para Pago automaticamente.
    /// </summary>
    [HttpPost("{id:int}/anexar-comprovante")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AnexarComprovante(int id, [FromForm] AnexarComprovanteDto dto)
    {
        var debito = await _context.DebitosMembros
            .Include(d => d.Comprovante)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (debito == null)
            return NotFound(new { erro = "Débito não encontrado para vinculação do comprovante." });

        // Permite apenas se o débito estiver EmAberto ou Rejeitado
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

            // APROVAÇÃO AUTOMÁTICA
            debito.Status = StatusDebito.Pago;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Comprovante anexado e débito marcado como PAGO com sucesso!",
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