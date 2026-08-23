using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MyWealth.Application.Common.Models;
using MyWealth.Application.Customers;
using MyWealth.Application.IdentityAuth.Login;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;

namespace MyWealth.Application.FunctionalTests.Customers;

public class CustomerHttpTests : TestBase
{
    private const string Password = "P@ssw0rd!";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Test]
    public async Task TenantAdmin_CanManageCustomers()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane Smith", "jane.smith@acme.com");
        var otherAdviser = await CreateAdviser(client, "Bob Jones", "bob.jones@acme.com");

        var created = await CreateCustomer(client, "Zhang San", "zhangsan@example.com", adviser.Id);
        created.Id.ShouldBeGreaterThan(0);

        var getResponse = await client.GetAsync($"/customers/{created.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var customer = await getResponse.Content.ReadFromJsonAsync<CustomerVm>(JsonOptions);
        customer.ShouldNotBeNull();
        customer.Id.ShouldBe(created.Id);
        customer.Name.ShouldBe("Zhang San");
        customer.Email.ShouldBe("zhangsan@example.com");
        customer.IsEnabled.ShouldBeTrue();
        customer.AdviserId.ShouldBe(adviser.Id);
        customer.AdviserName.ShouldBe("Jane Smith");

        var other = await CreateCustomer(client, "Li Si", "lisi@example.com", adviser.Id);

        var list = await GetList(client);
        list.Items.ShouldContain(c => c.Id == created.Id && c.Name == "Zhang San");
        list.Items.ShouldContain(c => c.Id == other.Id);

        var searchByName = await GetList(client, "?search=zhang");
        searchByName.Items.ShouldContain(c => c.Id == created.Id);
        searchByName.Items.ShouldNotContain(c => c.Id == other.Id);

        var searchByEmail = await GetList(client, "?search=lisi@");
        searchByEmail.Items.ShouldContain(c => c.Id == other.Id);
        searchByEmail.Items.ShouldNotContain(c => c.Id == created.Id);

        var searchById = await GetList(client, $"?search={created.Id}");
        searchById.Items.ShouldContain(c => c.Id == created.Id);

        var page = await GetList(client, "?page=1&pageSize=1");
        page.Items.Count.ShouldBe(1);
        page.PageNumber.ShouldBe(1);
        page.HasNextPage.ShouldBeTrue();
        page.TotalCount.ShouldBeGreaterThanOrEqualTo(2);

        var renameResponse = await client.PutAsJsonAsync(
            $"/customers/{created.Id}",
            new { id = created.Id, name = "Zhang San Ltd" });
        renameResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var renamed = await (await client.GetAsync($"/customers/{created.Id}")).Content.ReadFromJsonAsync<CustomerVm>(JsonOptions);
        renamed.ShouldNotBeNull();
        renamed.Name.ShouldBe("Zhang San Ltd");

        var reassignResponse = await client.PutAsJsonAsync(
            $"/customers/{created.Id}",
            new { id = created.Id, adviserId = otherAdviser.Id });
        reassignResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var reassigned = await (await client.GetAsync($"/customers/{created.Id}")).Content.ReadFromJsonAsync<CustomerVm>(JsonOptions);
        reassigned.ShouldNotBeNull();
        reassigned.AdviserId.ShouldBe(otherAdviser.Id);
        reassigned.AdviserName.ShouldBe("Bob Jones");

        var disableResponse = await client.DeleteAsync($"/customers/{created.Id}");
        disableResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var disabled = await (await client.GetAsync($"/customers/{created.Id}")).Content.ReadFromJsonAsync<CustomerVm>(JsonOptions);
        disabled.ShouldNotBeNull();
        disabled.IsEnabled.ShouldBeFalse();

        var enabledOnly = await GetList(client, "?isEnabled=true");
        enabledOnly.Items.ShouldNotContain(c => c.Id == created.Id);

        var disabledOnly = await GetList(client, "?isEnabled=false");
        disabledOnly.Items.ShouldContain(c => c.Id == created.Id);

        var enableResponse = await client.PutAsJsonAsync(
            $"/customers/{created.Id}",
            new { id = created.Id, isEnabled = true });
        enableResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var enabled = await (await client.GetAsync($"/customers/{created.Id}")).Content.ReadFromJsonAsync<CustomerVm>(JsonOptions);
        enabled.ShouldNotBeNull();
        enabled.IsEnabled.ShouldBeTrue();
    }

    [Test]
    public async Task Adviser_CanOnlyManageOwnCustomers()
    {
        var (admin, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(admin, "Jane Smith", "jane.own@acme.com");
        var otherAdviser = await CreateAdviser(admin, "Bob Jones", "bob.own@acme.com");
        var otherCustomer = await CreateCustomer(admin, "Other Client", "other.client@example.com", otherAdviser.Id);

        using var adviserClient = await LoginClient("jane.own@acme.com", Password);

        var created = await CreateCustomer(adviserClient, "Zhang San", "zhang.own@example.com", adviser.Id);

        var list = await GetList(adviserClient);
        list.Items.ShouldContain(c => c.Id == created.Id);
        list.Items.ShouldNotContain(c => c.Id == otherCustomer.Id);

        var own = await (await adviserClient.GetAsync($"/customers/{created.Id}")).Content.ReadFromJsonAsync<CustomerVm>(JsonOptions);
        own.ShouldNotBeNull();
        own.AdviserId.ShouldBe(adviser.Id);

        (await adviserClient.GetAsync($"/customers/{otherCustomer.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await adviserClient.PutAsJsonAsync($"/customers/{otherCustomer.Id}", new { id = otherCustomer.Id, name = "Hijack" }))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await adviserClient.DeleteAsync($"/customers/{otherCustomer.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await adviserClient.PutAsJsonAsync($"/customers/{created.Id}", new { id = created.Id, name = "Zhang San Ltd" }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await adviserClient.DeleteAsync($"/customers/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Adviser_AssigningToAnotherAdviser_Returns400()
    {
        var (admin, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(admin, "Jane", "jane.assign@acme.com");
        var otherAdviser = await CreateAdviser(admin, "Bob", "bob.assign@acme.com");

        using var adviserClient = await LoginClient("jane.assign@acme.com", Password);

        var create = await adviserClient.PostAsJsonAsync("/customers", new
        {
            name = "Zhang San",
            email = "zhang.assign@example.com",
            adviserId = otherAdviser.Id
        });
        create.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var created = await CreateCustomer(adviserClient, "Zhang San", "zhang.assign@example.com", adviser.Id);

        var reassign = await adviserClient.PutAsJsonAsync(
            $"/customers/{created.Id}",
            new { id = created.Id, adviserId = otherAdviser.Id });
        reassign.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreatedCustomer_CannotLogIn()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.nologin@acme.com");
        await CreateCustomer(client, "Zhang San", "zhang.nologin@example.com", adviser.Id);

        var (status, _) = await PostLogin("zhang.nologin@example.com", Password);
        status.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Create_DuplicateEmailDifferentCase_Returns400()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.dup@acme.com");

        (await client.PostAsJsonAsync("/customers", new
        {
            name = "Zhang",
            email = "zhang@example.com",
            adviserId = adviser.Id
        })).StatusCode.ShouldBe(HttpStatusCode.Created);

        var duplicate = await client.PostAsJsonAsync("/customers", new
        {
            name = "Zhang 2",
            email = "Zhang@example.com",
            adviserId = adviser.Id
        });

        duplicate.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Create_MissingFields_Returns400()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.missing@acme.com");

        (await client.PostAsJsonAsync("/customers", new { email = "zhang@example.com", adviserId = adviser.Id }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/customers", new { name = "Zhang", adviserId = adviser.Id }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/customers", new { name = "Zhang", email = "zhang@example.com" }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Create_InvalidAdviser_Returns400()
    {
        var (client, tenantId) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.invalid@acme.com");

        (await client.PostAsJsonAsync("/customers", new
        {
            name = "Zhang",
            email = "zhang.missing-adviser@example.com",
            adviserId = 999999
        })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        (await client.DeleteAsync($"/advisers/{adviser.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await client.PostAsJsonAsync("/customers", new
        {
            name = "Zhang",
            email = "zhang.disabled-adviser@example.com",
            adviserId = adviser.Id
        })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var tenantAdmin = User.CreateTenantAdmin(tenantId, "Other Admin", "other.ta.invalid@acme.com");
        await TestApp.AddAsync(tenantAdmin);

        (await client.PostAsJsonAsync("/customers", new
        {
            name = "Zhang",
            email = "zhang.ta-as-adviser@example.com",
            adviserId = tenantAdmin.Id
        })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Anonymous_Returns401()
    {
        using var client = TestApp.CreateClient();

        (await client.GetAsync("/customers")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/customers/1")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/customers", new { name = "Zhang", email = "zhang@example.com", adviserId = 1 }))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PutAsJsonAsync("/customers/1", new { id = 1, name = "Zhang" }))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.DeleteAsync("/customers/1")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SystemAdmin_Returns403()
    {
        using var client = await CreateRoleClient(UserRole.SystemAdmin);

        await ShouldBeStatus(client, HttpMethod.Get, "/customers", HttpStatusCode.Forbidden);
        await ShouldBeStatus(client, HttpMethod.Get, "/customers/1", HttpStatusCode.Forbidden);
        (await client.PostAsJsonAsync("/customers", new { name = "Zhang", email = "zhang@example.com", adviserId = 1 }))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await client.PutAsJsonAsync("/customers/1", new { id = 1, name = "Zhang" }))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await client.DeleteAsync("/customers/1")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetUpdateDelete_UnknownId_Returns404()
    {
        var (client, _) = await CreateTenantAdminClient();

        (await client.GetAsync("/customers/999999")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PutAsJsonAsync("/customers/999999", new { id = 999999, name = "Missing" }))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.DeleteAsync("/customers/999999")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CrossTenantId_Returns404()
    {
        var (clientA, _) = await CreateTenantAdminClient();
        var (clientB, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(clientB, "Jane", "jane.other@acme.com");
        var created = await CreateCustomer(clientB, "Zhang", "zhang.other@example.com", adviser.Id);

        (await clientA.GetAsync($"/customers/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await clientA.PutAsJsonAsync($"/customers/{created.Id}", new { id = created.Id, name = "Hijack" }))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await clientA.DeleteAsync($"/customers/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Get_NonCustomerDomainUser_Returns404()
    {
        var (client, tenantId) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.notcustomer@acme.com");
        var tenantAdmin = User.CreateTenantAdmin(tenantId, "Other Admin", "other.ta.notcustomer@acme.com");
        await TestApp.AddAsync(tenantAdmin);

        (await client.GetAsync($"/customers/{adviser.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.GetAsync($"/customers/{tenantAdmin.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Update_EmptyBody_Returns400()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.empty@acme.com");
        var created = await CreateCustomer(client, "Zhang", "zhang.empty@example.com", adviser.Id);

        var response = await client.PutAsJsonAsync($"/customers/{created.Id}", new { id = created.Id });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Update_RouteIdMismatch_Returns400()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.mismatch@acme.com");
        var created = await CreateCustomer(client, "Zhang", "zhang.mismatch@example.com", adviser.Id);

        var response = await client.PutAsJsonAsync(
            $"/customers/{created.Id}",
            new { id = created.Id + 1, name = "Other" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
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

    private static async Task<CreatedIdVm> CreateCustomer(HttpClient client, string name, string email, int adviserId)
    {
        var response = await client.PostAsJsonAsync("/customers", new { name, email, adviserId });
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"POST /customers expected 201 but was {(int)response.StatusCode}: {body}");
        }

        var created = await response.Content.ReadFromJsonAsync<CreatedIdVm>(JsonOptions);
        created.ShouldNotBeNull();
        return created;
    }

    private static async Task<PaginatedCustomers> GetList(HttpClient client, string query = "")
    {
        var response = await client.GetAsync($"/customers{query}");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"GET /customers{query} expected 200 but was {(int)response.StatusCode}: {body}");
        }

        var list = await response.Content.ReadFromJsonAsync<PaginatedCustomers>(JsonOptions);
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

    private static async Task<HttpClient> LoginClient(string email, string password)
    {
        var (status, login) = await PostLogin(email, password);
        status.ShouldBe(HttpStatusCode.OK);
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

    private sealed class PaginatedCustomers
    {
        public List<CustomerVm> Items { get; set; } = [];

        public int PageNumber { get; set; }

        public int TotalPages { get; set; }

        public int TotalCount { get; set; }

        public bool HasPreviousPage { get; set; }

        public bool HasNextPage { get; set; }
    }
}
