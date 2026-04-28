namespace NICE.Platform.Collaboration.Tests.Integration;

using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

[TestFixture]
[Category("Integration")]
public class CollaborationApiIntegrationTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task HealthEndpoint_Should_Return_200()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    // TODO: add integration tests per controller endpoint
}
