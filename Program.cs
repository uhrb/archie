using System.CommandLine;
using archie.Backends;
using archie.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace archie;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {

        var services = new ServiceCollection()
            .AddLogging(_ =>
            {
                _.AddConsole();
            })
            .AddSingleton<IBackendFactory, BackendFactory>()
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