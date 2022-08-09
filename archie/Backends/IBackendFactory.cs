namespace archie.Backends;

public interface IBackendFactory {
    IBackend GetByScheme(string schema);
}