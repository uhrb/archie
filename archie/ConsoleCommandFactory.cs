using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace archie.Commands;


public class ConsoleCommandFactory : ICommandFactory
{
    private readonly ILogger<ConsoleCommandFactory> _logger;
    private readonly IServiceProvider _provider;

    private readonly RootCommand _root;

    public ConsoleCommandFactory(ILogger<ConsoleCommandFactory> logger, IServiceProvider provider)
    {
        _logger = logger;
        _provider = provider;
        _root = new RootCommand();
    }

    public Task Initialize(IEnumerable<Type> types)
    {
        var commandTypes = types
            .Where(_ => _.IsClass && _.CustomAttributes.Any(_ => _.AttributeType == typeof(CommandAttribute)));

        foreach (var commandType in commandTypes)
        {
            var attr = (CommandAttribute)Attribute.GetCustomAttribute(commandType, typeof(CommandAttribute))!;
            var cmd = new Command(attr.Name, attr.Description);
            var handler = commandType.GetRuntimeMethods()
                    .FirstOrDefault(_ => _.CustomAttributes.Any(_ => _.AttributeType == typeof(HandlerAttribute)));
            if (handler != null)
            {
                var parameters = handler.GetParameters();

                if (parameters.All(
                    _ => _.CustomAttributes.Any(
                        __ => __.AttributeType.IsSubclassOf(typeof(OptionBaseAttribute)))))
                {
                    foreach (var parameter in parameters)
                    {
                        var attrType = parameter.CustomAttributes.First(_ => _.AttributeType.IsSubclassOf(typeof(OptionBaseAttribute)));
                        var genericType = attrType.AttributeType.GetGenericArguments().First();
                        var instance = (OptionBaseAttribute)Attribute.GetCustomAttribute(parameter, attrType.AttributeType)!;
                        var aliases = instance.Aliases;
                        var description = instance.Description;
                        var isRequired = instance.IsRequired;
                        var defaultValue = (object?)attrType.AttributeType.GetProperty("Default")!.GetValue(instance);
                        var optionConstructed = typeof(Option<>).MakeGenericType(genericType);
                        var optionInstance = (Option)Activator.CreateInstance(optionConstructed, aliases, description)!;
                        optionInstance.IsRequired = isRequired;
                        optionInstance.SetDefaultValue(defaultValue);
                        cmd.AddOption(optionInstance);
                    }
                }
                cmd.SetHandler(_ => UniversalHandler(_, handler, commandType));
            }
            _root.AddCommand(cmd);
            _logger.LogDebug($"Command registered (Name={cmd.Name}; Description={cmd.Description})");
        }
        return Task.CompletedTask;
    }

    private async Task<int> UniversalHandler(InvocationContext context, MethodInfo handler, Type commandType)
    {
        var commandInstance = ActivatorUtilities.CreateInstance(_provider, commandType);
        var optionValues = new List<object?>();
        foreach (var item in context.ParseResult.CommandResult.Command.Options)
        {
            optionValues.Add(context.ParseResult.CommandResult.GetValueForOption(item));
        }
        var result = handler.Invoke(commandInstance, optionValues.ToArray());
        if (result != null)
        {
            if (handler.ReturnType == typeof(Task<int>))
            {
                return await (Task<int>)result;
            }
            else if (handler.ReturnType.IsSubclassOf(typeof(Task)))
            {
                await (Task)result;
            }
        }

        return 0;
    }

    public Task<int> Run(string[] args)
    {
        _logger.LogDebug($"Starting execution");
        return _root.InvokeAsync(args);
    }
}