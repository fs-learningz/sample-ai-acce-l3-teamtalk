using TeamTalk.Domain.Common;
using TeamTalk.Domain.Entities;

namespace TeamTalk.Domain.Tests.Entities;

public class WorkspaceTests
{
    [Fact]
    public void Constructor_WithValidName_SetsProperties()
    {
        var id = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();
        var createdAtUtc = DateTime.UtcNow;

        var workspace = new Workspace(id, "  Engineering  ", createdByUserId, createdAtUtc);

        Assert.Equal(id, workspace.Id);
        Assert.Equal("Engineering", workspace.Name);
        Assert.Equal(createdByUserId, workspace.CreatedByUserId);
        Assert.Equal(createdAtUtc, workspace.CreatedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_Throws(string name)
    {
        Assert.Throws<DomainException>(() =>
            new Workspace(Guid.NewGuid(), name, Guid.NewGuid(), DateTime.UtcNow));
    }
}
