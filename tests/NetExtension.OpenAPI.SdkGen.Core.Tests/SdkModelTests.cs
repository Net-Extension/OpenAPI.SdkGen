using NetExtension.OpenAPI.SdkGen.Core;

namespace NetExtension.OpenAPI.SdkGen.Core.Tests;

public class SdkModelTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsBlankClientName(string? clientName)
    {
        SdkOperation[] operations = [SdkOperation.Create("GetOrder", "GET", "/orders/{id}")];

        // ThrowsAny, not Throws: the null case surfaces as ArgumentNullException, which is a
        // subclass, and xunit's Throws<T> demands an exact type match.
        ArgumentException error = Assert.ThrowsAny<ArgumentException>(
            () => SdkModel.Create(clientName!, operations));

        Assert.Equal("clientName", error.ParamName);
    }

    [Fact]
    public void Create_SuffixesCollidingMethodNamesDeterministically()
    {
        // Three different declared spellings all reduce to the identifier "GetOrder".
        SdkOperation[] operations =
        [
            SdkOperation.Create("GetOrder", "GET", "/orders/{id}"),
            SdkOperation.Create("get-order", "GET", "/legacy/orders/{id}"),
            SdkOperation.Create("Get Order", "GET", "/v3/orders/{id}"),
            SdkOperation.Create("ListOrders", "GET", "/orders"),
        ];

        SdkModel model = SdkModel.Create("OrdersClient", operations);

        Assert.Equal(
            ["GetOrder", "GetOrder2", "GetOrder3", "ListOrders"],
            model.Operations.Select(operation => operation.MethodName));
    }

    [Fact]
    public void Create_ResolvesCollisionsIdenticallyOnEveryRun()
    {
        SdkOperation[] operations =
        [
            SdkOperation.Create("GetOrder", "GET", "/orders/{id}"),
            SdkOperation.Create("get_order", "GET", "/legacy/orders/{id}"),
        ];

        SdkModel first = SdkModel.Create("OrdersClient", operations);
        SdkModel second = SdkModel.Create("OrdersClient", operations);

        Assert.Equal(
            first.Operations.Select(operation => operation.MethodName),
            second.Operations.Select(operation => operation.MethodName));
    }

    [Fact]
    public void Create_RejectsEmptyOperationList()
    {
        ArgumentException error = Assert.ThrowsAny<ArgumentException>(
            () => SdkModel.Create("OrdersClient", []));

        Assert.Equal("operations", error.ParamName);
    }

    [Fact]
    public void Create_RejectsNullOperationList()
    {
        ArgumentException error = Assert.ThrowsAny<ArgumentException>(
            () => SdkModel.Create("OrdersClient", null!));

        Assert.Equal("operations", error.ParamName);
    }
}
