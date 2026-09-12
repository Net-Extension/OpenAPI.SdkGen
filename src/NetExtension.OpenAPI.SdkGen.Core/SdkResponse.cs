namespace NetExtension.OpenAPI.SdkGen.Core;

/// <summary>
/// One response an operation declares: a status code and, optionally, the type of its body.
/// </summary>
public sealed record SdkResponse
{
    private SdkResponse(int statusCode, string? bodyTypeName)
    {
        StatusCode = statusCode;
        BodyTypeName = bodyTypeName;
    }

    /// <summary>The declared HTTP status code.</summary>
    public int StatusCode { get; }

    /// <summary>The body type name, or <see langword="null"/> when the response has no body.</summary>
    public string? BodyTypeName { get; }

    /// <summary>Whether this is a 2xx response.</summary>
    public bool IsSuccess => StatusCode is >= 200 and <= 299;

    /// <summary>Creates a response.</summary>
    public static SdkResponse Create(int statusCode, string? bodyTypeName = null) =>
        new(statusCode, bodyTypeName);
}
