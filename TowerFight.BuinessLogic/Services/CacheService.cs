using Microsoft.EntityFrameworkCore;
using TowerFight.BusinessLogic.Data;
using TowerFight.BusinessLogic.Data.Models;
using TowerFight.BusinessLogic.Data.RedisCache;
using TowerFight.BusinessLogic.Models;

namespace TowerFight.BusinessLogic.Services
{
    public interface ICacheService
    {
        Task ClearCache(CancellationToken cancellationToken);
        Task PushToDb(CancellationToken cancellationToken);
    }

    public class CacheService(IRedisCache _redisCache, IDbContextFactory<AppDbContext> _dbContextFactory) : ICacheService
    {
        const string leaderKey = nameof(Leader);            
        const string leaderCacheSet = nameof(Leader);

        public async Task ClearCache(CancellationToken cancellationToken)
        {
            await _redisCache.RemoveAsync(leaderCacheSet, leaderKey);            
        }

        public async Task PushToDb(CancellationToken cancellationToken)
        {
            if (!await IsDbAliveAsync(cancellationToken))
                return;

            using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            if ((await context.Leaders.AsNoTracking().FirstOrDefaultAsync(cancellationToken)) is not null)
                return;

            var data = await GetFromRedis();
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
        }

        private async Task<Dictionary<byte, List<Leader>>> GetFromRedis()
        {
            return await _redisCache.GetAsync<Dictionary<byte, List<Leader>>>(leaderCacheSet, leaderKey) ?? [];
        }
        
        private async Task<bool> IsDbAliveAsync(CancellationToken cancellationToken)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await db.Database.CanConnectAsync(cancellationToken);
        }
    }    
}
