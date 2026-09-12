using System.Text;

namespace NetExtension.OpenAPI.SdkGen.Core;

/// <summary>
/// A single API operation as declared by the annotations on a handler method.
/// </summary>
public sealed record SdkOperation
{
    private SdkOperation(
        string operationName,
        string verb,
        string routeTemplate,
        bool isStream,
        IReadOnlyList<SdkParameter> parameters,
        IReadOnlyList<SdkResponse> responses)
    {
        OperationName = operationName;
        Verb = verb;
        RouteTemplate = routeTemplate;
        IsStream = isStream;
        MethodName = string.IsNullOrWhiteSpace(operationName)
            ? BuildFallbackMethodName(verb, routeTemplate)
            : CSharpIdentifier.FromWords(operationName);

        // OrderBy is a stable sort, so responses declared with the same status code keep
        // their declaration order and the model stays deterministic.
        Responses = [.. responses.OrderBy(response => response.StatusCode)];
        ReturnTypeName = ResolveReturnTypeName(isStream, Responses);
        Parameters = OrderParameters(parameters);
    }

    /// <summary>
    /// The type name a streaming operation returns. The emitter maps it to
    /// <see cref="System.IO.Stream"/>.
    /// </summary>
    public const string StreamTypeName = "Stream";

    /// <summary>The operation name declared on the handler.</summary>
    public string OperationName { get; }

    /// <summary>The HTTP verb.</summary>
    public string Verb { get; }

    /// <summary>The route template, exactly as declared.</summary>
    public string RouteTemplate { get; }

    /// <summary>Whether the operation streams its response body rather than deserializing it.</summary>
    public bool IsStream { get; }

    /// <summary>
    /// The name this operation's method carries on the generated client, derived from the
    /// declared operation name, or from the verb and route when none was declared.
    /// </summary>
    public string MethodName { get; private init; }

    /// <summary>The declared responses, ordered by status code.</summary>
    public IReadOnlyList<SdkResponse> Responses { get; }

    /// <summary>
    /// The generated method's parameters, in signature order: path, then query, then body,
    /// and always a cancellation token last.
    /// </summary>
    public IReadOnlyList<SdkParameter> Parameters { get; }

    /// <summary>The name of the synthetic cancellation token parameter.</summary>
    public const string CancellationTokenParameterName = "cancellationToken";

    /// <summary>
    /// The type the generated method returns, or <see langword="null"/> when it returns no
    /// value — which the emitter renders as a bare <c>Task</c>.
    /// </summary>
    public string? ReturnTypeName { get; }

    /// <summary>Creates an operation.</summary>
    public static SdkOperation Create(
        string operationName,
        string verb,
        string routeTemplate,
        bool isStream = false,
        IReadOnlyList<SdkParameter>? parameters = null,
        IReadOnlyList<SdkResponse>? responses = null) =>
        new(operationName, verb, routeTemplate, isStream, parameters ?? [], responses ?? []);

    /// <summary>
    /// Puts the declared parameters into signature order and appends the cancellation token.
    /// <c>OrderBy</c> is a stable sort, so parameters of the same kind
    /// keep their declaration order. Any cancellation token that reached the model as a
    /// declared parameter is dropped, so exactly one is ever emitted and it is always last.
    /// </summary>
    private static IReadOnlyList<SdkParameter> OrderParameters(
        IReadOnlyList<SdkParameter> parameters) =>
    [
        .. parameters
            .Where(parameter => parameter.Kind != ParameterKind.CancellationToken)
            .OrderBy(parameter => parameter.Kind),
        SdkParameter.Create(
            CancellationTokenParameterName, "CancellationToken", ParameterKind.CancellationToken),
    ];

    /// <summary>
    /// Returns a copy carrying <paramref name="methodName"/>, used by <see cref="SdkModel"/>
    /// when it resolves collisions between operations.
    /// </summary>
    internal SdkOperation WithMethodName(string methodName) => this with { MethodName = methodName };

    /// <summary>
    /// A streaming operation always returns a stream, even when it also declares a body type:
    /// the caller asked for the bytes, not for a deserialized object. Otherwise the first 2xx
    /// response that declares a body wins, and when no success response carries one the
    /// operation returns nothing.
    /// </summary>
    private static string? ResolveReturnTypeName(
        bool isStream,
        IReadOnlyList<SdkResponse> orderedResponses)
    {
        if (isStream)
        {
            return StreamTypeName;
        }

        return orderedResponses
            .FirstOrDefault(response => response.IsSuccess && response.BodyTypeName is not null)
            ?.BodyTypeName;
    }

    /// <summary>
    /// The fallback naming rule, used when a handler declares no operation name: the verb
    /// followed by each route segment, with a <c>{token}</c> rendered as <c>By</c> plus the
    /// token name. <c>GET /orders/{id}</c> becomes <c>GetOrdersById</c>.
    /// </summary>
    private static string BuildFallbackMethodName(string verb, string routeTemplate)
    {
        // The verb is lowercased so that PascalCasing yields "Get" rather than "GET".
        StringBuilder words = new(verb.ToLowerInvariant());

        foreach (string segment in routeTemplate.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            bool isRouteToken = segment.StartsWith('{') && segment.EndsWith('}');
            words.Append(isRouteToken ? " By " : " ").Append(segment);
        }

        return CSharpIdentifier.FromWords(words.ToString());
    }
}
