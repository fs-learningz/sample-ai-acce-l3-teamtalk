using System.Net.Http.Json;
using TeamTalk.Application.Common;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Common.Models;
using TeamTalk.Application.Messages.Contracts;

namespace TeamTalk.Infrastructure.Realtime;

public class HubBroadcastService : IMessageBroadcastService
{
    private readonly HttpClient _httpClient;

    public HubBroadcastService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task BroadcastMessageSentAsync(MessageResponse message, CancellationToken cancellationToken)
    {
        await BroadcastAsync(RealtimeConstants.Events.MessageSent, message, cancellationToken);
    }

    public async Task BroadcastMessageEditedAsync(MessageResponse message, CancellationToken cancellationToken)
    {
        await BroadcastAsync(RealtimeConstants.Events.MessageEdited, message, cancellationToken);
    }

    public async Task BroadcastMessageDeletedAsync(MessageResponse message, CancellationToken cancellationToken)
    {
        await BroadcastAsync(RealtimeConstants.Events.MessageDeleted, message, cancellationToken);
    }

    private async Task BroadcastAsync(string eventName, MessageResponse message, CancellationToken cancellationToken)
    {
        var payload = new BroadcastMessageRequest
        {
            EventName = eventName,
            Message = message,
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/internal/broadcast/channels/{message.ChannelId}",
            payload,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
