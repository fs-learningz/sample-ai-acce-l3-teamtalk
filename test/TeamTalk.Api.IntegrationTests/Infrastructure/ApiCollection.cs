namespace TeamTalk.Api.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiWebApplicationFactory>
{
    public const string Name = "Api";
}
