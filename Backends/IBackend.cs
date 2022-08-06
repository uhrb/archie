using archie.Models;

namespace archie.Backends;

public interface IBackend {
    public Task<string> GetHash(FileEntry entry);
    public Task<IEnumerable<FileEntry>> List(string path);
}