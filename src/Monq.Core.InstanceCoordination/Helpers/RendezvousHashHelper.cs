using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Monq.Core.InstanceCoordination.Helpers;

/// <summary>
/// Helper methods for distributing work using rendezvous hashing.
/// </summary>
public static class RendezvousHashHelper
{
    /// <summary>
    /// Determines whether the specified instance owns the key.
    /// </summary>
    public static bool IsOwner(
        string key,
        string instanceId,
        IReadOnlyCollection<string> instances)
    {
        string? owner = null;
        ulong maxScore = 0;

        foreach (var candidate in instances)
        {
            var score = CalculateScore(key, candidate);
            if (owner is not null
                && (score < maxScore
                    || (score == maxScore
                        && string.CompareOrdinal(candidate, owner) <= 0)))
                continue;

            owner = candidate;
            maxScore = score;
        }

        return owner == instanceId;
    }

    static ulong CalculateScore(string key, string instanceId)
    {
        var value = Encoding.UTF8.GetBytes($"{key}:{instanceId}");
        var hash = SHA256.HashData(value);
        return BinaryPrimitives.ReadUInt64LittleEndian(hash);
    }
}
