using System.ComponentModel.DataAnnotations;

namespace TowerFight.BusinessLogic.Data.Models;

public sealed record BanzaiLogDao
{
    [Key]
    public int Id { get; init; }

    public Guid Guid { get; init; }

    [Required]
    [StringLength(1024)]
    public string Entry { get; init; } = null!;

    public DateTimeOffset TimeStamp { get; init; }
}
