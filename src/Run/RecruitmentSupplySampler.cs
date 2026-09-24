using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TowerAutobattler.Battle;
using TowerAutobattler.Project;

namespace TowerAutobattler.Run;

public static class RecruitmentSupplySampler
{
    // Draw a tier first, then a hero, without replacement. Exhaustion only redistributes
    // weight among nonempty tiers that were already legal at this stage.
    public static IReadOnlyList<string> Draw(CompiledRecruitmentSupply supply, IEnumerable<string> candidates,
        ImmutableArray<int> weights, int count, string identity)
    {
        var random = new DeterministicRandom(System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(
            SHA256.HashData(Encoding.UTF8.GetBytes(identity))));
        var buckets = Enumerable.Range(1, weights.Length).Select(tier => candidates.Distinct(StringComparer.Ordinal)
            .Where(id => supply.TierOf(id) == tier).Order(StringComparer.Ordinal).ToList()).ToArray();
        var result = new List<string>();
        while (result.Count < count)
        {
            var total = Enumerable.Range(0, weights.Length).Sum(index => buckets[index].Count > 0 ? weights[index] : 0);
            if (total == 0) break;
            var roll = random.NextInt(0, total);
            for (var tier = 0; tier < weights.Length; tier++)
            {
                var weight = buckets[tier].Count > 0 ? weights[tier] : 0;
                if (roll >= weight) { roll -= weight; continue; }
                var index = random.NextInt(0, buckets[tier].Count);
                result.Add(buckets[tier][index]);
                buckets[tier].RemoveAt(index);
                break;
            }
        }
        return result;
    }
}
