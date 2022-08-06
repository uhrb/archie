using archie.Models;

namespace archie.Backends;

public interface IBackend {
    public Task<string> GetHash(FileEntry entry);
    public Task<string[]> List(string path);
}