using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace archie.io;

public class JsonObjectFormatter : IOutputFormatter
{
    public string FormatObject(object w)
    {
        return JsonConvert.SerializeObject(w, Formatting.None, new [] { new StringEnumConverter() });        
    }
}