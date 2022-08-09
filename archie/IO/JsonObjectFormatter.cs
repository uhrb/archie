using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace archie.io;

public class JsonObjectFormatter : IObjectFormatter
{
    public string FormatObject(object w)
    {
        return JsonConvert.SerializeObject(w, Formatting.Indented, new [] { new StringEnumConverter() });        
    }

    public T UnformatObject<T>(string? s) {
        return JsonConvert.DeserializeObject<T>(s!, new[] { new StringEnumConverter()});
    }
}