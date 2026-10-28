using ApiTesourariaAMA.context;
using ApiTesourariaAMA.Enums;
using ApiTesourariaAMA.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiTesourariaAMA.Services;

/// <summary>
/// Gera os débitos vencidos de cada campanha. A restrição única do banco e a checagem
/// por competência deixam a operação segura para chamadas simultâneas e reprocessamentos.
/// </summary>
public sealed class MonthlyDebitGenerator(AppDbContext db, ILogger<MonthlyDebitGenerator> logger)
{
    public async Task<int> TryEnsureDueDebitsAsync(CancellationToken cancellationToken = default)
    {
        try { return await EnsureDueDebitsAsync(cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Não foi possível verificar os débitos mensais. O processo poderá ser repetido no próximo login ou ciclo diário.");
            return 0;
        }
    }

    public async Task<int> EnsureDueDebitsAsync(CancellationToken cancellationToken = default)
    {
        var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var types = await db.TiposDoacao
            .Include(t => t.UsuariosEspecificos)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var created = 0;
        foreach (var type in types)
        {
            if (type.ValorPadrao <= 0) continue; // Campanhas antigas precisam ter o valor configurado pelo tesoureiro.
            var firstMonth = type.MesAbertura ?? new DateTime(type.DataCriacao.Year, type.DataCriacao.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastMonth = new DateTime(type.Data_Expiracao.Year, type.Data_Expiracao.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            if (firstMonth > lastMonth || firstMonth > currentMonth) continue;
            if (lastMonth > currentMonth) lastMonth = currentMonth;

            var recipients = type.TodosUsuarios
                ? await db.Usuarios.AsNoTracking().Select(u => u.Id).ToListAsync(cancellationToken)
                : type.UsuariosEspecificos.Select(u => u.Id).Distinct().ToList();
            if (recipients.Count == 0) continue;

            var existing = await db.DebitosMembros.AsNoTracking()
                .Where(d => d.TipoDoacaoId == type.Id)
                .Select(d => new { d.UsuarioId, d.MesReferencia, d.DataCriacao })
                .ToListAsync(cancellationToken);
            var existingKeys = existing.Select(d => (d.UsuarioId, Month: d.MesReferencia.HasValue
                    ? new DateTime(d.MesReferencia.Value.Year, d.MesReferencia.Value.Month, 1)
                    : new DateTime(d.DataCriacao.Year, d.DataCriacao.Month, 1)))
                .ToHashSet();

            var pending = new List<DebitoMembro>();
            for (var month = firstMonth; month <= lastMonth; month = month.AddMonths(1))
            {
                foreach (var userId in recipients)
                {
                    if (!existingKeys.Add((userId, month))) continue;
                    pending.Add(new DebitoMembro
                    {
                        UsuarioId = userId,
                        TipoDoacaoId = type.Id,
                        MesReferencia = month,
                        Valor = type.ValorPadrao,
                        Status = StatusDebito.EmAberto,
                        DataCriacao = DateTime.UtcNow
                    });
                }
            }

            if (pending.Count == 0) continue;
            db.DebitosMembros.AddRange(pending);
            try
            {
                created += await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                // Outra instância pode ter criado a mesma competência no intervalo entre
                // a checagem e o insert. Desanexa o lote; a próxima execução completa os faltantes.
                foreach (var debit in pending) db.Entry(debit).State = EntityState.Detached;
                logger.LogWarning(exception, "Conflito ao gerar débitos mensais do tipo {TypeId}; será reprocessado na próxima execução.", type.Id);
            }
        }

        return created;
    }
}

public sealed class MonthlyDebitGenerationWorker(IServiceScopeFactory scopeFactory, ILogger<MonthlyDebitGenerationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunOnce(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRun = now.Date.AddDays(1).AddMinutes(5);
            await Task.Delay(nextRun - now, stoppingToken);
            await RunOnce(stoppingToken);
        }
    }

    private async Task RunOnce(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var generator = scope.ServiceProvider.GetRequiredService<MonthlyDebitGenerator>();
            var count = await generator.TryEnsureDueDebitsAsync(cancellationToken);
            if (count > 0) logger.LogInformation("Gerados {Count} débitos mensais pendentes.", count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha na geração mensal de débitos. A próxima execução ou o login dos usuários tentará novamente.");
        }
    }
}
