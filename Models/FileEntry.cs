namespace archie.Models;

public class FileEntry
{
    public string RelativeName { get; set; }
    public string BasePath { get; set; }

    public Uri FullName { get; set; }
}