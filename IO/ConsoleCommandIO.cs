namespace archie.io;

public class ConsoleCommandIO : ICommandIO
{
    public Task<int> ReadAsync()
    {
        return Task.FromResult(Console.Read());
    }

    public Task<string?> ReadLineAsync()
    {
        return Task.FromResult(Console.ReadLine());
    }

    public Task WriteAsync(string s)
    {
        Console.Write(s);
        return Task.CompletedTask;
    }

    public Task WriteLineAsync(string s)
    {
        Console.WriteLine(s);
        return Task.CompletedTask;
    }
}