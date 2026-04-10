using System.Net;
using Expenses.Api.Tests.Helpers;
using NUnit.Framework;

namespace Expenses.Api.Tests.Controllers;

[TestFixture]
public class HealthControllerTests
{
    private TestWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Live_ReturnsOk()
    {
        var response = await _client.GetAsync("/live");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Ready_ReturnsOk()
    {
        var response = await _client.GetAsync("/ready");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
