using CommandLine;
using LineAdjustment;
using LineAdjustment.Algorithm;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;


try
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
        .CreateLogger();
    

    Log.Debug("Application start.");

    var application = new Application(args, new LineAdjustmentAlgorithm(), Parser.Default);
    var cts = new CancellationTokenSource();
    var result = await application.Start(cts.Token);

    Log.Information("Result: " + Environment.NewLine + result);
}
catch (Exception e)
{
    Log.Fatal($@"Application failed. Ex: {e}");
}


Console.ReadKey();
Log.Debug("Application shutdown.");