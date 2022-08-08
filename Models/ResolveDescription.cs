namespace archie.Models;

public class ResolveDescription
{
    public Uri Source { get; set; }
    public Uri Target { get; set; }
    public string Created { get; set; }
    public List<FileOperation> Operations { get; set; }
}