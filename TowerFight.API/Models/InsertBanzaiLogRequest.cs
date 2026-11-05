using System.ComponentModel.DataAnnotations;

namespace TowerFight.API.Models;

/// <summary>Request payload for inserting a Banzai log entry.</summary>
public sealed record InsertBanzaiLogRequest
{
    /// <summary>Identifier associated with the log entry.</summary>
    [Required]
    public required Guid Guid { get; init; }

    /// <summary>Log entry text, up to 1024 characters.</summary>
    [Required]
    [StringLength(1024)]
    public required string Entry { get; init; }
}
