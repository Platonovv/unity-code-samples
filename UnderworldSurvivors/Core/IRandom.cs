namespace UnderworldSurvivors.Core
{
    public interface IRandom
    {
        float Value { get; }

        int Range(int minInclusive, int maxExclusive);
        float Range(float minInclusive, float maxInclusive);
    }
}
