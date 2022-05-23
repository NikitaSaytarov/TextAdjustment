namespace LineAdjustment.Algorithm;

internal sealed record Line
{
    public string Adjusted { get; set; }
    public int Index { get; init; }
    public LineTransform Transformation { get; init; }

    public override string ToString()
    {
        return Adjusted;
    }
}