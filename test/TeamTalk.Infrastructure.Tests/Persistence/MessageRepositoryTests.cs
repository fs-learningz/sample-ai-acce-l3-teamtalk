using TeamTalk.Domain.Entities;
using TeamTalk.Infrastructure.Persistence.Repositories;

namespace TeamTalk.Infrastructure.Tests.Persistence;

[Collection(PostgresCollection.Name)]
public class MessageRepositoryTests
{
    private readonly PostgresContainerFixture _fixture;

    public MessageRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SoftDelete_ThenUpdateAsync_RetainsOriginalContentForAudit()
    {
        // arrange
        var channelId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var message = new Message(Guid.CreateVersion7(), channelId, senderId, "original content", DateTime.UtcNow);

        await using (var writeDbContext = _fixture.CreateDbContext())
        {
            var repository = new MessageRepository(writeDbContext);
            await repository.AddAsync(message);
        }

        message.SoftDelete(senderId, DateTime.UtcNow);

        await using (var updateDbContext = _fixture.CreateDbContext())
        {
            var repository = new MessageRepository(updateDbContext);
            await repository.UpdateAsync(message);
        }

        // act
        await using var readDbContext = _fixture.CreateDbContext();
        var readRepository = new MessageRepository(readDbContext);
        var found = await readRepository.GetByIdAsync(message.Id);

        // assert
        Assert.NotNull(found);
        Assert.True(found!.IsDeleted);
        Assert.Equal("original content", found.Content);
    }

    [Fact]
    public async Task GetChannelHistoryAsync_ReturnsMessagesForChannelOnly()
    {
        // arrange
        var channelId = Guid.NewGuid();
        var otherChannelId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var message = new Message(Guid.CreateVersion7(), channelId, senderId, "hello", DateTime.UtcNow);
        var otherMessage = new Message(Guid.CreateVersion7(), otherChannelId, senderId, "elsewhere", DateTime.UtcNow);

        await using (var writeDbContext = _fixture.CreateDbContext())
        {
            var repository = new MessageRepository(writeDbContext);
            await repository.AddAsync(message);
            await repository.AddAsync(otherMessage);
        }

        // act
        await using var readDbContext = _fixture.CreateDbContext();
        var readRepository = new MessageRepository(readDbContext);
        var history = await readRepository.GetChannelHistoryAsync(channelId, null, 50);

        // assert
        Assert.Single(history);
        Assert.Equal(message.Id, history[0].Id);
    }
}
