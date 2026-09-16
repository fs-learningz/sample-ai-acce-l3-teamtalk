using TeamTalk.Domain.Common;

namespace TeamTalk.Domain.Entities;

public class Workspace
{
    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Workspace(Guid id, string name, Guid createdByUserId, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Workspace name is required.");
        }

        Id = id;
        Name = name.Trim();
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    private Workspace()
    {
        Name = string.Empty;
    }
}
