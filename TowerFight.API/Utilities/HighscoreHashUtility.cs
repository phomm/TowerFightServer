using System.Security.Cryptography;
using System.Text;
using TowerFight.API.Models;
using TowerFight.BusinessLogic.Data.Config;

namespace TowerFight.API.Utilities;

public class HighscoreHashUtility
{
    private readonly string _salt;

    public HighscoreHashUtility(IConfiguration configuration)
    {
        var signingSettings = configuration.GetSection("SigningSettings").Get<SigningSettings>();
        _salt = signingSettings?.HighscoreHashSalt ?? throw new InvalidOperationException("HighscoreHashSalt not configured");
    }

    public bool IsValid(InsertHighscoreRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Hash))
        {
            return false;
        }

        var parts = new List<string>
        {
            $"{nameof(request.Difficulty)}={request.Difficulty}",
            $"{nameof(request.Score)}={request.Score}",
            $"{nameof(request.Name)}={request.Name}",
            $"Salt={_salt}",
        };

        if (request.Guid.HasValue)
        {
            parts.Add($"{nameof(request.Guid)}={request.Guid}");
        }

        parts.Sort();
        var payload = string.Join(":", parts);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var hashBytes = SHA256.HashData(payloadBytes);
        var expectedHash = Convert.ToHexString(hashBytes);

        return string.Equals(expectedHash, request.Hash, StringComparison.OrdinalIgnoreCase);
    }
}
