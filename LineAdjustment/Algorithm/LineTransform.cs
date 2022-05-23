namespace LineAdjustment.Algorithm;

internal sealed record LineTransform
{
    public int WordsTextLength { get; init; }
    public string[] Words { get; init; }

    public int LineBreakWidth { get; init; }
}