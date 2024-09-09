using System;
using System.IO;
using Microsoft.Extensions.Logging;
using msbuildls.LanguageServer.Extensions;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;


// while (!Debugger.IsAttached)
// {
//     Debugger.Launch();
// }

var logFilePath = Path.Combine(Path.GetTempPath(), "msbuildls.log");

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
    .MinimumLevel.Verbose()
    .CreateLogger();

Log.Logger.Information("Startup");

var server = await LanguageServer.From(
    options =>
        options
            .WithInput(Console.OpenStandardInput())
            .WithOutput(Console.OpenStandardOutput())
            .ConfigureLogging(logBuilder => logBuilder
                .AddSerilog(Log.Logger)
                .AddLanguageProtocolLogging()
                .SetMinimumLevel(LogLevel.Debug)
            )
            .AddTextDocumentHandlers()
            .WithServices(services =>
            {
                services
                    .AddSymbolResolutionServices();
            })
).ConfigureAwait(false);

await server.WaitForExit.ConfigureAwait(false);