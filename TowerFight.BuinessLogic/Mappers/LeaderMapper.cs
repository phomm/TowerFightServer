using TowerFight.BusinessLogic.Data.Models;
using TowerFight.BusinessLogic.Models;
using Riok.Mapperly.Abstractions;

namespace TowerFight.BusinessLogic.Mappers;

[Mapper]
public partial class LeaderMapper
{
    public partial Leader Map(LeaderDao dao);
    public partial LeaderResponse Map(Leader dao);

    public static IReadOnlyList<Leader> MapDaos(IReadOnlyList<LeaderDao> input)
    {
        var mapper = new LeaderMapper();
        return [.. input.Select(mapper.Map)];
    }

    public static IReadOnlyList<LeaderResponse> MapList(IEnumerable<Leader> input)
    {
        var mapper = new LeaderMapper();
        return [.. input.Select(mapper.Map).Select((l, i) => l with { Number = i + 1 })];
    }
}