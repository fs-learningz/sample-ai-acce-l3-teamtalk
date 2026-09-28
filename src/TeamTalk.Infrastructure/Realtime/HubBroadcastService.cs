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

    public async Task BroadcastMessageSentAsync(MessageResponse message)
    {
        await BroadcastAsync(RealtimeConstants.Events.MessageSent, message);
    }

    public async Task BroadcastMessageEditedAsync(MessageResponse message)
    {
        await BroadcastAsync(RealtimeConstants.Events.MessageEdited, message);
    }

    public async Task BroadcastMessageDeletedAsync(MessageResponse message)
    {
        await BroadcastAsync(RealtimeConstants.Events.MessageDeleted, message);
    }

    private async Task BroadcastAsync(string eventName, MessageResponse message)
    {
        var payload = new BroadcastMessageRequest
        {
            EventName = eventName,
            Message = message,
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/internal/broadcast/channels/{message.ChannelId}",
            payload);

        response.EnsureSuccessStatusCode();
    }
}
