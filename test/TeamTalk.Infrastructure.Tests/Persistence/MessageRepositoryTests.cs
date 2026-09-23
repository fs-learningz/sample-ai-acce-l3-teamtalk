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
        var channelId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var message = new Message(Guid.CreateVersion7(), channelId, senderId, "original content", DateTime.UtcNow);

        await using (var writeDbContext = _fixture.CreateDbContext())
        {
            var repository = new MessageRepository(writeDbContext);
            await repository.AddAsync(message, CancellationToken.None);
        }

        message.SoftDelete(senderId, DateTime.UtcNow);

        await using (var updateDbContext = _fixture.CreateDbContext())
        {
            var repository = new MessageRepository(updateDbContext);
            await repository.UpdateAsync(message, CancellationToken.None);
        }

        await using var readDbContext = _fixture.CreateDbContext();
        var readRepository = new MessageRepository(readDbContext);
        var found = await readRepository.GetByIdAsync(message.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.True(found!.IsDeleted);
        Assert.Equal("original content", found.Content);
    }

    [Fact]
    public async Task GetChannelHistoryAsync_ReturnsMessagesForChannelOnly()
    {
        var channelId = Guid.NewGuid();
        var otherChannelId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var message = new Message(Guid.CreateVersion7(), channelId, senderId, "hello", DateTime.UtcNow);
        var otherMessage = new Message(Guid.CreateVersion7(), otherChannelId, senderId, "elsewhere", DateTime.UtcNow);

        await using (var writeDbContext = _fixture.CreateDbContext())
        {
            var repository = new MessageRepository(writeDbContext);
            await repository.AddAsync(message, CancellationToken.None);
            await repository.AddAsync(otherMessage, CancellationToken.None);
        }

        await using var readDbContext = _fixture.CreateDbContext();
        var readRepository = new MessageRepository(readDbContext);
        var history = await readRepository.GetChannelHistoryAsync(channelId, null, 50, CancellationToken.None);

        Assert.Single(history);
        Assert.Equal(message.Id, history[0].Id);
    }
}
