using TowerAutobattler.Content;

namespace TowerAutobattler.Battle;

public sealed class DeterministicRandom(ulong seed) : IDeterministicRandom
{
    private ulong _state = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;

    private ulong Next()
    {
        _state ^= _state >> 12;
        _state ^= _state << 25;
        _state ^= _state >> 27;
        return _state * 2685821657736338717UL;
    }

    public int NextInt(int minimumInclusive, int maximumExclusive)
    {
        if (maximumExclusive <= minimumInclusive) return minimumInclusive;
        return minimumInclusive + (int)(Next() % (uint)(maximumExclusive - minimumInclusive));
    }

    public float NextFloat() => (Next() >> 40) / (float)(1UL << 24);
}
