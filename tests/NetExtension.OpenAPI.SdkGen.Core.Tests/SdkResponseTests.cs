using NetExtension.OpenAPI.SdkGen.Core;

namespace NetExtension.OpenAPI.SdkGen.Core.Tests;

public class SdkResponseTests
{
    [Theory]
    [InlineData(100, false)] // Informational is not success, and is below the 2xx range.
    [InlineData(199, false)]
    [InlineData(200, true)]
    [InlineData(204, true)]
    [InlineData(299, true)]
    [InlineData(300, false)]
    [InlineData(404, false)]
    [InlineData(500, false)]
    public void IsSuccess_IsTrueOnlyInsideThe2xxRange(int statusCode, bool expected)
    {
        SdkResponse response = SdkResponse.Create(statusCode);

        Assert.Equal(expected, response.IsSuccess);
    }

    [Fact]
    public void Create_DefaultsToNoBody()
    {
        SdkResponse response = SdkResponse.Create(204);

        Assert.Null(response.BodyTypeName);
    }

    /// <summary>
    /// A 1xx response never becomes the return type, even when it declares a body: only a
    /// 2xx response can.
    /// </summary>
    [Fact]
    public void ReturnTypeName_IgnoresInformationalResponsesThatDeclareABody()
    {
        SdkOperation operation = SdkOperation.Create(
            "UpgradeConnection",
            "GET",
            "/stream",
            responses: [SdkResponse.Create(101, "UpgradeDto")]);

        Assert.Null(operation.ReturnTypeName);
    }
}
