using System.Text.Json;
using System.Text.Json.Serialization;

namespace Portfolio.API.Common;

/// <summary>
/// One set of JSON options for the whole API: camelCase properties, enums as their snake_case
/// string names so responses read the same way the Postgres enums do.
/// </summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));

        return options;
    }
}
