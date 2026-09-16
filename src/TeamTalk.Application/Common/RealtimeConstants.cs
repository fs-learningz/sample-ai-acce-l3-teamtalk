namespace TeamTalk.Application.Common;

public static class RealtimeConstants
{
    public const string InternalApiKeyHeaderName = "X-Internal-Api-Key";

    public static class Events
    {
        public const string MessageSent = "ReceiveMessage";
        public const string MessageEdited = "MessageEdited";
        public const string MessageDeleted = "MessageDeleted";
    }
}
