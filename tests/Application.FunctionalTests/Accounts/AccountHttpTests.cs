using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyWealth.Application.Accounts;
using MyWealth.Application.Common.Models;
using MyWealth.Application.Customers;
using MyWealth.Application.IdentityAuth.Login;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;

namespace MyWealth.Application.FunctionalTests.Accounts;

public class AccountHttpTests : TestBase
{
    private const string Password = "P@ssw0rd!";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    [Test]
    public async Task TenantAdmin_CanManageAccounts()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane Smith", "jane.smith@acme.com");
        var customer = await CreateCustomer(client, "Zhang San", "zhangsan@example.com", adviser.Id);
        var otherCustomer = await CreateCustomer(client, "Li Si", "lisi@example.com", adviser.Id);

        var created = await CreateAccount(client, customer.Id, "Primary Brokerage", "Brokerage", "nzd");
        created.Id.ShouldBeGreaterThan(0);

        var getResponse = await client.GetAsync($"/accounts/{created.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var account = await getResponse.Content.ReadFromJsonAsync<AccountVm>(JsonOptions);
        account.ShouldNotBeNull();
        account.Id.ShouldBe(created.Id);
        account.CustomerId.ShouldBe(customer.Id);
        account.CustomerName.ShouldBe("Zhang San");
        account.Name.ShouldBe("Primary Brokerage");
        account.Type.ShouldBe(AccountType.Brokerage);
        account.Status.ShouldBe(AccountStatus.Active);
        account.Currency.ShouldBe("NZD");

        var other = await CreateAccount(client, otherCustomer.Id, "Everyday Cash", "Cash", "NZD");

        var list = await GetList(client);
        list.Items.ShouldContain(a => a.Id == created.Id && a.Name == "Primary Brokerage");
        list.Items.ShouldContain(a => a.Id == other.Id);

        var searchByName = await GetList(client, "?search=brokerage");
        searchByName.Items.ShouldContain(a => a.Id == created.Id);
        searchByName.Items.ShouldNotContain(a => a.Id == other.Id);

        var searchById = await GetList(client, $"?search={created.Id}");
        searchById.Items.ShouldContain(a => a.Id == created.Id);

        var byCustomer = await GetList(client, $"?customerId={customer.Id}");
        byCustomer.Items.ShouldContain(a => a.Id == created.Id);
        byCustomer.Items.ShouldNotContain(a => a.Id == other.Id);

        var page = await GetList(client, "?page=1&pageSize=1");
        page.Items.Count.ShouldBe(1);
        page.PageNumber.ShouldBe(1);
        page.HasNextPage.ShouldBeTrue();
        page.TotalCount.ShouldBeGreaterThanOrEqualTo(2);

        var renameResponse = await client.PutAsJsonAsync(
            $"/accounts/{created.Id}",
            new { id = created.Id, name = "Main Brokerage" });
        renameResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var typeResponse = await client.PutAsJsonAsync(
            $"/accounts/{created.Id}",
            new { id = created.Id, type = "Bank" });
        typeResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var updated = await (await client.GetAsync($"/accounts/{created.Id}")).Content.ReadFromJsonAsync<AccountVm>(JsonOptions);
        updated.ShouldNotBeNull();
        updated.Name.ShouldBe("Main Brokerage");
        updated.Type.ShouldBe(AccountType.Bank);
        updated.Currency.ShouldBe("NZD");

        var closeResponse = await client.PostAsync($"/accounts/{created.Id}/close", null);
        closeResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var closed = await (await client.GetAsync($"/accounts/{created.Id}")).Content.ReadFromJsonAsync<AccountVm>(JsonOptions);
        closed.ShouldNotBeNull();
        closed.Status.ShouldBe(AccountStatus.Closed);

        var activeOnly = await GetList(client, "?status=Active");
        activeOnly.Items.ShouldNotContain(a => a.Id == created.Id);
        activeOnly.Items.ShouldContain(a => a.Id == other.Id);

        var closedOnly = await GetList(client, "?status=Closed");
        closedOnly.Items.ShouldContain(a => a.Id == created.Id);

        var renameClosed = await client.PutAsJsonAsync(
            $"/accounts/{created.Id}",
            new { id = created.Id, name = "Closed Brokerage" });
        renameClosed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Adviser_CanOnlyManageOwnCustomersAccounts()
    {
        var (admin, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(admin, "Jane Smith", "jane.own@acme.com");
        var otherAdviser = await CreateAdviser(admin, "Bob Jones", "bob.own@acme.com");
        var ownCustomer = await CreateCustomer(admin, "Zhang San", "zhang.own@example.com", adviser.Id);
        var otherCustomer = await CreateCustomer(admin, "Other Client", "other.client@example.com", otherAdviser.Id);
        var otherAccount = await CreateAccount(admin, otherCustomer.Id, "Other Cash", "Cash", "NZD");

        using var adviserClient = await LoginClient("jane.own@acme.com", Password);

        var created = await CreateAccount(adviserClient, ownCustomer.Id, "Own Brokerage", "Brokerage", "NZD");

        var list = await GetList(adviserClient);
        list.Items.ShouldContain(a => a.Id == created.Id);
        list.Items.ShouldNotContain(a => a.Id == otherAccount.Id);

        (await adviserClient.GetAsync($"/accounts/{otherAccount.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await adviserClient.PutAsJsonAsync($"/accounts/{otherAccount.Id}", new { id = otherAccount.Id, name = "Hijack" }))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await adviserClient.PostAsync($"/accounts/{otherAccount.Id}/close", null)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await adviserClient.PutAsJsonAsync($"/accounts/{created.Id}", new { id = created.Id, name = "Own Brokerage Ltd" }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await adviserClient.PostAsync($"/accounts/{created.Id}/close", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Adviser_CreatingForAnotherAdvisersCustomer_Returns400()
    {
        var (admin, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(admin, "Jane", "jane.assign@acme.com");
        var otherAdviser = await CreateAdviser(admin, "Bob", "bob.assign@acme.com");
        var otherCustomer = await CreateCustomer(admin, "Other", "other.assign@example.com", otherAdviser.Id);

        using var adviserClient = await LoginClient("jane.assign@acme.com", Password);

        var create = await adviserClient.PostAsJsonAsync("/accounts", new
        {
            customerId = otherCustomer.Id,
            name = "Hijack",
            type = "Cash",
            currency = "NZD"
        });
        create.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var ownCustomer = await CreateCustomer(adviserClient, "Zhang", "zhang.assign@example.com", adviser.Id);
        (await CreateAccount(adviserClient, ownCustomer.Id, "Own Cash", "Cash", "NZD")).Id.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task Create_InvalidCustomer_Returns400()
    {
        var (client, tenantId) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.invalid@acme.com");
        var customer = await CreateCustomer(client, "Zhang", "zhang.invalid@example.com", adviser.Id);

        (await client.PostAsJsonAsync("/accounts", new
        {
            customerId = 999999,
            name = "Cash",
            type = "Cash",
            currency = "NZD"
        })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        (await client.DeleteAsync($"/customers/{customer.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await client.PostAsJsonAsync("/accounts", new
        {
            customerId = customer.Id,
            name = "Cash",
            type = "Cash",
            currency = "NZD"
        })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        (await client.PostAsJsonAsync("/accounts", new
        {
            customerId = adviser.Id,
            name = "Cash",
            type = "Cash",
            currency = "NZD"
        })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var tenantAdmin = User.CreateTenantAdmin(tenantId, "Other Admin", "other.ta.invalid@acme.com");
        await TestApp.AddAsync(tenantAdmin);

        (await client.PostAsJsonAsync("/accounts", new
        {
            customerId = tenantAdmin.Id,
            name = "Cash",
            type = "Cash",
            currency = "NZD"
        })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Create_MissingFields_Returns400()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.missing@acme.com");
        var customer = await CreateCustomer(client, "Zhang", "zhang.missing@example.com", adviser.Id);

        (await client.PostAsJsonAsync("/accounts", new { name = "Cash", type = "Cash", currency = "NZD" }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/accounts", new { customerId = customer.Id, type = "Cash", currency = "NZD" }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/accounts", new { customerId = customer.Id, name = "Cash", currency = "NZD" }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/accounts", new { customerId = customer.Id, name = "Cash", type = "Cash" }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Close_AlreadyClosed_Returns400()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.close@acme.com");
        var customer = await CreateCustomer(client, "Zhang", "zhang.close@example.com", adviser.Id);
        var created = await CreateAccount(client, customer.Id, "Cash", "Cash", "NZD");

        (await client.PostAsync($"/accounts/{created.Id}/close", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.PostAsync($"/accounts/{created.Id}/close", null)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Update_EmptyBody_Returns400()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.empty@acme.com");
        var customer = await CreateCustomer(client, "Zhang", "zhang.empty@example.com", adviser.Id);
        var created = await CreateAccount(client, customer.Id, "Cash", "Cash", "NZD");

        var response = await client.PutAsJsonAsync($"/accounts/{created.Id}", new { id = created.Id });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Update_ForbiddenFields_Returns400()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.forbidden@acme.com");
        var customer = await CreateCustomer(client, "Zhang", "zhang.forbidden@example.com", adviser.Id);
        var created = await CreateAccount(client, customer.Id, "Cash", "Cash", "NZD");

        (await client.PutAsJsonAsync($"/accounts/{created.Id}", new { id = created.Id, name = "Cash", currency = "USD" }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PutAsJsonAsync($"/accounts/{created.Id}", new { id = created.Id, name = "Cash", customerId = customer.Id }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PutAsJsonAsync($"/accounts/{created.Id}", new { id = created.Id, name = "Cash", status = "Closed" }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Update_RouteIdMismatch_Returns400()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.mismatch@acme.com");
        var customer = await CreateCustomer(client, "Zhang", "zhang.mismatch@example.com", adviser.Id);
        var created = await CreateAccount(client, customer.Id, "Cash", "Cash", "NZD");

        var response = await client.PutAsJsonAsync(
            $"/accounts/{created.Id}",
            new { id = created.Id + 1, name = "Other" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Anonymous_Returns401()
    {
        using var client = TestApp.CreateClient();

        (await client.GetAsync("/accounts")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/accounts/1")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/accounts", new { customerId = 1, name = "Cash", type = "Cash", currency = "NZD" }))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PutAsJsonAsync("/accounts/1", new { id = 1, name = "Cash" }))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PostAsync("/accounts/1/close", null)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SystemAdmin_Returns403()
    {
        using var client = await CreateRoleClient(UserRole.SystemAdmin);

        await ShouldBeStatus(client, HttpMethod.Get, "/accounts", HttpStatusCode.Forbidden);
        await ShouldBeStatus(client, HttpMethod.Get, "/accounts/1", HttpStatusCode.Forbidden);
        (await client.PostAsJsonAsync("/accounts", new { customerId = 1, name = "Cash", type = "Cash", currency = "NZD" }))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await client.PutAsJsonAsync("/accounts/1", new { id = 1, name = "Cash" }))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await client.PostAsync("/accounts/1/close", null)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetUpdateClose_UnknownId_Returns404()
    {
        var (client, _) = await CreateTenantAdminClient();

        (await client.GetAsync("/accounts/999999")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PutAsJsonAsync("/accounts/999999", new { id = 999999, name = "Missing" }))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PostAsync("/accounts/999999/close", null)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CrossTenantId_Returns404()
    {
        var (clientA, _) = await CreateTenantAdminClient();
        var (clientB, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(clientB, "Jane", "jane.other@acme.com");
        var customer = await CreateCustomer(clientB, "Zhang", "zhang.other@example.com", adviser.Id);
        var created = await CreateAccount(clientB, customer.Id, "Cash", "Cash", "NZD");

        (await clientA.GetAsync($"/accounts/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await clientA.PutAsJsonAsync($"/accounts/{created.Id}", new { id = created.Id, name = "Hijack" }))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await clientA.PostAsync($"/accounts/{created.Id}/close", null)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DisableCustomer_WithActiveAccount_Returns400_ThenSucceedsAfterClose()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.guard@acme.com");
        var customer = await CreateCustomer(client, "Zhang", "zhang.guard@example.com", adviser.Id);
        var created = await CreateAccount(client, customer.Id, "Cash", "Cash", "NZD");

        (await client.DeleteAsync($"/customers/{customer.Id}")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PutAsJsonAsync($"/customers/{customer.Id}", new { id = customer.Id, isEnabled = false }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        (await client.PostAsync($"/accounts/{created.Id}/close", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.DeleteAsync($"/customers/{customer.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var disabled = await (await client.GetAsync($"/customers/{customer.Id}")).Content.ReadFromJsonAsync<CustomerVm>(JsonOptions);
        disabled.ShouldNotBeNull();
        disabled.IsEnabled.ShouldBeFalse();
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

    private static async Task<CreatedIdVm> CreateAccount(
        HttpClient client,
        int customerId,
        string name,
        string type,
        string currency)
    {
        var response = await client.PostAsJsonAsync("/accounts", new { customerId, name, type, currency });
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"POST /accounts expected 201 but was {(int)response.StatusCode}: {body}");
        }

        var created = await response.Content.ReadFromJsonAsync<CreatedIdVm>(JsonOptions);
        created.ShouldNotBeNull();
        return created;
    }

    private static async Task<PaginatedAccounts> GetList(HttpClient client, string query = "")
    {
        var response = await client.GetAsync($"/accounts{query}");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"GET /accounts{query} expected 200 but was {(int)response.StatusCode}: {body}");
        }

        var list = await response.Content.ReadFromJsonAsync<PaginatedAccounts>(JsonOptions);
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

    private sealed class PaginatedAccounts
    {
        public List<AccountVm> Items { get; set; } = [];

        public int PageNumber { get; set; }

        public int TotalPages { get; set; }

        public int TotalCount { get; set; }

        public bool HasPreviousPage { get; set; }

        public bool HasNextPage { get; set; }
    }
}
