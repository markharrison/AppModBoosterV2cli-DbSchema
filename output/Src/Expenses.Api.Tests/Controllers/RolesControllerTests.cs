using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Expenses.Api.Tests.Helpers;
using NUnit.Framework;

namespace Expenses.Api.Tests.Controllers;

[TestFixture]
public class RolesControllerTests
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
    public async Task GetAll_ReturnsSeededRoles()
    {
        var response = await _client.GetAsync("/api/roles");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var roles = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(roles.GetArrayLength(), Is.GreaterThanOrEqualTo(2));
    }
}
