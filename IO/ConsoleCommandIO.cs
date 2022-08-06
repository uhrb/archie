namespace archie.io;

public class ConsoleCommandIO : ICommandIO
{
    private IOutputFormatter _formatter;

    public ConsoleCommandIO(IOutputFormatter formatter)
    {
        _formatter = formatter;
    }

    public Task<int> ReadAsync()
    {
        return Task.FromResult(Console.Read());
    }

    public Task<string?> ReadLineAsync()
    {
        return Task.FromResult(Console.ReadLine());
    }

    public Task WriteAsync(object s)
    {
        Console.Write(_formatter.FormatObject(s));
        return Task.CompletedTask;
    }

    public Task WriteLineAsync(object s)
    {
        Console.WriteLine(_formatter.FormatObject(s));
        return Task.CompletedTask;
    }
}