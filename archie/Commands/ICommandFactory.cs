namespace archie.Commands;

public interface ICommandFactory {
    public Task Initialize(IEnumerable<Type> types);
    public Task<int> Run(string[] args);
}