namespace NetExtension.OpenAPI.SdkGen.Core;

/// <summary>
/// One parameter of a generated client method.
/// </summary>
public sealed record SdkParameter
{
    private SdkParameter(string name, string typeName, ParameterKind kind)
    {
        Name = name;
        TypeName = typeName;
        Kind = kind;
    }

    /// <summary>The parameter name as it appears in the generated signature.</summary>
    public string Name { get; }

    /// <summary>The parameter's C# type name.</summary>
    public string TypeName { get; }

    /// <summary>How the parameter reaches the API.</summary>
    public ParameterKind Kind { get; }

    /// <summary>Creates a parameter.</summary>
    public static SdkParameter Create(string name, string typeName, ParameterKind kind) =>
        new(name, typeName, kind);
}
