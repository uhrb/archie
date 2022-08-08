namespace archie.Models;

public class DiffDescription
{
    public Uri Source {get;set;}
    public Uri Target {get;set;}
    public string Created {get;set;}
    public List<FileDescription> OnlyOnTarget {get;set;}
    public List<FileDescription> OnlyOnSource {get;set;}
    public List<FileDescription[]> Both {get;set;}
}