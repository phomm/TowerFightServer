using TowerFight.BusinessLogic.Data;
using TowerFight.BusinessLogic.Data.Models;
using TowerFight.BusinessLogic.Data.RedisCache;
using TowerFight.BusinessLogic.Mappers;
using TowerFight.BusinessLogic.Models;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace TowerFight.BusinessLogic.Services
{
    public interface ILeadersService
    {
        Task<IEnumerable<LeaderResponse>> GetLeadersAsync(CancellationToken cancellationToken);
        Task<OneOf<InsertHighscoreSuccess, NameOwnedByAnotherAccountError, OperationOkNoChanges>> InsertHighscoreAsync(Leader leader, Guid? guid, CancellationToken cancellationToken);
    }

    public class LeadersService(IDbContextFactory<AppDbContext> _dbContextFactory, IRedisCache _redisCache) : ILeadersService
    {
        private const int MaxLeadersCount = 10;
        private const int AnyDifficulty = -1;
        private const string cacheSet = nameof(Leader);
        private const string key = nameof(Leader);        
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public async Task<IEnumerable<LeaderResponse>> GetLeadersAsync(CancellationToken cancellationToken)
        {   
            var leadersDict = await _redisCache.GetAsync<Dictionary<byte, List<Leader>>>(cacheSet, key);
            if (leadersDict is not null)
            {
                return LeaderMapper.MapList(MapDict(leadersDict));
            }

            if (!await IsDbAliveAsync(cancellationToken))
                return [];

            leadersDict = await GetFromDb(cancellationToken);

            await InsertToRedis(leadersDict);

            return LeaderMapper.MapList(MapDict(leadersDict));
        }

        public async Task<OneOf<InsertHighscoreSuccess, NameOwnedByAnotherAccountError, OperationOkNoChanges>> InsertHighscoreAsync(Leader candidate, Guid? guid, CancellationToken cancellationToken)
        {
            if (!await IsDbAliveAsync(cancellationToken))
                return await InsertFallBackToRedis(candidate, guid, cancellationToken);

            var existingLeader = await GetByNameDifficultyAsync(candidate.Name, AnyDifficulty, cancellationToken);
            
            if (existingLeader is not null && existingLeader.Guid != guid)
            {
                return MakeNameError(candidate.Name);
            }            

            var newGuid = guid ?? Guid.NewGuid();
            existingLeader = await GetByNameDifficultyAsync(candidate.Name, candidate.Difficulty, cancellationToken);
            if (existingLeader is not null)
            {
                if (existingLeader.Score >= candidate.Difficulty)
                    return new OperationOkNoChanges("The operation is succesful, but no changes to Leaders board");

                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                await context.Leaders
                    .Where(x => x.Id == existingLeader.Id)
                    .ExecuteUpdateAsync(c => c
                        .SetProperty(x => x.Score, candidate.Score)
                        .SetProperty(x => x.TimeStamp, DateTimeOffset.UtcNow), cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                var leaderDao = new LeaderDao
                {
                    Difficulty = candidate.Difficulty,
                    Score = candidate.Score,
                    Name = candidate.Name,
                    Guid = newGuid
                };

                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                await context.Leaders.AddAsync(leaderDao, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }

            var leadersDict = await GetFromDb(cancellationToken);
            await InsertToRedis(leadersDict);

            return new InsertHighscoreSuccess(newGuid);
        }

        private async Task<OneOf<InsertHighscoreSuccess, NameOwnedByAnotherAccountError, OperationOkNoChanges>> InsertFallBackToRedis(Leader candidate, Guid? guid, CancellationToken cancellationToken)
        {
            var newGuid = guid ?? new Guid();
            var leadersDict = await GetFromRedis();
            if (leadersDict.Count == 0)
            {                
                await _redisCache.AddAsync(cacheSet, key, new[]{MakeNewLeader()}.ToDictionary(x => x.Difficulty, x => new List<Leader>{x}));
                return new InsertHighscoreSuccess(newGuid);
            }

            if (leadersDict.SelectMany(x => x.Value).Any(x => x.Name == candidate.Name && x.Guid != guid))
            {
                return MakeNameError(candidate.Name);
            }
            
            if (leadersDict.TryGetValue(candidate.Difficulty, out var leadersList))
            {
                leadersList.Sort((x, y) => x.Score.CompareTo(y.Score));
                var index = leadersList.FindIndex(x => x.Score < candidate.Score);
                leadersList.Insert(index, candidate);
                //leadersDict[candidate.Difficulty] = leadersList;
            }
            else
            {
                leadersDict.Add(candidate.Difficulty, [MakeNewLeader()]);
            }
            
            await InsertToRedis(leadersDict);
            return new InsertHighscoreSuccess(newGuid);

            Leader MakeNewLeader() => 
                new()
                {   
                    Difficulty = candidate.Difficulty,
                    Name = candidate.Name,
                    //Number = 1,
                    Guid = newGuid,
                    Score = candidate.Score
                };
        }

        private async Task InsertToRedis(Dictionary<byte, List<Leader>> leadersDict)
        {
            // TODO later might be switched to distibuted lock in Redis, using RedLock nuget
            await _semaphore.WaitAsync();
            try
            {
                await _redisCache.RemoveAsync(cacheSet, key);
                await _redisCache.AddAsync(cacheSet, key, leadersDict);
            }
            finally
            {                
                _semaphore.Release();
            }
        }
        
        private async Task<LeaderDao?> GetByNameDifficultyAsync(string name, int difficulty, CancellationToken cancellationToken)
        {
            await using var dbcontext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await dbcontext.Leaders
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Name == name && (difficulty == AnyDifficulty || l.Difficulty == difficulty), cancellationToken);
        }

        private async Task<Dictionary<byte, List<Leader>>> GetFromDb(CancellationToken cancellationToken)
        {
            List<LeaderDao> leaderDaos = null!;
            await Task.WhenAll(
                Task.Run(async () =>
                    {
                        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                        leaderDaos = await context.Leaders
                            .AsNoTracking()
                            // need to use subquery as groupBy+selectMany can't be traversed to sql, but seems subquery is optimal
                            .Select(l => l.Difficulty)
                            .Distinct()
                            .SelectMany(difficulty => context.Leaders
                                .Where(l => l.Difficulty == difficulty)
                                .OrderByDescending(l => l.Score)
                                .Take(MaxLeadersCount))
                            //.GroupBy(l => l.Difficulty)
                            //.SelectMany(g => g.OrderByDescending(l => l.Score).Take(MaxLeadersCount))                            
                            //.OrderBy(l => l.Difficulty).ThenByDescending(l => l.Score)
                            .ToListAsync(cancellationToken);
                            
                            // alternative with rawsql
                            //var leaders = await context.Leaders
                            //.FromSqlRaw(@"
                            //    SELECT * FROM (
                            //        SELECT *, ROW_NUMBER() OVER (PARTITION BY Difficulty ORDER BY Score DESC) as Rank
                            //        FROM Leaders
                            //    ) AS t
                            //    WHERE Rank <= {0}", MaxLeadersCount)
                            //.ToListAsync(cancellationToken);
                    }, cancellationToken)
                );
            
            return LeaderMapper.MapDaos(leaderDaos).GroupBy(x => x.Difficulty).ToDictionary(x => x.Key, x => x.ToList());
        }

        private async Task<Dictionary<byte, List<Leader>>> GetFromRedis()
        {
            return await _redisCache.GetAsync<Dictionary<byte, List<Leader>>>(cacheSet, key) ?? [];
        }

        private async Task<bool> IsDbAliveAsync(CancellationToken ct)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
            return await db.Database.CanConnectAsync(ct);
        }

        private static IEnumerable<Leader> MapDict(IDictionary<byte, List<Leader>> leadersDict) => 
            leadersDict.SelectMany(x => x.Value).OrderBy(x => x.Difficulty).ThenByDescending(x => x.Score);

        private static NameOwnedByAnotherAccountError MakeNameError(string Name) =>
            new($"Name {Name} does not belong to your account, use different name");
    }
}
