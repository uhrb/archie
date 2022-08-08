namespace archie.Models;

public class FileOperation
{
    public FileDescription Source { get; set; }
    public FileDescription Target { get; set; }
    public OperationType Operation { get; set; }
}

public enum OperationType
{
    Unknown,
    CopyToSource,
    CopyToTarget,
    OverwriteAtSource,
    OverwriteAtTarget,
    DeleteAtSource,
    DeleteAtTarget,
    AppendAtSource,
    AppendAtTarget,
    Skip
}