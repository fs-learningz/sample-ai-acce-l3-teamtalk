using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;

namespace TeamTalk.Domain.Tests.Entities;

public class WorkspaceMemberTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        // arrange
        var id = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var joinedAtUtc = DateTime.UtcNow;

        // act
        var member = new WorkspaceMember(id, workspaceId, userId, WorkspaceRole.Member, joinedAtUtc);

        // assert
        Assert.Equal(id, member.Id);
        Assert.Equal(workspaceId, member.WorkspaceId);
        Assert.Equal(userId, member.UserId);
        Assert.Equal(WorkspaceRole.Member, member.Role);
        Assert.Equal(joinedAtUtc, member.JoinedAtUtc);
    }

    [Theory]
    [InlineData(WorkspaceRole.Admin, true)]
    [InlineData(WorkspaceRole.Member, false)]
    public void IsAdmin_ReturnsWhetherRoleIsAdmin(WorkspaceRole role, bool expected)
    {
        // arrange
        var member = new WorkspaceMember(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), role, DateTime.UtcNow);

        // act
        var isAdmin = member.IsAdmin();

        // assert
        Assert.Equal(expected, isAdmin);
    }

    [Fact]
    public void ChangeRole_FromMemberToAdmin_UpdatesRole()
    {
        // arrange
        var member = new WorkspaceMember(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, DateTime.UtcNow);

        // act
        member.ChangeRole(WorkspaceRole.Admin);

        // assert
        Assert.Equal(WorkspaceRole.Admin, member.Role);
        Assert.True(member.IsAdmin());
    }

    [Fact]
    public void ChangeRole_FromAdminToMember_UpdatesRole()
    {
        // arrange
        var member = new WorkspaceMember(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Admin, DateTime.UtcNow);

        // act
        member.ChangeRole(WorkspaceRole.Member);

        // assert
        Assert.Equal(WorkspaceRole.Member, member.Role);
        Assert.False(member.IsAdmin());
    }
}
