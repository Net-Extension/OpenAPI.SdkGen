namespace NetExtension.OpenAPI.SdkGen.Core;

/// <summary>
/// How a parameter reaches the API. The numeric order is the order the parameters appear in
/// the generated method signature.
/// </summary>
public enum ParameterKind
{
    /// <summary>Substituted into a <c>{token}</c> in the route template.</summary>
    Path = 0,

    /// <summary>Appended to the query string.</summary>
    Query = 1,

    /// <summary>Sent as the request body.</summary>
    Body = 2,

    /// <summary>
    /// The synthetic cancellation token every generated method ends with. The scanner never
    /// produces one of these; the model appends it.
    /// </summary>
    CancellationToken = 3,
}
