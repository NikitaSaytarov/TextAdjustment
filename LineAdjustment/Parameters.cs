using CommandLine;

namespace LineAdjustment;

internal sealed record Parameters
{
    [Option(Required = true)]
    public string Text { get; init;}
    [Option(Required = true)]
    public int LineWidth { get; init; }
}