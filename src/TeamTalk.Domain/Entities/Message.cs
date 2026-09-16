using TeamTalk.Domain.Common;

namespace TeamTalk.Domain.Entities;

public class Message
{
    public const int MaxContentLength = 4000;

    public Guid Id { get; private set; }

    public Guid ChannelId { get; private set; }

    public Guid SenderId { get; private set; }

    public string Content { get; private set; }

    public DateTime SentAtUtc { get; private set; }

    public DateTime? EditedAtUtc { get; private set; }

    public DateTime? DeletedAtUtc { get; private set; }

    public Guid? DeletedByUserId { get; private set; }

    public Message(Guid id, Guid channelId, Guid senderId, string content, DateTime sentAtUtc)
    {
        ValidateContent(content);

        Id = id;
        ChannelId = channelId;
        SenderId = senderId;
        Content = content.Trim();
        SentAtUtc = sentAtUtc;
    }

    private Message()
    {
        Content = string.Empty;
    }

    public bool IsDeleted
    {
        get { return DeletedAtUtc.HasValue; }
    }

    public void Edit(string newContent, DateTime nowUtc)
    {
        if (IsDeleted)
        {
            throw new DomainException("Cannot edit a deleted message.");
        }

        ValidateContent(newContent);

        Content = newContent.Trim();
        EditedAtUtc = nowUtc;
    }

    public void SoftDelete(Guid deletedByUserId, DateTime nowUtc)
    {
        if (IsDeleted)
        {
            throw new DomainException("Message is already deleted.");
        }

        DeletedAtUtc = nowUtc;
        DeletedByUserId = deletedByUserId;
    }

    private static void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainException("Message content is required.");
        }

        if (content.Length > MaxContentLength)
        {
            throw new DomainException($"Message content cannot exceed {MaxContentLength} characters.");
        }
    }
}
