using TowerFight.BusinessLogic.Data.RedisCache;
using TowerFight.BusinessLogic.Models;

namespace TowerFight.BusinessLogic.Services
{
    public interface ICacheService
    {
        Task ClearCache(CancellationToken cancellationToken);
    }

    public class CacheService(IRedisCache _redisCache) : ICacheService
    {
        public async Task ClearCache(CancellationToken cancellationToken)
        {
            const string leaderKey = nameof(Leader);            
            const string leaderCacheSet = nameof(Leader);
            
            await _redisCache.RemoveAsync(leaderCacheSet, leaderKey);            
        }
    }
}
