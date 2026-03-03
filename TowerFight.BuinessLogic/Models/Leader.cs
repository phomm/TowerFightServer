namespace TowerFight.BusinessLogic.Models;

public record Leader
{
    public int Number { get; init; }
    public string Name { get; init; }
    public byte Difficulty { get; init;}
}