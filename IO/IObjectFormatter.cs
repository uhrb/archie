namespace archie.io;

public interface IObjectFormatter {
    string FormatObject(object w);

    T UnformatObject<T>(string? s);
}