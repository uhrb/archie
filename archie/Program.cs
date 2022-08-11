using System.CommandLine;
using archie.Backends;
using archie.Commands;
using archie.io;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace archie;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var runlog = $"/home/uh/github/archie/logs/log.log";
        if (File.Exists(runlog))
        {
            File.Delete(runlog);
        }
        var services = new ServiceCollection()
            .AddLogging(_ =>
            {
                var serilog = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo
                    .File(runlog,
                          outputTemplate: "{Timestamp:yyyy-MM-ddTHH:mm:ss.fffzzz} [{Level}] {Scope} {Message} {NewLine}")
                    .CreateLogger();
                _.SetMinimumLevel(LogLevel.Trace)
                .AddSerilog(logger: serilog);

            })
            .AddSingleton<IBackendFactory, BackendFactory>()
            .AddSingleton<IObjectFormatter, JsonObjectFormatter>()
            .AddSingleton<ICommandFactory, ConsoleCommandFactory>()
            .AddSingleton<IStreams>(_ => new Streams(Console.In, Console.Out, Console.Error))
            .BuildServiceProvider();

        var factory = services.GetRequiredService<ICommandFactory>();
        await factory.Initialize(typeof(Program).Assembly.GetTypes());
        return await factory.Run(args);
    }
}