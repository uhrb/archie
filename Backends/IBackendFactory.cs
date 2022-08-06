namespace archie.Backends;

public interface IBackendFactory {
    IBackend GetBySchema(string schema);
}