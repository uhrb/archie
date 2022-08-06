namespace archie.Models;

public class FileOperation
{
    public OperationType Operation { get; set; }
    public FileEntry Source { get; set; }

    public FileEntry Target { get; set; }

    public string SourceHash { get; set; }

    public string TargetHash { get; set; }
}

public enum OperationType
{
    Unknown,
    CopySourceToTarget,
    CopyTargetToSource,
    Conflict
}