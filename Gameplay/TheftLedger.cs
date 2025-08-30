public static class TheftLedger
{
    public static float StolenValue { get; private set; }

    public static void Add(float amount)
    {
        if (amount > 0f) StolenValue += amount;
    }

    public static void Reset() => StolenValue = 0f;
}
