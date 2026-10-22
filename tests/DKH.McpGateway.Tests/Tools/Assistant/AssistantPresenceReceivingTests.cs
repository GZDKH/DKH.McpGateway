using DKH.AssistantService.Contracts.Assistant.Models.V1;
using DKH.AssistantService.Contracts.Assistant.Services.V1;
using DKH.McpGateway.Application.Tools.Assistant;
using Google.Protobuf;

namespace DKH.McpGateway.Tests.Tools.Assistant;

public sealed class AssistantPresenceReceivingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OmittedFinancialValuesRemainUnknownThroughActualUnaryAndStreamingToolsAsync(bool streaming)
    {
        var card = new ProductCardModel { Id = "product-a", Code = "TEA-001", Name = "Tea", Brand = "Origin" };
        var received = ProductCardModel.Parser.ParseFrom(card.Clone().ToByteArray());
        var output = await ExecuteAsync(received, streaming);
        using var json = JsonDocument.Parse(output);
        var product = json.RootElement.GetProperty("products")[0];
        product.GetProperty("price").ValueKind.Should().Be(JsonValueKind.Null);
        product.GetProperty("currency").ValueKind.Should().Be(JsonValueKind.Null);
        product.GetProperty("inStock").ValueKind.Should().Be(JsonValueKind.Null);
        product.GetProperty("oldPrice").ValueKind.Should().Be(JsonValueKind.Null);
        product.GetProperty("id").GetString().Should().Be("product-a");
        product.GetProperty("code").GetString().Should().Be("TEA-001");
        product.GetProperty("name").GetString().Should().Be("Tea");
        product.GetProperty("brand").GetString().Should().Be("Origin");
    }

    [Theory]
    [InlineData("price")]
    [InlineData("currency")]
    [InlineData("in_stock")]
    public async Task IndependentExplicitDefaultsAndClearingSurviveActualBothToolsAsync(string name)
    {
        var field = ProductCardModel.Descriptor.FindFieldByName(name);
        field.HasPresence.Should().BeTrue("the actual published SDK must expose presence");
        var card = new ProductCardModel { Id = "product-a", Code = "TEA-001", Name = "Tea", OldPrice = 0 };
        object supplied = name switch
        {
            "price" => 0d,
            "currency" => "",
            "in_stock" => false,
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };
        field.Accessor.SetValue(card, supplied);
        var parsed = ProductCardModel.Parser.ParseFrom(card.Clone().ToByteArray());
        foreach (var streaming in new[] { false, true })
        {
            using var json = JsonDocument.Parse(await ExecuteAsync(parsed, streaming));
            var product = json.RootElement.GetProperty("products")[0];
            foreach (var candidate in new[] { "price", "currency", "in_stock" })
            {
                var jsonName = candidate == "in_stock" ? "inStock" : candidate;
                var value = product.GetProperty(jsonName);
                if (candidate != name)
                {
                    value.ValueKind.Should().Be(JsonValueKind.Null);
                }
                else if (name == "price")
                {
                    value.GetDouble().Should().Be(0);
                }
                else if (name == "currency")
                {
                    value.GetString().Should().BeEmpty();
                }
                else
                {
                    value.GetBoolean().Should().BeFalse();
                }
            }

            product.GetProperty("oldPrice").GetDouble().Should().Be(0);
        }

        field.Accessor.Clear(parsed);
        var cleared = ProductCardModel.Parser.ParseFrom(parsed.ToByteArray());
        field.Accessor.HasValue(cleared).Should().BeFalse();
        foreach (var streaming in new[] { false, true })
        {
            using var json = JsonDocument.Parse(await ExecuteAsync(cleared, streaming));
            var product = json.RootElement.GetProperty("products")[0];
            product.GetProperty("price").ValueKind.Should().Be(JsonValueKind.Null);
            product.GetProperty("currency").ValueKind.Should().Be(JsonValueKind.Null);
            product.GetProperty("inStock").ValueKind.Should().Be(JsonValueKind.Null);
            product.GetProperty("oldPrice").GetDouble().Should().Be(0);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ExplicitDefaultsAndNondefaultsRemainSuppliedThroughActualBothToolsAsync(
        bool streaming, bool nondefault)
    {
        var card = new ProductCardModel
        {
            Id = "product-a",
            Code = "TEA-001",
            Name = "Tea",
            Price = nondefault ? 19.25 : 0,
            OldPrice = nondefault ? 30 : 0,
            Currency = nondefault ? "CNY" : "",
            InStock = nondefault,
            MatchReason = "",
        };
        var parsed = ProductCardModel.Parser.ParseFrom(card.Clone().ToByteArray());
        parsed.HasPrice.Should().BeTrue();
        parsed.HasCurrency.Should().BeTrue();
        parsed.HasInStock.Should().BeTrue();
        using var json = JsonDocument.Parse(await ExecuteAsync(parsed, streaming));
        var product = json.RootElement.GetProperty("products")[0];
        product.GetProperty("price").GetDouble().Should().Be(nondefault ? 19.25 : 0);
        product.GetProperty("oldPrice").GetDouble().Should().Be(nondefault ? 30 : 0);
        product.GetProperty("currency").GetString().Should().Be(nondefault ? "CNY" : "");
        product.GetProperty("inStock").GetBoolean().Should().Be(nondefault);
        product.GetProperty("matchReason").GetString().Should().BeEmpty();
        product.GetProperty("code").GetString().Should().Be("TEA-001");
    }

    private static async Task<string> ExecuteAsync(ProductCardModel card, bool streaming)
    {
        using var cts = new CancellationTokenSource();
        var auth = ApiKeyContextMocks.FullAccess();
        var client = Substitute.For<AssistantChatService.AssistantChatServiceClient>();
        string output;
        if (streaming)
        {
            client.ChatStream(Arg.Any<ChatRequest>(), Arg.Any<Metadata>(),
                    Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
                .Returns(GrpcTestHelpers.CreateServerStreamingCall(new ChatStreamChunk { Product = card }));
            output = await AssistantChatStreamTool.ExecuteAsync(auth, client, "storefront-a", "session-a", "Tea",
                catalogSeo: "catalog-a", cancellationToken: cts.Token);
            _ = client.Received(1).ChatStream(
                Arg.Is<ChatRequest>(r => r.StorefrontId == "storefront-a" && r.SessionId == "session-a" && r.CatalogSeo == "catalog-a"),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), cts.Token);
        }
        else
        {
            var response = new ChatResponse { Text = "Tea" };
            response.Products.Add(card);
            client.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<Metadata>(),
                    Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
                .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));
            output = await AssistantChatTool.ExecuteAsync(auth, client, "storefront-a", "session-a", "Tea",
                catalogSeo: "catalog-a", cancellationToken: cts.Token);
            _ = client.Received(1).ChatAsync(
                Arg.Is<ChatRequest>(r => r.StorefrontId == "storefront-a" && r.SessionId == "session-a" && r.CatalogSeo == "catalog-a"),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), cts.Token);
        }

        auth.Received().EnsurePermission(McpPermissions.Write);
        return output;
    }
}
