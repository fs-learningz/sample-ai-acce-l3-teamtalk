using TeamTalk.Domain.Entities;

namespace TeamTalk.Domain.Tests.Entities;

public class ChannelUserStateTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsIdentifiers()
    {
        // arrange
        var id = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // act
        var state = new ChannelUserState(id, channelId, userId);

        // assert
        Assert.Equal(id, state.Id);
        Assert.Equal(channelId, state.ChannelId);
        Assert.Equal(userId, state.UserId);
    }

    [Fact]
    public void Constructor_WithValidArguments_DefaultsToNotPinnedNotMutedAndNeverRead()
    {
        // act
        var state = new ChannelUserState(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // assert
        Assert.False(state.IsPinned);
        Assert.False(state.IsMuted);
        Assert.Null(state.LastReadAtUtc);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void SetPreferences_WithFlags_SetsPinnedAndMuted(bool isPinned, bool isMuted)
    {
        // arrange
        var state = new ChannelUserState(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // act
        state.SetPreferences(isPinned, isMuted);

        // assert
        Assert.Equal(isPinned, state.IsPinned);
        Assert.Equal(isMuted, state.IsMuted);
    }

    [Fact]
    public void SetPreferences_WhenPreviouslyPinnedAndMuted_ClearsBothFlags()
    {
        // arrange
        var state = new ChannelUserState(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        state.SetPreferences(true, true);

        // act
        state.SetPreferences(false, false);

        // assert
        Assert.False(state.IsPinned);
        Assert.False(state.IsMuted);
    }

    [Fact]
    public void MarkRead_WithTimestamp_SetsLastReadAtUtc()
    {
        // arrange
        var state = new ChannelUserState(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var nowUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // act
        state.MarkRead(nowUtc);

        // assert
        Assert.Equal(nowUtc, state.LastReadAtUtc);
    }

    [Fact]
    public void MarkRead_WhenAlreadyRead_OverwritesLastReadAtUtc()
    {
        // arrange
        var state = new ChannelUserState(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var first = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var second = first.AddHours(1);
        state.MarkRead(first);

        // act
        state.MarkRead(second);

        // assert
        Assert.Equal(second, state.LastReadAtUtc);
    }
}
