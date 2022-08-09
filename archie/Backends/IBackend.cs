using archie.Models;

namespace archie.Backends;

public interface IBackend {
    public Task<IEnumerable<FileDescription>> List(Uri path);
    public Task<FileDescription> GetFileDescription(Uri path);

    Stream OpenRead(Uri entry);
    Stream OpenWrite(Uri uri);
    Uri ComputePath(Uri basePath, string[] fragments);
    string[] GetRelativeFragments(Uri basePath, Uri fullName);
}