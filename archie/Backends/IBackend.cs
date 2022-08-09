using archie.Models;

namespace archie.Backends;

public interface IBackend {
    public string Scheme {get;}
    public Task<IEnumerable<FileDescription>> List(Uri path);
    public Task<FileDescription> GetFileDescription(Uri path);
    Task<Stream> OpenRead(Uri entry);
    Task<Stream> OpenWrite(Uri uri, FileMode mode);
    Uri ComputePath(Uri basePath, string[] fragments);
    string[] GetRelativeFragments(Uri basePath, Uri fullName);
}