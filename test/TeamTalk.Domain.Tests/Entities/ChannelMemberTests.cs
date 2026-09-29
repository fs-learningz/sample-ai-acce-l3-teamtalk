using TeamTalk.Domain.Entities;

namespace TeamTalk.Domain.Tests.Entities;

public class ChannelMemberTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        // arrange
        var id = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var joinedAtUtc = DateTime.UtcNow;

        // act
        var member = new ChannelMember(id, channelId, userId, joinedAtUtc);

        // assert
        Assert.Equal(id, member.Id);
        Assert.Equal(channelId, member.ChannelId);
        Assert.Equal(userId, member.UserId);
        Assert.Equal(joinedAtUtc, member.JoinedAtUtc);
    }
}
