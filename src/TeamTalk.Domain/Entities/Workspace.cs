using TeamTalk.Domain.Common;

namespace TeamTalk.Domain.Entities;

public class Workspace
{
    public const int MaxNameLength = 50;
    public const int MaxDescriptionLength = 200;

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public byte[]? IconData { get; private set; }

    public string? IconContentType { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public Workspace(Guid id, string name, Guid createdByUserId, DateTime createdAtUtc)
    {
        Id = id;
        Name = string.Empty;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;

        SetName(name);
    }

    private Workspace()
    {
        Name = string.Empty;
    }

    public bool HasIcon => IconData is not null;

    public void UpdateDetails(string name, string? description, DateTime nowUtc)
    {
        SetName(name);
        SetDescription(description);
        UpdatedAtUtc = nowUtc;
    }

    public void SetIcon(byte[] iconData, string iconContentType, DateTime nowUtc)
    {
        if (iconData.Length is 0)
        {
            throw new DomainException("Workspace icon data is required.");
        }

        if (string.IsNullOrWhiteSpace(iconContentType))
        {
            throw new DomainException("Workspace icon content type is required.");
        }

        IconData = iconData;
        IconContentType = iconContentType;
        UpdatedAtUtc = nowUtc;
    }

    public void RemoveIcon(DateTime nowUtc)
    {
        IconData = null;
        IconContentType = null;
        UpdatedAtUtc = nowUtc;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Workspace name is required.");
        }

        var trimmed = name.Trim();

        if (trimmed.Length > MaxNameLength)
        {
            throw new DomainException($"Workspace name cannot exceed {MaxNameLength} characters.");
        }

        Name = trimmed;
    }

    private void SetDescription(string? description)
    {
        var trimmed = description?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            Description = null;

            return;
        }

        if (trimmed.Length > MaxDescriptionLength)
        {
            throw new DomainException($"Workspace description cannot exceed {MaxDescriptionLength} characters.");
        }

        Description = trimmed;
    }
}
