using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Expenses.Api.Tests.Helpers;
using NUnit.Framework;

namespace Expenses.Api.Tests.Controllers;

[TestFixture]
public class CategoriesControllerTests
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
    public async Task GetAll_ReturnsSeededCategories()
    {
        var response = await _client.GetAsync("/api/categories");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var categories = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(categories.GetArrayLength(), Is.GreaterThanOrEqualTo(5));
    }
}
