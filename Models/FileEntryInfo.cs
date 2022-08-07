namespace archie.Models;

public class FileEntryInfo: FileEntry
{
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string[] Tags { get; set; }
    public FileEntryTiers Tier { get; set; }

    public long Size {get;set;}
}


public enum FileEntryTiers
{
    NotSupported,
    Hot,
    Cool,
    Archive
}