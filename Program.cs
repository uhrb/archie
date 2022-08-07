using System.CommandLine;
using archie.Backends;
using archie.Commands;
using archie.io;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace archie;

public static class Program
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
            .AddSingleton<TextWriter>(_ => Console.Out)
            .AddSingleton<TextReader>(_ => Console.In)
            .BuildServiceProvider();

        var rootCommand = new RootCommand();

        RegisterCommands(services, rootCommand);
        return await rootCommand.InvokeAsync(args);
    }

    public static void RegisterCommands(IServiceProvider provider, RootCommand root)
    {
        var iface = typeof(ICommand);
        var instances = typeof(Program)
            .Assembly
            .GetTypes()
            .Where(_ => _.IsClass && !_.IsAbstract && iface.IsAssignableFrom(_))
            .Select(_ => ActivatorUtilities.CreateInstance(provider, _))
            .Cast<ICommand>();
        foreach (var instance in instances)
        {
            instance.Register(root);
        }
    }
}