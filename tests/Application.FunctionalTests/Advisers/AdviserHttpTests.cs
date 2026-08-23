using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MyWealth.Application.Advisers;
using MyWealth.Application.Common.Models;
using MyWealth.Application.IdentityAuth.Login;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;

namespace MyWealth.Application.FunctionalTests.Advisers;

public class AdviserHttpTests : TestBase
{
    private const string Password = "P@ssw0rd!";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Test]
    public async Task TenantAdmin_CanManageAdvisers()
    {
        var (client, _) = await CreateTenantAdminClient();

        var created = await CreateAdviser(client, "Jane Smith", "jane.smith@acme.com");
        created.Id.ShouldBeGreaterThan(0);

        var getResponse = await client.GetAsync($"/advisers/{created.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var adviser = await getResponse.Content.ReadFromJsonAsync<AdviserVm>(JsonOptions);
        adviser.ShouldNotBeNull();
        adviser.Id.ShouldBe(created.Id);
        adviser.Name.ShouldBe("Jane Smith");
        adviser.Email.ShouldBe("jane.smith@acme.com");
        adviser.IsEnabled.ShouldBeTrue();

        var other = await CreateAdviser(client, "Bob Jones", "bob.jones@acme.com");

        var list = await GetList(client);
        list.Items.ShouldContain(a => a.Id == created.Id && a.Name == "Jane Smith");
        list.Items.ShouldContain(a => a.Id == other.Id);

        var searchByName = await GetList(client, "?search=jane");
        searchByName.Items.ShouldContain(a => a.Id == created.Id);
        searchByName.Items.ShouldNotContain(a => a.Id == other.Id);

        var searchByEmail = await GetList(client, "?search=bob.jones");
        searchByEmail.Items.ShouldContain(a => a.Id == other.Id);
        searchByEmail.Items.ShouldNotContain(a => a.Id == created.Id);

        var searchById = await GetList(client, $"?search={created.Id}");
        searchById.Items.ShouldContain(a => a.Id == created.Id);

        var page = await GetList(client, "?page=1&pageSize=1");
        page.Items.Count.ShouldBe(1);
        page.PageNumber.ShouldBe(1);
        page.HasNextPage.ShouldBeTrue();
        page.TotalCount.ShouldBeGreaterThanOrEqualTo(2);

        var renameResponse = await client.PutAsJsonAsync(
            $"/advisers/{created.Id}",
            new { id = created.Id, name = "Jane Smith Ltd" });
        renameResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var renamed = await (await client.GetAsync($"/advisers/{created.Id}")).Content.ReadFromJsonAsync<AdviserVm>(JsonOptions);
        renamed.ShouldNotBeNull();
        renamed.Name.ShouldBe("Jane Smith Ltd");

        var disableResponse = await client.DeleteAsync($"/advisers/{created.Id}");
        disableResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var disabled = await (await client.GetAsync($"/advisers/{created.Id}")).Content.ReadFromJsonAsync<AdviserVm>(JsonOptions);
        disabled.ShouldNotBeNull();
        disabled.IsEnabled.ShouldBeFalse();

        var enabledOnly = await GetList(client, "?isEnabled=true");
        enabledOnly.Items.ShouldNotContain(a => a.Id == created.Id);

        var disabledOnly = await GetList(client, "?isEnabled=false");
        disabledOnly.Items.ShouldContain(a => a.Id == created.Id);

        var enableResponse = await client.PutAsJsonAsync(
            $"/advisers/{created.Id}",
            new { id = created.Id, isEnabled = true });
        enableResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var enabled = await (await client.GetAsync($"/advisers/{created.Id}")).Content.ReadFromJsonAsync<AdviserVm>(JsonOptions);
        enabled.ShouldNotBeNull();
        enabled.IsEnabled.ShouldBeTrue();
    }

    [Test]
    public async Task CreatedAdviser_CanLogIn_UntilDisabled()
    {
        var (client, tenantId) = await CreateTenantAdminClient();
        await CreateAdviser(client, "Jane Smith", "jane.login@acme.com");

        var (loginStatus, login) = await PostLogin("jane.login@acme.com", Password);
        loginStatus.ShouldBe(HttpStatusCode.OK);
        login.ShouldNotBeNull();
        login.Role.ShouldBe(Roles.Adviser);
        login.TenantId.ShouldBe(tenantId);
        login.DisplayName.ShouldBe("Jane Smith");

        var created = (await GetList(client, "?search=jane.login@acme.com")).Items.Single();

        (await client.DeleteAsync($"/advisers/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (disabledStatus, _) = await PostLogin("jane.login@acme.com", Password);
        disabledStatus.ShouldBe(HttpStatusCode.Unauthorized);

        (await client.PutAsJsonAsync($"/advisers/{created.Id}", new { id = created.Id, isEnabled = true }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (reenabledStatus, reenabled) = await PostLogin("jane.login@acme.com", Password);
        reenabledStatus.ShouldBe(HttpStatusCode.OK);
        reenabled.ShouldNotBeNull();
    }

    [Test]
    public async Task Create_DuplicateEmailDifferentCase_Returns400()
    {
        var (client, _) = await CreateTenantAdminClient();

        (await client.PostAsJsonAsync("/advisers", new
        {
            name = "Jane",
            email = "jane@acme.com",
            password = Password
        })).StatusCode.ShouldBe(HttpStatusCode.Created);

        var duplicate = await client.PostAsJsonAsync("/advisers", new
        {
            name = "Jane 2",
            email = "Jane@acme.com",
            password = Password
        });

        duplicate.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Create_WeakPassword_Returns400()
    {
        var (client, _) = await CreateTenantAdminClient();

        var response = await client.PostAsJsonAsync("/advisers", new
        {
            name = "Jane",
            email = "jane.weak@acme.com",
            password = "password"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Create_MissingFields_Returns400()
    {
        var (client, _) = await CreateTenantAdminClient();

        (await client.PostAsJsonAsync("/advisers", new { email = "jane@acme.com", password = Password }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/advisers", new { name = "Jane", password = Password }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/advisers", new { name = "Jane", email = "jane@acme.com" }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Disable_WhenCustomersAssigned_Returns400()
    {
        var (client, tenantId) = await CreateTenantAdminClient();
        var created = await CreateAdviser(client, "Jane", "jane.guard@acme.com");

        await TestApp.AddAsync(User.CreateCustomer(tenantId, created.Id, "Zhang San", "zhangsan@example.com"));

        (await client.DeleteAsync($"/advisers/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PutAsJsonAsync($"/advisers/{created.Id}", new { id = created.Id, isEnabled = false }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var stillEnabled = await (await client.GetAsync($"/advisers/{created.Id}")).Content.ReadFromJsonAsync<AdviserVm>(JsonOptions);
        stillEnabled.ShouldNotBeNull();
        stillEnabled.IsEnabled.ShouldBeTrue();
    }

    [Test]
    public async Task Anonymous_Returns401()
    {
        using var client = TestApp.CreateClient();

        (await client.GetAsync("/advisers")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/advisers/1")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/advisers", new { name = "Jane", email = "jane@acme.com", password = Password }))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PutAsJsonAsync("/advisers/1", new { id = 1, name = "Jane" }))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.DeleteAsync("/advisers/1")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Adviser_Returns403()
    {
        await AssertForbidden(UserRole.Adviser);
    }

    [Test]
    public async Task SystemAdmin_Returns403()
    {
        await AssertForbidden(UserRole.SystemAdmin);
    }

    [Test]
    public async Task GetUpdateDelete_UnknownId_Returns404()
    {
        var (client, _) = await CreateTenantAdminClient();

        (await client.GetAsync("/advisers/999999")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PutAsJsonAsync("/advisers/999999", new { id = 999999, name = "Missing" }))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.DeleteAsync("/advisers/999999")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CrossTenantId_Returns404()
    {
        var (clientA, _) = await CreateTenantAdminClient();
        var (clientB, _) = await CreateTenantAdminClient();
        var created = await CreateAdviser(clientB, "Jane", "jane.other@acme.com");

        (await clientA.GetAsync($"/advisers/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await clientA.PutAsJsonAsync($"/advisers/{created.Id}", new { id = created.Id, name = "Hijack" }))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await clientA.DeleteAsync($"/advisers/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Get_TenantAdminDomainUser_Returns404()
    {
        var (client, tenantId) = await CreateTenantAdminClient();
        var tenantAdmin = User.CreateTenantAdmin(tenantId, "Other Admin", "other.ta@acme.com");
        await TestApp.AddAsync(tenantAdmin);

        tenantAdmin.Id.ShouldBeGreaterThan(0);
        (await client.GetAsync($"/advisers/{tenantAdmin.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Update_EmptyBody_Returns400()
    {
        var (client, _) = await CreateTenantAdminClient();
        var created = await CreateAdviser(client, "Jane", "jane.empty@acme.com");

        var response = await client.PutAsJsonAsync($"/advisers/{created.Id}", new { id = created.Id });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Update_RouteIdMismatch_Returns400()
    {
        var (client, _) = await CreateTenantAdminClient();
        var created = await CreateAdviser(client, "Jane", "jane.mismatch@acme.com");

        var response = await client.PutAsJsonAsync(
            $"/advisers/{created.Id}",
            new { id = created.Id + 1, name = "Other" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static async Task AssertForbidden(UserRole role)
    {
        using var client = await CreateRoleClient(role, tenantId: role == UserRole.SystemAdmin ? null : 1);

        await ShouldBeStatus(client, HttpMethod.Get, "/advisers", HttpStatusCode.Forbidden);
        await ShouldBeStatus(client, HttpMethod.Get, "/advisers/1", HttpStatusCode.Forbidden);
        (await client.PostAsJsonAsync("/advisers", new { name = "Jane", email = "jane@acme.com", password = Password }))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await client.PutAsJsonAsync("/advisers/1", new { id = 1, name = "Jane" }))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await client.DeleteAsync("/advisers/1")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task<(HttpClient Client, int TenantId)> CreateTenantAdminClient()
    {
        var tenantId = await CreateTenant($"Firm {Guid.NewGuid():N}"[..20]);
        var client = await CreateRoleClient(UserRole.TenantAdmin, tenantId);
        return (client, tenantId);
    }

    private static async Task<int> CreateTenant(string name)
    {
        using var client = await CreateRoleClient(UserRole.SystemAdmin);
        var response = await client.PostAsJsonAsync("/tenants", new { name });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreatedIdVm>(JsonOptions);
        created.ShouldNotBeNull();
        return created.Id;
    }

    private static async Task<CreatedIdVm> CreateAdviser(HttpClient client, string name, string email)
    {
        var response = await client.PostAsJsonAsync("/advisers", new { name, email, password = Password });
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"POST /advisers expected 201 but was {(int)response.StatusCode}: {body}");
        }

        var created = await response.Content.ReadFromJsonAsync<CreatedIdVm>(JsonOptions);
        created.ShouldNotBeNull();
        return created;
    }

    private static async Task<PaginatedAdvisers> GetList(HttpClient client, string query = "")
    {
        var response = await client.GetAsync($"/advisers{query}");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"GET /advisers{query} expected 200 but was {(int)response.StatusCode}: {body}");
        }

        var list = await response.Content.ReadFromJsonAsync<PaginatedAdvisers>(JsonOptions);
        list.ShouldNotBeNull();
        return list;
    }

    private static async Task<HttpClient> CreateRoleClient(UserRole role, int? tenantId = null)
    {
        var email = $"{role.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}@local";
        await TestApp.CreateUserAsync(email, "Password1!", role, tenantId);

        var (_, login) = await PostLogin(email, "Password1!");
        login.ShouldNotBeNull();

        var client = TestApp.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
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

    private sealed class PaginatedAdvisers
    {
        public List<AdviserVm> Items { get; set; } = [];

        public int PageNumber { get; set; }

        public int TotalPages { get; set; }

        public int TotalCount { get; set; }

        public bool HasPreviousPage { get; set; }

        public bool HasNextPage { get; set; }
    }
}
