using TowerFight.BusinessLogic.Data.Models;
using TowerFight.BusinessLogic.Models;
using Riok.Mapperly.Abstractions;

namespace TowerFight.BusinessLogic.Mappers;

[Mapper]
public partial class LeaderMapper
{
    public partial Leader Map(LeaderDao dao);

    public static IReadOnlyList<Leader> Map(IReadOnlyList<LeaderDao> daos)
    {
        var mapper = new LeaderMapper();
        return [.. daos.Select(mapper.Map)];
    }
}