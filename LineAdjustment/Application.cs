using System.Diagnostics;
using CommandLine;
using LineAdjustment.Algorithm;
using Microsoft.Extensions.Configuration;

namespace LineAdjustment;

internal sealed class Application : IDisposable
{
    private readonly TaskCompletionSource<string> _tcs = new();

    private readonly Parser _parameterParser;
    private readonly LineAdjustmentAlgorithm _algorithm;
    private Parameters? _parameters;

    public Application(string[] args, LineAdjustmentAlgorithm algorithm, Parser parser)
    {
        if (args == null) throw new ArgumentNullException(nameof(args));
        _algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
        _parameterParser = parser ?? throw new ArgumentNullException(nameof(parser));

        Initialize(args);
    }

    public Task<string> Start(CancellationToken cancellationToken = default)
    {
        try
        {
            Debug.Assert(_parameters != null, nameof(_parameters) + " != null");

            var text = _parameters.Text;
            var lineWidth = _parameters.LineWidth;

            var result = _algorithm.Transform(text, lineWidth, cancellationToken);
            _tcs.SetResult(result);
        }
        catch (Exception e)
        {
            _tcs.TrySetException(e);
        }

        return _tcs.Task;
    }

    private void Initialize(string[] args)
    {
        if (args.Any())
        {
            _parameterParser.ParseArguments<Parameters>(args)
                .WithParsed(ParametersParsed)
                .WithNotParsed(HandleParametersParsingError);
        }
        else
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var parameters = configuration
                .GetSection("Parameters")
                .Get<Parameters>();

            ParametersParsed(parameters);
        }
    }

    private void ParametersParsed(Parameters? parameters)
    {
        _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }

    private void HandleParametersParsingError(IEnumerable<Error> errors)
    {
        if (errors == null) throw new ArgumentNullException(nameof(errors));
        _tcs.TrySetException(new Exception($@"Incorrect input parameters: {string.Join(",", errors)}"));
    }

    public void Dispose()
    {
        _algorithm.Dispose();
        _parameterParser.Dispose();
        _tcs.TrySetResult(string.Empty);
    }
}