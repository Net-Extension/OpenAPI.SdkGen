using NetExtension.OpenAPI.SdkGen.Core;

namespace NetExtension.OpenAPI.SdkGen.Core.Tests;

public class SdkOperationTests
{
    [Theory]
    [InlineData("GetOrder", "GetOrder")]
    [InlineData("getOrder", "GetOrder")]
    [InlineData("get-order", "GetOrder")]
    [InlineData("get_order_by_id", "GetOrderById")]
    [InlineData("Get Order", "GetOrder")]
    [InlineData("get.order", "GetOrder")]
    [InlineData("2ndAttempt", "_2ndAttempt")]
    public void MethodName_DerivesFromDeclaredOperationName(string declared, string expected)
    {
        SdkOperation operation = SdkOperation.Create(declared, "GET", "/orders/{id}");

        Assert.Equal(expected, operation.MethodName);
    }

    [Fact]
    public void ReturnTypeName_IsTheFirstSuccessResponseCarryingABody()
    {
        // Declared out of order on purpose: 200 wins because responses are considered in
        // status-code order, not declaration order.
        SdkOperation operation = SdkOperation.Create(
            "GetOrder",
            "GET",
            "/orders/{id}",
            responses:
            [
                SdkResponse.Create(201, "OrderCreatedDto"),
                SdkResponse.Create(200, "OrderDto"),
            ]);

        Assert.Equal("OrderDto", operation.ReturnTypeName);
    }

    [Fact]
    public void ReturnTypeName_SkipsSuccessResponsesWithoutABody()
    {
        SdkOperation operation = SdkOperation.Create(
            "CreateOrder",
            "POST",
            "/orders",
            responses: [SdkResponse.Create(200, null), SdkResponse.Create(201, "OrderDto")]);

        Assert.Equal("OrderDto", operation.ReturnTypeName);
    }

    [Fact]
    public void Parameters_AreOrderedPathThenQueryThenBody_PreservingDeclarationOrderWithinAKind()
    {
        SdkOperation operation = SdkOperation.Create(
            "UpdateOrderItem",
            "PUT",
            "/orders/{orderId}/items/{itemId}",
            parameters:
            [
                SdkParameter.Create("payload", "UpdateOrderItemRequest", ParameterKind.Body),
                SdkParameter.Create("includeTotals", "bool?", ParameterKind.Query),
                SdkParameter.Create("orderId", "Guid", ParameterKind.Path),
                SdkParameter.Create("dryRun", "bool?", ParameterKind.Query),
                SdkParameter.Create("itemId", "Guid", ParameterKind.Path),
            ]);

        Assert.Equal(
            ["orderId", "itemId", "includeTotals", "dryRun", "payload"],
            operation.Parameters
                .Where(parameter => parameter.Kind != ParameterKind.CancellationToken)
                .Select(parameter => parameter.Name));
    }

    [Fact]
    public void Responses_AreExposedSortedByStatusCode()
    {
        SdkOperation operation = SdkOperation.Create(
            "CreateOrder",
            "POST",
            "/orders",
            responses:
            [
                SdkResponse.Create(500, "ProblemDetails"),
                SdkResponse.Create(201, "OrderDto"),
                SdkResponse.Create(422, "ValidationProblemDetails"),
                SdkResponse.Create(400, "ProblemDetails"),
            ]);

        Assert.Equal(
            [201, 400, 422, 500],
            operation.Responses.Select(response => response.StatusCode));
    }

    [Fact]
    public void Parameters_EndWithACancellationTokenEvenWhenNoneAreDeclared()
    {
        SdkOperation operation = SdkOperation.Create("Ping", "GET", "/ping");

        SdkParameter last = Assert.Single(operation.Parameters);
        Assert.Equal(ParameterKind.CancellationToken, last.Kind);
        Assert.Equal("cancellationToken", last.Name);
        Assert.Equal("CancellationToken", last.TypeName);
    }

    [Fact]
    public void Parameters_ReplaceADeclaredCancellationTokenWithTheSyntheticOne()
    {
        SdkOperation operation = SdkOperation.Create(
            "GetOrder",
            "GET",
            "/orders/{id}",
            parameters:
            [
                SdkParameter.Create("ct", "CancellationToken", ParameterKind.CancellationToken),
                SdkParameter.Create("id", "Guid", ParameterKind.Path),
            ]);

        Assert.Equal(["id", "cancellationToken"], operation.Parameters.Select(p => p.Name));
        Assert.Equal(
            ParameterKind.CancellationToken,
            operation.Parameters[^1].Kind);
    }

    [Fact]
    public void ReturnTypeName_IsStreamWhenTheStreamFlagIsSet_OverridingADeclaredBody()
    {
        SdkOperation operation = SdkOperation.Create(
            "DownloadInvoice",
            "GET",
            "/invoices/{id}/pdf",
            isStream: true,
            responses: [SdkResponse.Create(200, "InvoiceDto")]);

        Assert.Equal("Stream", operation.ReturnTypeName);
        Assert.True(operation.IsStream);
    }

    [Fact]
    public void ReturnTypeName_IsStreamWhenTheStreamFlagIsSetAndNoBodyIsDeclared()
    {
        SdkOperation operation = SdkOperation.Create(
            "DownloadReport", "GET", "/reports/latest", isStream: true);

        Assert.Equal("Stream", operation.ReturnTypeName);
    }

    [Fact]
    public void ReturnTypeName_IsNullWhenOnlyNonSuccessResponsesCarryABody()
    {
        SdkOperation operation = SdkOperation.Create(
            "DeleteOrder",
            "DELETE",
            "/orders/{id}",
            responses: [SdkResponse.Create(204), SdkResponse.Create(404, "ProblemDetails")]);

        Assert.Null(operation.ReturnTypeName);
    }

    [Fact]
    public void ReturnTypeName_IsNullWhenNoResponsesAreDeclared()
    {
        SdkOperation operation = SdkOperation.Create("Ping", "GET", "/ping");

        Assert.Null(operation.ReturnTypeName);
    }

    /// <summary>
    /// The documented fallback: when no operation name is declared, the method name is the
    /// verb followed by the route's segments, where a <c>{token}</c> becomes <c>By</c> plus
    /// the token name.
    /// </summary>
    [Theory]
    [InlineData(null, "GET", "/orders/{id}", "GetOrdersById")]
    [InlineData("", "POST", "/orders", "PostOrders")]
    [InlineData("   ", "DELETE", "/orders/{orderId}/items/{itemId}", "DeleteOrdersByOrderIdItemsByItemId")]
    [InlineData("", "GET", "/", "Get")]
    [InlineData("", "get", "/health-check", "GetHealthCheck")]
    [InlineData("", "OPTIONS", "orders", "OptionsOrders")]
    public void MethodName_FallsBackToVerbAndRoute(
        string? declared, string verb, string route, string expected)
    {
        SdkOperation operation = SdkOperation.Create(declared!, verb, route);

        Assert.Equal(expected, operation.MethodName);
    }
}
