using archie.Models;

namespace archie.Backends;

public interface IBackend {
    public Task<string> GetHash(string entry);
    public Task<string[]> List(string path);
}