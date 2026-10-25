using UnityEngine;

namespace UnderworldSurvivors.Core
{
    public sealed class UnityRandom : IRandom
    {
        public float Value => Random.value;

        public int Range(int minInclusive, int maxExclusive) => Random.Range(minInclusive, maxExclusive);

        public float Range(float minInclusive, float maxInclusive) => Random.Range(minInclusive, maxInclusive);
    }
}
