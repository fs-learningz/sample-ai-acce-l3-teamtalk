using TeamTalk.Domain.Common;
using TeamTalk.Domain.Entities;

namespace TeamTalk.Domain.Tests.Entities;

public class MessageTests
{
    [Fact]
    public void Constructor_WithValidContent_SetsProperties()
    {
        var id = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var sentAtUtc = DateTime.UtcNow;

        var message = new Message(id, channelId, senderId, "hello", sentAtUtc);

        Assert.Equal(id, message.Id);
        Assert.Equal(channelId, message.ChannelId);
        Assert.Equal(senderId, message.SenderId);
        Assert.Equal("hello", message.Content);
        Assert.Equal(sentAtUtc, message.SentAtUtc);
        Assert.False(message.IsDeleted);
        Assert.Null(message.EditedAtUtc);
        Assert.Null(message.DeletedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyContent_Throws(string content)
    {
        Assert.Throws<DomainException>(() =>
            new Message(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), content, DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_WithContentExceedingMaxLength_Throws()
    {
        var tooLong = new string('a', Message.MaxContentLength + 1);

        Assert.Throws<DomainException>(() =>
            new Message(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), tooLong, DateTime.UtcNow));
    }

    [Fact]
    public void Edit_WhenNotDeleted_UpdatesContentAndEditedAt()
    {
        var message = new Message(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "original", DateTime.UtcNow);
        var editedAtUtc = DateTime.UtcNow.AddMinutes(1);

        message.Edit("updated", editedAtUtc);

        Assert.Equal("updated", message.Content);
        Assert.Equal(editedAtUtc, message.EditedAtUtc);
    }

    [Fact]
    public void Edit_WhenAlreadyDeleted_Throws()
    {
        var message = new Message(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "original", DateTime.UtcNow);
        message.SoftDelete(Guid.NewGuid(), DateTime.UtcNow);

        Assert.Throws<DomainException>(() => message.Edit("updated", DateTime.UtcNow));
    }

    [Fact]
    public void SoftDelete_WhenNotDeleted_SetsDeletedFields()
    {
        var message = new Message(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "original", DateTime.UtcNow);
        var deletedByUserId = Guid.NewGuid();
        var deletedAtUtc = DateTime.UtcNow.AddMinutes(1);

        message.SoftDelete(deletedByUserId, deletedAtUtc);

        Assert.True(message.IsDeleted);
        Assert.Equal(deletedByUserId, message.DeletedByUserId);
        Assert.Equal(deletedAtUtc, message.DeletedAtUtc);
        Assert.Equal("original", message.Content);
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_Throws()
    {
        var message = new Message(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "original", DateTime.UtcNow);
        message.SoftDelete(Guid.NewGuid(), DateTime.UtcNow);

        Assert.Throws<DomainException>(() => message.SoftDelete(Guid.NewGuid(), DateTime.UtcNow));
    }
}
