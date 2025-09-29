using System;

public static class MoneyFormatter
{
    const long K = 1_000;
    const long M = 1_000_000;
    const long B = 1_000_000_000;
    const long T = 1_000_000_000_000;

    public static string Short(long v)
    {
        if (v >= 999 * T) return (v / T).ToString("N0") + "T";
        if (v >= T) return WithSuffix(v, T, "T");
        if (v >= B) return WithSuffix(v, B, "B");
        if (v >= M) return WithSuffix(v, M, "M");
        if (v >= K) return WithSuffix(v, K, "K");
        return v.ToString("N0");
    }

    static string WithSuffix(long v, long unit, string suf)
    {
        if (v % unit == 0)                // кратные — без десятых: 5K, 2M, 3B, ...
            return (v / unit).ToString("N0") + suf;

        double d = v / (double)unit;      // иначе 1.6K, 12.34M, ...
        string s = d.ToString("0.##");
        return s + suf;
    }
}
