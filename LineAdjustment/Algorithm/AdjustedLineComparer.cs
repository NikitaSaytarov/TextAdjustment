namespace LineAdjustment.Algorithm;

internal sealed class AdjustedLineComparer : IComparer<Line>
{
    public int Compare(Line x, Line y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (ReferenceEquals(null, y)) return 1;
        if (ReferenceEquals(null, x)) return -1;
        return x.Index > y.Index ? 1 : -1;
    }
}