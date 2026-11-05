using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TowerFight.BusinessLogic.Data;
using TowerFight.BusinessLogic.Data.Models;

namespace TowerFight.BusinessLogic.Services;

public interface IBanzaiLogService
{
    Task InsertAsync(Guid guid, string entry, CancellationToken cancellationToken);
}

public sealed class BanzaiLogService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ILogger<BanzaiLogService> logger) : IBanzaiLogService
{
    public async Task InsertAsync(Guid guid, string entry, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating database context to insert Banzai log for {Guid}.", guid);
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.BanzaiLogs.AddAsync(new BanzaiLogDao
        {
            Guid = guid,
            Entry = entry,
            TimeStamp = DateTimeOffset.UtcNow
        }, cancellationToken);

        logger.LogInformation("Saving Banzai log for {Guid} to the database.", guid);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Banzai log for {Guid} saved to the database.", guid);
    }
}
