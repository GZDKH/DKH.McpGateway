using DKH.AssistantService.Contracts.Assistant.Models.V1;
using DKH.AssistantService.Contracts.Assistant.Services.V1;
using DKH.McpGateway.Application.Tools.Assistant;
using DKH.McpGateway.Application.Tools.Payment;
using Google.Protobuf.WellKnownTypes;
using AssistantChannel = DKH.AssistantService.Contracts.Assistant.Models.V1.Channel;

namespace DKH.McpGateway.Tests.Tools.Assistant;

public sealed class AssistantToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
    private readonly AssistantChatService.AssistantChatServiceClient _client =
        Substitute.For<AssistantChatService.AssistantChatServiceClient>();

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void AssistantToolSurface_DefinesOnlyConversationalSpecTools()
    {
        string[] expected =
        [
            "assistant_chat",
            "assistant_chat_stream",
            "assistant_get_suggestions",
            "assistant_clear_session",
        ];

        var actual = typeof(GetPaymentTool).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "DKH.McpGateway.Application.Tools.Assistant")
            .SelectMany(t => t.GetMethods())
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Where(a => a.GetType().Name == "McpServerToolAttribute")
            .Select(a => (string?)a.GetType().GetProperty("Name")?.GetValue(a))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        actual.Should().BeEquivalentTo(expected);
        actual.Should().NotContain(name => name.Contains("config", StringComparison.OrdinalIgnoreCase));
        actual.Should().NotContain(name => name.Contains("operator", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AssistantChat_MissingMessage_ReturnsErrorAsync()
    {
        var result = await AssistantChatTool.ExecuteAsync(
            _auth,
            _client,
            storefrontId: "sf-1",
            sessionId: "session-1",
            message: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("message is required");
    }

    [Fact]
    public async Task AssistantChat_HappyPath_SetsRequestAndMapsResponseWithoutUserIdAsync()
    {
        var response = new ChatResponse
        {
            Text = "Try this tea",
            Intent = Intent.Recommend,
            FromCache = true,
            SessionMessageCount = 4,
        };
        response.Products.Add(new ProductCardModel
        {
            Id = "product-1",
            Code = "tea-001",
            Name = "Jasmine",
            Description = "Green tea",
            Price = 12.5,
            OldPrice = 15,
            Currency = "USD",
            ImageUrl = "/tea.png",
            ProductUrl = "/products/tea-001",
            Rating = 4.8,
            ReviewCount = 7,
            InStock = true,
            Brand = "TeaCo",
            MatchReason = "floral",
        });
        response.Products[0].KeySpecs.Add(new ProductSpecSummaryModel { Name = "origin", Value = "CN" });
        response.Suggestions.Add(new SuggestionModel { Text = "Show cheaper options", Type = SuggestionType.SuggestionRefine });

        _client.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

        var json = Parse(await AssistantChatTool.ExecuteAsync(
            _auth,
            _client,
            storefrontId: "sf-1",
            sessionId: "session-1",
            message: "recommend tea",
            channel: "web",
            locale: "ru",
            userId: "user-123",
            currency: "USD",
            catalogSeo: "tea"));

        json.GetProperty("text").GetString().Should().Be("Try this tea");
        json.GetProperty("intent").GetString().Should().Be("Recommend");
        json.GetProperty("fromCache").GetBoolean().Should().BeTrue();
        json.GetProperty("sessionMessageCount").GetInt32().Should().Be(4);
        json.GetProperty("products")[0].GetProperty("oldPrice").GetDouble().Should().Be(15);
        json.GetProperty("products")[0].GetProperty("keySpecs")[0].GetProperty("name").GetString().Should().Be("origin");
        json.GetProperty("suggestions")[0].GetProperty("type").GetString().Should().Be("SuggestionRefine");
        json.GetRawText().Should().NotContain("user-123");
        json.GetRawText().Should().NotContain("userId");

        _ = _client.Received(1).ChatAsync(
            Arg.Is<ChatRequest>(r =>
                r.StorefrontId == "sf-1" &&
                r.SessionId == "session-1" &&
                r.Message == "recommend tea" &&
                r.Channel == AssistantChannel.Web &&
                r.Locale == "ru" &&
                r.UserId == "user-123" &&
                r.Currency == "USD" &&
                r.CatalogSeo == "tea"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssistantGetSuggestions_HappyPath_MapsSuggestionsAsync()
    {
        var response = new GetSuggestionsResponse();
        response.Suggestions.Add(new SuggestionModel { Text = "Ask about sizes", Type = SuggestionType.SuggestionExplore });
        _client.GetSuggestionsAsync(Arg.Any<GetSuggestionsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

        var json = Parse(await AssistantGetSuggestionsTool.ExecuteAsync(
            _auth,
            _client,
            storefrontId: "sf-1",
            sessionId: "session-1",
            channel: "api",
            locale: "en"));

        json.GetProperty("suggestions")[0].GetProperty("text").GetString().Should().Be("Ask about sizes");
        _ = _client.Received(1).GetSuggestionsAsync(
            Arg.Is<GetSuggestionsRequest>(r =>
                r.StorefrontId == "sf-1" &&
                r.SessionId == "session-1" &&
                r.Channel == AssistantChannel.Api &&
                r.Locale == "en"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssistantChatStream_HappyPath_AggregatesChunksAsync()
    {
        _client.ChatStream(Arg.Any<ChatRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateServerStreamingCall(
                new ChatStreamChunk { TextDelta = "Hello " },
                new ChatStreamChunk { TextDelta = "there" },
                new ChatStreamChunk
                {
                    Product = new ProductCardModel
                    {
                        Id = "product-1",
                        Code = "tea-001",
                        Name = "Jasmine",
                        Price = 12.5,
                        Currency = "USD",
                        InStock = true,
                    },
                },
                new ChatStreamChunk { Suggestion = new SuggestionModel { Text = "Compare", Type = SuggestionType.SuggestionAction } },
                new ChatStreamChunk
                {
                    Meta = new ChatStreamMeta
                    {
                        Intent = Intent.Compare,
                        SessionMessageCount = 2,
                        ProductsFound = 1,
                    },
                }));

        var json = Parse(await AssistantChatStreamTool.ExecuteAsync(
            _auth,
            _client,
            storefrontId: "sf-1",
            sessionId: "session-1",
            message: "compare"));

        json.GetProperty("text").GetString().Should().Be("Hello there");
        json.GetProperty("products")[0].GetProperty("id").GetString().Should().Be("product-1");
        json.GetProperty("suggestions")[0].GetProperty("type").GetString().Should().Be("SuggestionAction");
        json.GetProperty("intent").GetString().Should().Be("Compare");
        json.GetProperty("sessionMessageCount").GetInt32().Should().Be(2);
        json.GetProperty("productsFound").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task AssistantClearSession_HappyPath_ReturnsConfirmationAsync()
    {
        _client.ClearSessionAsync(Arg.Any<ClearSessionRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

        var json = Parse(await AssistantClearSessionTool.ExecuteAsync(
            _auth,
            _client,
            storefrontId: "sf-1",
            sessionId: "session-1"));

        json.GetProperty("cleared").GetBoolean().Should().BeTrue();
        json.GetProperty("storefrontId").GetString().Should().Be("sf-1");
        json.GetProperty("sessionId").GetString().Should().Be("session-1");
        _ = _client.Received(1).ClearSessionAsync(
            Arg.Is<ClearSessionRequest>(r => r.StorefrontId == "sf-1" && r.SessionId == "session-1"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }
}
