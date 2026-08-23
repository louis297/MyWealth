using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MyWealth.Application.Common.Models;
using MyWealth.Application.IdentityAuth.Login;
using MyWealth.Application.TenantAdmins;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;

namespace MyWealth.Application.FunctionalTests.TenantAdmins;

public class TenantAdminHttpTests : TestBase
{
    private const string Password = "P@ssw0rd!";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Test]
    public async Task SystemAdmin_CanManageTenantAdmins()
    {
        using var client = await CreateSystemAdminClient();
        var tenantId = await CreateTenant(client, "Acme Wealth");
        var otherTenantId = await CreateTenant(client, "Beta Partners");

        var created = await CreateTenantAdmin(client, tenantId, "Alice Chen", "alice.chen@acme.com");
        created.Id.ShouldBeGreaterThan(0);

        var getResponse = await client.GetAsync($"/tenant-admins/{created.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var admin = await getResponse.Content.ReadFromJsonAsync<TenantAdminVm>(JsonOptions);
        admin.ShouldNotBeNull();
        admin.Id.ShouldBe(created.Id);
        admin.TenantId.ShouldBe(tenantId);
        admin.TenantName.ShouldBe("Acme Wealth");
        admin.Name.ShouldBe("Alice Chen");
        admin.Email.ShouldBe("alice.chen@acme.com");
        admin.IsEnabled.ShouldBeTrue();

        var other = await CreateTenantAdmin(client, tenantId, "Bob Jones", "bob.jones@acme.com");
        var otherTenantAdmin = await CreateTenantAdmin(
            client, otherTenantId, "Cara Lee", "cara.lee@beta.com");

        var list = await GetList(client);
        list.Items.ShouldContain(a => a.Id == created.Id && a.Name == "Alice Chen");
        list.Items.ShouldContain(a => a.Id == other.Id);
        list.Items.ShouldContain(a => a.Id == otherTenantAdmin.Id);

        var searchByName = await GetList(client, "?search=alice");
        searchByName.Items.ShouldContain(a => a.Id == created.Id);
        searchByName.Items.ShouldNotContain(a => a.Id == other.Id);

        var searchByEmail = await GetList(client, "?search=bob.jones");
        searchByEmail.Items.ShouldContain(a => a.Id == other.Id);
        searchByEmail.Items.ShouldNotContain(a => a.Id == created.Id);

        var searchById = await GetList(client, $"?search={created.Id}");
        searchById.Items.ShouldContain(a => a.Id == created.Id);

        var byTenant = await GetList(client, $"?tenantId={tenantId}");
        byTenant.Items.ShouldContain(a => a.Id == created.Id);
        byTenant.Items.ShouldContain(a => a.Id == other.Id);
        byTenant.Items.ShouldNotContain(a => a.Id == otherTenantAdmin.Id);

        var page = await GetList(client, "?page=1&pageSize=1");
        page.Items.Count.ShouldBe(1);
        page.PageNumber.ShouldBe(1);
        page.HasNextPage.ShouldBeTrue();
        page.TotalCount.ShouldBeGreaterThanOrEqualTo(3);

        var renameResponse = await client.PutAsJsonAsync(
            $"/tenant-admins/{created.Id}",
            new { id = created.Id, name = "Alice Chen Ltd" });
        renameResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var renamed = await (await client.GetAsync($"/tenant-admins/{created.Id}"))
            .Content.ReadFromJsonAsync<TenantAdminVm>(JsonOptions);
        renamed.ShouldNotBeNull();
        renamed.Name.ShouldBe("Alice Chen Ltd");

        var disableResponse = await client.DeleteAsync($"/tenant-admins/{created.Id}");
        disableResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var disabled = await (await client.GetAsync($"/tenant-admins/{created.Id}"))
            .Content.ReadFromJsonAsync<TenantAdminVm>(JsonOptions);
        disabled.ShouldNotBeNull();
        disabled.IsEnabled.ShouldBeFalse();

        var enabledOnly = await GetList(client, "?isEnabled=true");
        enabledOnly.Items.ShouldNotContain(a => a.Id == created.Id);

        var disabledOnly = await GetList(client, "?isEnabled=false");
        disabledOnly.Items.ShouldContain(a => a.Id == created.Id);

        var enableResponse = await client.PutAsJsonAsync(
            $"/tenant-admins/{created.Id}",
            new { id = created.Id, isEnabled = true });
        enableResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var enabled = await (await client.GetAsync($"/tenant-admins/{created.Id}"))
            .Content.ReadFromJsonAsync<TenantAdminVm>(JsonOptions);
        enabled.ShouldNotBeNull();
        enabled.IsEnabled.ShouldBeTrue();
    }

    [Test]
    public async Task CreatedTenantAdmin_CanLogIn_UntilDisabled()
    {
        using var client = await CreateSystemAdminClient();
        var tenantId = await CreateTenant(client, "Login Firm");
        await CreateTenantAdmin(client, tenantId, "Alice Chen", "alice.login@acme.com");

        var (loginStatus, login) = await PostLogin("alice.login@acme.com", Password);
        loginStatus.ShouldBe(HttpStatusCode.OK);
        login.ShouldNotBeNull();
        login.Role.ShouldBe(Roles.TenantAdmin);
        login.TenantId.ShouldBe(tenantId);
        login.DisplayName.ShouldBe("Alice Chen");

        var created = (await GetList(client, "?search=alice.login@acme.com")).Items.Single();

        (await client.DeleteAsync($"/tenant-admins/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (disabledStatus, _) = await PostLogin("alice.login@acme.com", Password);
        disabledStatus.ShouldBe(HttpStatusCode.Unauthorized);

        (await client.PutAsJsonAsync($"/tenant-admins/{created.Id}", new { id = created.Id, isEnabled = true }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (reenabledStatus, reenabled) = await PostLogin("alice.login@acme.com", Password);
        reenabledStatus.ShouldBe(HttpStatusCode.OK);
        reenabled.ShouldNotBeNull();
    }

    [Test]
    public async Task Create_DuplicateEmailDifferentCase_Returns400()
    {
        using var client = await CreateSystemAdminClient();
        var tenantId = await CreateTenant(client, "Acme Wealth");

        (await client.PostAsJsonAsync("/tenant-admins", new
        {
            tenantId,
            name = "Alice",
            email = "alice@acme.com",
            password = Password
        })).StatusCode.ShouldBe(HttpStatusCode.Created);

        var duplicate = await client.PostAsJsonAsync("/tenant-admins", new
        {
            tenantId,
            name = "Alice 2",
            email = "Alice@acme.com",
            password = Password
        });

        duplicate.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Create_UnknownTenant_Returns400()
    {
        using var client = await CreateSystemAdminClient();

        var response = await client.PostAsJsonAsync("/tenant-admins", new
        {
            tenantId = 999999,
            name = "Alice",
            email = "alice.unknown@acme.com",
            password = Password
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Create_DisabledTenant_Returns400()
    {
        using var client = await CreateSystemAdminClient();
        var tenantId = await CreateTenant(client, "Disabled Firm");

        (await client.PutAsJsonAsync($"/tenants/{tenantId}", new { id = tenantId, isEnabled = false }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var response = await client.PostAsJsonAsync("/tenant-admins", new
        {
            tenantId,
            name = "Alice",
            email = "alice.disabled@acme.com",
            password = Password
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Create_WeakPassword_Returns400()
    {
        using var client = await CreateSystemAdminClient();
        var tenantId = await CreateTenant(client, "Acme Wealth");

        var response = await client.PostAsJsonAsync("/tenant-admins", new
        {
            tenantId,
            name = "Alice",
            email = "alice.weak@acme.com",
            password = "password"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Create_MissingFields_Returns400()
    {
        using var client = await CreateSystemAdminClient();
        var tenantId = await CreateTenant(client, "Acme Wealth");

        (await client.PostAsJsonAsync("/tenant-admins", new
        {
            email = "alice@acme.com",
            password = Password
        })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/tenant-admins", new
        {
            tenantId,
            password = Password
        })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/tenant-admins", new
        {
            tenantId,
            name = "Alice",
            password = Password
        })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/tenant-admins", new
        {
            tenantId,
            name = "Alice",
            email = "alice.missing@acme.com"
        })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Disable_LastTenantAdmin_Succeeds()
    {
        using var client = await CreateSystemAdminClient();
        var tenantId = await CreateTenant(client, "Solo Firm");
        var created = await CreateTenantAdmin(client, tenantId, "Alice", "alice.last@acme.com");

        (await client.DeleteAsync($"/tenant-admins/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var disabled = await (await client.GetAsync($"/tenant-admins/{created.Id}"))
            .Content.ReadFromJsonAsync<TenantAdminVm>(JsonOptions);
        disabled.ShouldNotBeNull();
        disabled.IsEnabled.ShouldBeFalse();
    }

    [Test]
    public async Task Anonymous_Returns401()
    {
        using var client = TestApp.CreateClient();

        (await client.GetAsync("/tenant-admins")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/tenant-admins/1")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/tenant-admins", new
        {
            tenantId = 1,
            name = "Alice",
            email = "alice@acme.com",
            password = Password
        })).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PutAsJsonAsync("/tenant-admins/1", new { id = 1, name = "Alice" }))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.DeleteAsync("/tenant-admins/1")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
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
    public async Task GetUpdateDelete_UnknownId_Returns404()
    {
        using var client = await CreateSystemAdminClient();

        (await client.GetAsync("/tenant-admins/999999")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PutAsJsonAsync("/tenant-admins/999999", new { id = 999999, name = "Missing" }))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.DeleteAsync("/tenant-admins/999999")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Get_AdviserDomainUser_Returns404()
    {
        using var client = await CreateSystemAdminClient();
        var tenantId = await CreateTenant(client, "Acme Wealth");
        var adviser = User.CreateAdviser(tenantId, "Jane Smith", "jane.smith@acme.com");
        await TestApp.AddAsync(adviser);

        adviser.Id.ShouldBeGreaterThan(0);
        (await client.GetAsync($"/tenant-admins/{adviser.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Update_EmptyBody_Returns400()
    {
        using var client = await CreateSystemAdminClient();
        var tenantId = await CreateTenant(client, "Acme Wealth");
        var created = await CreateTenantAdmin(client, tenantId, "Alice", "alice.empty@acme.com");

        var response = await client.PutAsJsonAsync($"/tenant-admins/{created.Id}", new { id = created.Id });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Update_RouteIdMismatch_Returns400()
    {
        using var client = await CreateSystemAdminClient();
        var tenantId = await CreateTenant(client, "Acme Wealth");
        var created = await CreateTenantAdmin(client, tenantId, "Alice", "alice.mismatch@acme.com");

        var response = await client.PutAsJsonAsync(
            $"/tenant-admins/{created.Id}",
            new { id = created.Id + 1, name = "Other" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static async Task AssertForbidden(UserRole role)
    {
        using var client = await CreateRoleClient(role, tenantId: 1);

        await ShouldBeStatus(client, HttpMethod.Get, "/tenant-admins", HttpStatusCode.Forbidden);
        await ShouldBeStatus(client, HttpMethod.Get, "/tenant-admins/1", HttpStatusCode.Forbidden);
        (await client.PostAsJsonAsync("/tenant-admins", new
        {
            tenantId = 1,
            name = "Alice",
            email = "alice@acme.com",
            password = Password
        })).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await client.PutAsJsonAsync("/tenant-admins/1", new { id = 1, name = "Alice" }))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await client.DeleteAsync("/tenant-admins/1")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task<HttpClient> CreateSystemAdminClient()
        => await CreateRoleClient(UserRole.SystemAdmin);

    private static async Task<int> CreateTenant(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/tenants", new { name });
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"POST /tenants expected 201 but was {(int)response.StatusCode}: {body}");
        }

        var created = await response.Content.ReadFromJsonAsync<CreatedIdVm>(JsonOptions);
        created.ShouldNotBeNull();
        return created.Id;
    }

    private static async Task<CreatedIdVm> CreateTenantAdmin(
        HttpClient client,
        int tenantId,
        string name,
        string email)
    {
        var response = await client.PostAsJsonAsync("/tenant-admins", new
        {
            tenantId,
            name,
            email,
            password = Password
        });
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"POST /tenant-admins expected 201 but was {(int)response.StatusCode}: {body}");
        }

        var created = await response.Content.ReadFromJsonAsync<CreatedIdVm>(JsonOptions);
        created.ShouldNotBeNull();
        return created;
    }

    private static async Task<PaginatedTenantAdmins> GetList(HttpClient client, string query = "")
    {
        var response = await client.GetAsync($"/tenant-admins{query}");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"GET /tenant-admins{query} expected 200 but was {(int)response.StatusCode}: {body}");
        }

        var list = await response.Content.ReadFromJsonAsync<PaginatedTenantAdmins>(JsonOptions);
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

    private sealed class PaginatedTenantAdmins
    {
        public List<TenantAdminVm> Items { get; set; } = [];

        public int PageNumber { get; set; }

        public int TotalPages { get; set; }

        public int TotalCount { get; set; }

        public bool HasPreviousPage { get; set; }

        public bool HasNextPage { get; set; }
    }
}
