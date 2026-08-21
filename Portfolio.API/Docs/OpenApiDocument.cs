using System.Reflection;

namespace Portfolio.API.Docs;

/// <summary>
/// The hand-authored <c>openapi.yaml</c>, embedded in the assembly and read once. There is no
/// generator behind it: three frontends need a contract, and a checked-in file keeps that
/// contract reviewable in a pull request instead of scattered across attributes.
/// </summary>
public static class OpenApiDocument
{
    private const string ResourceName = "Portfolio.API.Docs.openapi.yaml";

    private static readonly Lazy<string> Document = new(Read, LazyThreadSafetyMode.ExecutionAndPublication);

    public const string ContentType = "application/yaml";

    /// <summary>The spec as YAML.</summary>
    public static string Yaml => Document.Value;

    private static string Read()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{ResourceName}' is missing. Check the EmbeddedResource item in Portfolio.API.csproj.");

        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
