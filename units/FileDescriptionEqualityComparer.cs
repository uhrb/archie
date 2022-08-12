using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using archie.Models;

namespace units;

internal class FileDescriptionEqualityComparer : IEqualityComparer<FileDescription>
{
    public bool Equals(FileDescription? x, FileDescription? y)
    {
        return (x!.FullName == y!.FullName) && (x!.MD5 == y!.MD5);
    }

    public int GetHashCode([DisallowNull] FileDescription obj)
    {
        return obj.GetHashCode();
    }
}
