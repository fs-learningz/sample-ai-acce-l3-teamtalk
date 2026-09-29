using System.Net;
using System.Text.Json;
using TeamTalk.Application.Common;
using TeamTalk.Application.Messages.Contracts;
using TeamTalk.Infrastructure.Realtime;

namespace TeamTalk.Infrastructure.Tests.Realtime;

public class HubBroadcastServiceTests
{
    private readonly StubHttpMessageHandler _handler;
    private readonly HubBroadcastService _sut;

    public HubBroadcastServiceTests()
    {
        _handler = new StubHttpMessageHandler();
        var httpClient = new HttpClient(_handler) { BaseAddress = new Uri("http://hub") };
        _sut = new HubBroadcastService(httpClient);
    }

    private static MessageResponse BuildMessage(Guid channelId)
    {
        return new MessageResponse
        {
            Id = Guid.NewGuid(),
            ChannelId = channelId,
            SenderId = Guid.NewGuid(),
            SenderDisplayName = "Alice",
            Content = "hello",
            SentAtUtc = DateTime.UtcNow,
            IsDeleted = false,
        };
    }

    [Fact]
    public async Task BroadcastMessageSentAsync_PostsSentEventToChannelEndpoint()
    {
        // arrange
        var message = BuildMessage(Guid.NewGuid());

        // act
        await _sut.BroadcastMessageSentAsync(message);

        // assert
        var request = Assert.Single(_handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"/internal/broadcast/channels/{message.ChannelId}", request.Path);
        Assert.Equal(RealtimeConstants.Events.MessageSent, request.EventName);
        Assert.Equal(message.Id, request.MessageId);
        Assert.Equal("hello", request.MessageContent);
    }

    [Fact]
    public async Task BroadcastMessageEditedAsync_PostsEditedEventToChannelEndpoint()
    {
        // arrange
        var message = BuildMessage(Guid.NewGuid());

        // act
        await _sut.BroadcastMessageEditedAsync(message);

        // assert
        var request = Assert.Single(_handler.Requests);
        Assert.Equal($"/internal/broadcast/channels/{message.ChannelId}", request.Path);
        Assert.Equal(RealtimeConstants.Events.MessageEdited, request.EventName);
    }

    [Fact]
    public async Task BroadcastMessageDeletedAsync_PostsDeletedEventToChannelEndpoint()
    {
        // arrange
        var message = BuildMessage(Guid.NewGuid());

        // act
        await _sut.BroadcastMessageDeletedAsync(message);

        // assert
        var request = Assert.Single(_handler.Requests);
        Assert.Equal($"/internal/broadcast/channels/{message.ChannelId}", request.Path);
        Assert.Equal(RealtimeConstants.Events.MessageDeleted, request.EventName);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task BroadcastMessageSentAsync_WhenHubReturnsFailure_Throws(HttpStatusCode statusCode)
    {
        // arrange
        _handler.StatusCode = statusCode;
        var message = BuildMessage(Guid.NewGuid());

        // act & assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _sut.BroadcastMessageSentAsync(message));
    }

    private sealed record CapturedRequest(
        HttpMethod Method,
        string Path,
        string EventName,
        Guid MessageId,
        string MessageContent);

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = new();

        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var message = root.GetProperty("message");

            Requests.Add(new CapturedRequest(
                request.Method,
                request.RequestUri!.AbsolutePath,
                root.GetProperty("eventName").GetString()!,
                message.GetProperty("id").GetGuid(),
                message.GetProperty("content").GetString()!));

            return new HttpResponseMessage(StatusCode);
        }
    }
}
