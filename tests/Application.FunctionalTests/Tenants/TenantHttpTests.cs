using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MyWealth.Application.Common.Models;
using MyWealth.Application.IdentityAuth.Login;
using MyWealth.Application.Tenants;
using MyWealth.Domain.Enums;

namespace MyWealth.Application.FunctionalTests.Tenants;

public class TenantHttpTests : TestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Test]
    public async Task SystemAdmin_CanManageTenants()
    {
        using var client = await CreateAuthenticatedClient(UserRole.SystemAdmin);

        var createResponse = await client.PostAsJsonAsync("/tenants", new { name = "Acme Wealth" });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedIdVm>(JsonOptions);
        created.ShouldNotBeNull();
        created.Id.ShouldBeGreaterThan(0);
        createResponse.Headers.Location.ShouldNotBeNull();

        var getResponse = await client.GetAsync($"/tenants/{created.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tenant = await getResponse.Content.ReadFromJsonAsync<TenantVm>(JsonOptions);
        tenant.ShouldNotBeNull();
        tenant.Id.ShouldBe(created.Id);
        tenant.Name.ShouldBe("Acme Wealth");
        tenant.IsEnabled.ShouldBeTrue();

        var otherCreate = await client.PostAsJsonAsync("/tenants", new { name = "Beta Partners" });
        otherCreate.StatusCode.ShouldBe(HttpStatusCode.Created);
        var other = await otherCreate.Content.ReadFromJsonAsync<CreatedIdVm>(JsonOptions);
        other.ShouldNotBeNull();

        var list = await GetList(client);
        list.Items.Count.ShouldBeGreaterThanOrEqualTo(2);
        list.Items.ShouldContain(t => t.Id == created.Id && t.Name == "Acme Wealth");

        var searchByName = await GetList(client, "?search=acme");
        searchByName.Items.ShouldContain(t => t.Id == created.Id);
        searchByName.Items.ShouldNotContain(t => t.Id == other.Id);

        var searchById = await GetList(client, $"?search={created.Id}");
        searchById.Items.ShouldContain(t => t.Id == created.Id);

        var page = await GetList(client, "?page=1&pageSize=1");
        page.Items.Count.ShouldBe(1);
        page.PageNumber.ShouldBe(1);
        page.HasNextPage.ShouldBeTrue();
        page.TotalCount.ShouldBeGreaterThanOrEqualTo(2);

        var renameResponse = await client.PutAsJsonAsync($"/tenants/{created.Id}", new { id = created.Id, name = "Acme Wealth Ltd" });
        renameResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var renamed = await (await client.GetAsync($"/tenants/{created.Id}")).Content.ReadFromJsonAsync<TenantVm>(JsonOptions);
        renamed.ShouldNotBeNull();
        renamed.Name.ShouldBe("Acme Wealth Ltd");

        var disableResponse = await client.PutAsJsonAsync($"/tenants/{created.Id}", new { id = created.Id, isEnabled = false });
        disableResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var disabled = await (await client.GetAsync($"/tenants/{created.Id}")).Content.ReadFromJsonAsync<TenantVm>(JsonOptions);
        disabled.ShouldNotBeNull();
        disabled.IsEnabled.ShouldBeFalse();

        var disabledList = await GetList(client);
        disabledList.Items.ShouldContain(t => t.Id == created.Id && t.IsEnabled == false);

        var enabledOnly = await GetList(client, "?isEnabled=true");
        enabledOnly.Items.ShouldNotContain(t => t.Id == created.Id);

        var disabledOnly = await GetList(client, "?isEnabled=false");
        disabledOnly.Items.ShouldContain(t => t.Id == created.Id);
    }

    [Test]
    public async Task Create_DuplicateNameDifferentCase_Returns400()
    {
        using var client = await CreateAuthenticatedClient(UserRole.SystemAdmin);

        (await client.PostAsJsonAsync("/tenants", new { name = "Acme Wealth" })).StatusCode.ShouldBe(HttpStatusCode.Created);

        var duplicate = await client.PostAsJsonAsync("/tenants", new { name = "acme wealth" });
        duplicate.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Anonymous_Returns401()
    {
        using var client = TestApp.CreateClient();

        (await client.GetAsync("/tenants")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/tenants/1")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/tenants", new { name = "Acme" })).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PutAsJsonAsync("/tenants/1", new { id = 1, name = "Acme" })).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task TenantAdmin_Returns403()
    {
        await AssertForbidden(UserRole.TenantAdmin);
    }

    [Test]
    public async Task Adviser_Returns403()
    {
        await AssertForbidden(UserRole.Adviser);
    }

    [Test]
    public async Task GetAndUpdate_UnknownId_Returns404()
    {
        using var client = await CreateAuthenticatedClient(UserRole.SystemAdmin);

        (await client.GetAsync("/tenants/999999")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PutAsJsonAsync("/tenants/999999", new { id = 999999, name = "Missing" })).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Update_EmptyBody_Returns400()
    {
        using var client = await CreateAuthenticatedClient(UserRole.SystemAdmin);
        var created = await CreateTenant(client, "Acme Wealth");

        var response = await client.PutAsJsonAsync($"/tenants/{created.Id}", new { id = created.Id });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Update_RouteIdMismatch_Returns400()
    {
        using var client = await CreateAuthenticatedClient(UserRole.SystemAdmin);
        var created = await CreateTenant(client, "Acme Wealth");

        var response = await client.PutAsJsonAsync($"/tenants/{created.Id}", new { id = created.Id + 1, name = "Other" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static async Task AssertForbidden(UserRole role)
    {
        using var client = await CreateAuthenticatedClient(role, tenantId: 1);

        await ShouldBeStatus(client, HttpMethod.Get, "/tenants", HttpStatusCode.Forbidden);
        await ShouldBeStatus(client, HttpMethod.Get, "/tenants/1", HttpStatusCode.Forbidden);
        (await client.PostAsJsonAsync("/tenants", new { name = "Acme" })).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await client.PutAsJsonAsync("/tenants/1", new { id = 1, name = "Acme" })).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task<HttpClient> CreateAuthenticatedClient(UserRole role, int? tenantId = null)
    {
        var email = $"{role.ToString().ToLowerInvariant()}@local";
        var password = "Password1!";
        await TestApp.CreateUserAsync(email, password, role, tenantId);

        var (_, login) = await PostLogin(email, password);
        login.ShouldNotBeNull();

        var client = TestApp.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    private static async Task<CreatedIdVm> CreateTenant(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/tenants", new { name });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreatedIdVm>(JsonOptions);
        created.ShouldNotBeNull();
        return created;
    }

    private static async Task<PaginatedTenants> GetList(HttpClient client, string query = "")
    {
        var response = await client.GetAsync($"/tenants{query}");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"GET /tenants{query} expected 200 but was {(int)response.StatusCode}: {body}");
        }
        var list = await response.Content.ReadFromJsonAsync<PaginatedTenants>(JsonOptions);
        list.ShouldNotBeNull();
        return list;
    }

    private static async Task ShouldBeStatus(HttpClient client, HttpMethod method, string url, HttpStatusCode expected)
    {
        using var request = new HttpRequestMessage(method, url);
        var response = await client.SendAsync(request);
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"{method} {url} expected {expected} but was {(int)response.StatusCode}: {body}");
        }
    }

    private static async Task<(HttpStatusCode Status, LoginResultVm? Body)> PostLogin(string email, string password)
    {
        using var client = TestApp.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new { email, password });
        var body = response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<LoginResultVm>(JsonOptions)
            : null;

        return (response.StatusCode, body);
    }

    private sealed class PaginatedTenants
    {
        public List<TenantVm> Items { get; set; } = [];

        public int PageNumber { get; set; }

        public int TotalPages { get; set; }

        public int TotalCount { get; set; }

        public bool HasPreviousPage { get; set; }

        public bool HasNextPage { get; set; }
    }
}
