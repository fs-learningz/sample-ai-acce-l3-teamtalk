using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeamTalk.Infrastructure.Email;

namespace TeamTalk.Infrastructure.Tests.Email;

public class SmtpEmailSenderTests
{
    private static SmtpEmailSender CreateSut(Dictionary<string, string?> settings, TestLogger logger)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new SmtpEmailSender(configuration, logger);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendAsync_WhenSmtpHostIsNotConfigured_LogsEmailInsteadOfSending(string? host)
    {
        // arrange
        var logger = new TestLogger();
        var sut = CreateSut(new Dictionary<string, string?> { ["Email:Smtp:Host"] = host }, logger);

        // act
        await sut.SendAsync("bob@example.com", "Welcome", "<p>Hello</p>");

        // assert
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("bob@example.com", entry.Message);
        Assert.Contains("Welcome", entry.Message);
        Assert.Contains("<p>Hello</p>", entry.Message);
    }

    [Fact]
    public async Task SendAsync_WhenSmtpHostIsNotConfigured_DoesNotThrow()
    {
        // arrange
        var sut = CreateSut(new Dictionary<string, string?>(), new TestLogger());

        // act
        var exception = await Record.ExceptionAsync(() => sut.SendAsync("bob@example.com", "Subject", "Body"));

        // assert
        Assert.Null(exception);
    }

    private sealed record LogEntry(LogLevel Level, string Message);

    private sealed class TestLogger : ILogger<SmtpEmailSender>
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }
    }
}
