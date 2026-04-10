using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Expenses.Api.Tests.Helpers;
using NUnit.Framework;

namespace Expenses.Api.Tests.Controllers;

[TestFixture]
public class UsersControllerTests
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
    public async Task GetAll_ReturnsSeededUsers()
    {
        var response = await _client.GetAsync("/api/users");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var users = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(users.GetArrayLength(), Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public async Task GetById_ReturnsUserWithRoleAndManager()
    {
        var response = await _client.GetAsync("/api/users/1");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var user = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(user.GetProperty("userName").GetString(), Is.EqualTo("Alice Example"));
        Assert.That(user.GetProperty("role").GetProperty("roleName").GetString(), Is.Not.Null.And.Not.Empty);
        Assert.That(user.TryGetProperty("manager", out _), Is.True);
    }

    [Test]
    public async Task GetById_NonExistent_Returns404()
    {
        var response = await _client.GetAsync("/api/users/999");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
