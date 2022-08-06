namespace archie.io;

public interface ICommandIO {
    Task<int> ReadAsync();
    Task<string?> ReadLineAsync();

    Task WriteAsync(object w);
    Task WriteLineAsync(object s);
}