using archie.Models;

namespace archie.Backends;

public interface IBackend {
    public Task<string> GetHash(FileEntry entry);
    public Task<IEnumerable<FileEntry>> List(Uri path);
    FileEntryInfo GetEntryInfo(FileEntry target);

    Stream OpenRead(FileEntry entry);
    Stream OpenRead(Uri uri);
}