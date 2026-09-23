namespace TeamTalk.Infrastructure.Tests;

[CollectionDefinition(Name)]
public class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "Postgres";
}
