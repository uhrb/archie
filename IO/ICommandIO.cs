namespace archie.io;

public interface ICommandIO {
    Task<int> ReadAsync();
    Task<string?> ReadLineAsync();

    Task WriteAsync(string s);
    Task WriteLineAsync(string s);
}