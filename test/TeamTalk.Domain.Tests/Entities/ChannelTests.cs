using TeamTalk.Domain.Common;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;

namespace TeamTalk.Domain.Tests.Entities;

public class ChannelTests
{
    [Fact]
    public void Constructor_WithValidName_SetsProperties()
    {
        // arrange
        var id = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();
        var createdAtUtc = DateTime.UtcNow;

        // act
        var channel = new Channel(id, workspaceId, "  general  ", ChannelType.Public, createdByUserId, createdAtUtc);

        // assert
        Assert.Equal(id, channel.Id);
        Assert.Equal(workspaceId, channel.WorkspaceId);
        Assert.Equal("general", channel.Name);
        Assert.Equal(ChannelType.Public, channel.Type);
        Assert.True(channel.IsPublic());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_Throws(string name)
    {
        // act & assert
        Assert.Throws<DomainException>(() =>
            new Channel(Guid.NewGuid(), Guid.NewGuid(), name, ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_WithNameExceedingMaxLength_Throws()
    {
        // arrange
        var tooLong = new string('a', Channel.MaxNameLength + 1);

        // act & assert
        Assert.Throws<DomainException>(() =>
            new Channel(Guid.NewGuid(), Guid.NewGuid(), tooLong, ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public void IsPublic_WhenPrivate_ReturnsFalse()
    {
        // act
        var channel = new Channel(Guid.NewGuid(), Guid.NewGuid(), "secret", ChannelType.Private, Guid.NewGuid(), DateTime.UtcNow);

        // assert
        Assert.False(channel.IsPublic());
    }
}
