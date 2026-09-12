using System.Globalization;

namespace NetExtension.OpenAPI.SdkGen.Core;

/// <summary>
/// The complete description of a client SDK to be generated: the client's name and the
/// operations it exposes. Pure data — this type performs no I/O.
/// </summary>
public sealed record SdkModel
{
    private SdkModel(string clientName, IReadOnlyList<SdkOperation> operations)
    {
        ClientName = clientName;
        Operations = operations;
    }

    /// <summary>The name the generated client is built around.</summary>
    public string ClientName { get; }

    /// <summary>The operations the generated client exposes.</summary>
    public IReadOnlyList<SdkOperation> Operations { get; }

    /// <summary>Creates a model, rejecting inputs that cannot produce a usable client.</summary>
    /// <exception cref="ArgumentException">
    /// The client name is null, empty or whitespace, or the operation list is null or empty.
    /// </exception>
    public static SdkModel Create(string clientName, IReadOnlyList<SdkOperation> operations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
        ArgumentNullException.ThrowIfNull(operations);

        if (operations.Count == 0)
        {
            throw new ArgumentException(
                "A client needs at least one operation to be worth generating.",
                nameof(operations));
        }

        return new SdkModel(clientName, ResolveMethodNameCollisions(operations));
    }

    /// <summary>
    /// Gives every operation a unique method name. The first operation to claim a name keeps
    /// it; later claimants get the lowest free numeric suffix. Resolution walks the list in
    /// order and depends on nothing else, so the same input always yields the same names —
    /// including when a suffixed name would itself collide with one declared explicitly.
    /// </summary>
    private static IReadOnlyList<SdkOperation> ResolveMethodNameCollisions(
        IReadOnlyList<SdkOperation> operations)
    {
        HashSet<string> claimed = new(StringComparer.Ordinal);
        List<SdkOperation> resolved = new(operations.Count);

        foreach (SdkOperation operation in operations)
        {
            string candidate = operation.MethodName;

            for (int suffix = 2; !claimed.Add(candidate); suffix++)
            {
                candidate = operation.MethodName + suffix.ToString(CultureInfo.InvariantCulture);
            }

            resolved.Add(
                candidate == operation.MethodName ? operation : operation.WithMethodName(candidate));
        }

        return resolved;
    }
}
