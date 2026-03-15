using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TowerFight.BusinessLogic.Data;
using TowerFight.BusinessLogic.Data.Models;
using TowerFight.BusinessLogic.Data.RedisCache;
using TowerFight.BusinessLogic.Models;

namespace TowerFight.BusinessLogic.Services
{
    public interface ICacheService
    {
        Task ClearCache(CancellationToken cancellationToken);
        Task<bool> PushToDb(CancellationToken cancellationToken);
    }

    public class CacheService(IRedisCache _redisCache, IDbContextFactory<AppDbContext> _dbContextFactory, ILogger<CacheService> _logger) : ICacheService
    {
        const string leaderKey = nameof(Leader);            
        const string leaderCacheSet = nameof(Leader);

        public async Task ClearCache(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Clearing cache for key: {LeaderKey} in set: {LeaderCacheSet}", leaderKey, leaderCacheSet);
            await _redisCache.RemoveAsync(leaderCacheSet, leaderKey);
            _logger.LogInformation("Cache cleared for key: {LeaderKey}", leaderKey);
        }

        public async Task<bool> PushToDb(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Pushing cache data to database.");
            if (!await IsDbAliveAsync(cancellationToken))
            {
                _logger.LogWarning("Database is not available. Aborting PushToDb.");
                return false;
            }

            using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            if ((await context.Leaders.AsNoTracking().FirstOrDefaultAsync(cancellationToken)) is not null)
            {
                _logger.LogInformation("Leaders already exist in database. Skipping push.");
                return false;
            }

            var data = await GetFromRedis();
            _logger.LogInformation("Retrieved {Count} leader entries from Redis.", data.Sum(x => x.Value.Count));
            var leaders = data.SelectMany(x => x.Value).Select(x => 
                new LeaderDao
                {
                    Difficulty = x.Difficulty,
                    Score = x.Score,
                    Name = x.Name,
                    Guid = x.Guid
                });

            await context.Leaders.AddRangeAsync(leaders, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Pushed leader data to database.");
            return true;
        }

        private async Task<Dictionary<byte, List<Leader>>> GetFromRedis()
        {
            _logger.LogDebug("Getting leaders from Redis cache.");
            var result = await _redisCache.GetAsync<Dictionary<byte, List<Leader>>>(leaderCacheSet, leaderKey) ?? [];
            _logger.LogDebug("Retrieved {Count} leader groups from Redis.", result.Count);
            return result;
        }
        
        private async Task<bool> IsDbAliveAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Checking if database is alive.");
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            _logger.LogDebug("Database connectivity check result: {CanConnect}", canConnect);
            return canConnect;
        }
    }    
}
