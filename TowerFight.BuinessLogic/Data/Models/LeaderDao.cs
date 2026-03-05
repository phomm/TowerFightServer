using System.ComponentModel.DataAnnotations;
using Riok.Mapperly.Abstractions;

namespace TowerFight.BusinessLogic.Data.Models;

public record LeaderDao 
{
    [Key]
    [MapperIgnore]
    public int Id { get; init; }
    public int Number { get; init; }
    public byte Difficulty { get; init;}
    public int Score { get; init; }
    [StringLength(32)]
    public string Name { get; init; }
    [MapperIgnore]
    public Guid Guid { get; init; }
}