namespace archie.Models;

public abstract class FileDescription
{
    public Uri FullName { get; set; }
    public string MD5 { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string[] Tags { get; set; }
    public FileEntryTiers Tier { get; set; }

    public long Size { get; set; }

    public abstract string GetRelativeName(Uri basePath); 
}

public enum FileEntryTiers
{
    NotSupported,
    Hot,
    Cool,
    Archive
}