using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyWealth.Application.Common.Models;
using MyWealth.Application.Holdings;
using MyWealth.Application.IdentityAuth.Login;
using MyWealth.Domain.Enums;

namespace MyWealth.Application.FunctionalTests.Holdings;

public class HoldingHttpTests : TestBase
{
    private const string Password = "P@ssw0rd!";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    [Test]
    public async Task TenantAdmin_CanManageHoldings()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane Smith", "jane.hold@acme.com");
        var customer = await CreateCustomer(client, "Zhang San", "zhang.hold@example.com", adviser.Id);
        var account = await CreateAccount(client, customer.Id, "Primary Brokerage", "Brokerage", "NZD");

        var created = await CreateHolding(client, account.Id, "Apple Inc.", "AAPL", 100m, 18500m, "nzd");
        created.Id.ShouldBeGreaterThan(0);

        var getResponse = await client.GetAsync(HoldingUrl(account.Id, created.Id));
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var holding = await getResponse.Content.ReadFromJsonAsync<HoldingVm>(JsonOptions);
        holding.ShouldNotBeNull();
        holding.Id.ShouldBe(created.Id);
        holding.AccountId.ShouldBe(account.Id);
        holding.Instrument.Name.ShouldBe("Apple Inc.");
        holding.Instrument.Symbol.ShouldBe("AAPL");
        holding.Quantity.ShouldBe(100m);
        holding.CostBasis.Amount.ShouldBe(18500m);
        holding.CostBasis.Currency.ShouldBe("NZD");

        var other = await CreateHolding(client, account.Id, "Microsoft", "MSFT", 10m, 4000m, "NZD");

        var list = await GetList(client, account.Id);
        list.ShouldContain(h => h.Id == created.Id && h.Instrument.Name == "Apple Inc.");
        list.ShouldContain(h => h.Id == other.Id);

        (await client.PutAsJsonAsync(HoldingUrl(account.Id, created.Id), new { instrument = new { name = "Apple Inc.", symbol = "AAPL" } }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.PutAsJsonAsync(HoldingUrl(account.Id, created.Id), new { quantity = 120m }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.PutAsJsonAsync(HoldingUrl(account.Id, created.Id), new { costBasis = new { amount = 19200m } }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var updated = await (await client.GetAsync(HoldingUrl(account.Id, created.Id))).Content.ReadFromJsonAsync<HoldingVm>(JsonOptions);
        updated.ShouldNotBeNull();
        updated.Quantity.ShouldBe(120m);
        updated.CostBasis.Amount.ShouldBe(19200m);
        updated.CostBasis.Currency.ShouldBe("NZD");
        updated.Instrument.Name.ShouldBe("Apple Inc.");

        (await client.DeleteAsync(HoldingUrl(account.Id, created.Id))).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.GetAsync(HoldingUrl(account.Id, created.Id))).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var afterDelete = await GetList(client, account.Id);
        afterDelete.ShouldNotContain(h => h.Id == created.Id);
        afterDelete.ShouldContain(h => h.Id == other.Id);
    }

    [Test]
    public async Task ClosedAccount_AllowsReads_RejectsWrites()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.closed@acme.com");
        var customer = await CreateCustomer(client, "Zhang", "zhang.closed@example.com", adviser.Id);
        var account = await CreateAccount(client, customer.Id, "Cash", "Cash", "NZD");
        var created = await CreateHolding(client, account.Id, "Cash Buffer", null, 0m, 0m, "NZD");

        (await client.PostAsync($"/accounts/{account.Id}/close", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await client.GetAsync(HoldingsUrl(account.Id))).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync(HoldingUrl(account.Id, created.Id))).StatusCode.ShouldBe(HttpStatusCode.OK);

        (await client.PostAsJsonAsync(HoldingsUrl(account.Id), NewHoldingBody("X", "X", 1m, 1m, "NZD")))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PutAsJsonAsync(HoldingUrl(account.Id, created.Id), new { quantity = 1m }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.DeleteAsync(HoldingUrl(account.Id, created.Id))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Adviser_CanOnlyManageOwnCustomersHoldings()
    {
        var (admin, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(admin, "Jane Smith", "jane.ownh@acme.com");
        var otherAdviser = await CreateAdviser(admin, "Bob Jones", "bob.ownh@acme.com");
        var ownCustomer = await CreateCustomer(admin, "Zhang San", "zhang.ownh@example.com", adviser.Id);
        var otherCustomer = await CreateCustomer(admin, "Other Client", "other.ownh@example.com", otherAdviser.Id);
        var ownAccount = await CreateAccount(admin, ownCustomer.Id, "Own Brokerage", "Brokerage", "NZD");
        var otherAccount = await CreateAccount(admin, otherCustomer.Id, "Other Cash", "Cash", "NZD");
        var otherHolding = await CreateHolding(admin, otherAccount.Id, "Other", "OTH", 1m, 1m, "NZD");

        using var adviserClient = await LoginClient("jane.ownh@acme.com", Password);

        var created = await CreateHolding(adviserClient, ownAccount.Id, "Own Stock", "OWN", 5m, 50m, "NZD");

        var list = await GetList(adviserClient, ownAccount.Id);
        list.ShouldContain(h => h.Id == created.Id);

        (await adviserClient.GetAsync(HoldingsUrl(otherAccount.Id))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await adviserClient.GetAsync(HoldingUrl(otherAccount.Id, otherHolding.Id))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await adviserClient.PutAsJsonAsync(HoldingUrl(otherAccount.Id, otherHolding.Id), new { quantity = 9m }))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await adviserClient.DeleteAsync(HoldingUrl(otherAccount.Id, otherHolding.Id))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await adviserClient.PostAsJsonAsync(HoldingsUrl(otherAccount.Id), NewHoldingBody("Hijack", "H", 1m, 1m, "NZD")))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await adviserClient.PutAsJsonAsync(HoldingUrl(ownAccount.Id, created.Id), new { quantity = 6m }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await adviserClient.DeleteAsync(HoldingUrl(ownAccount.Id, created.Id))).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Create_CurrencyMismatch_Returns400()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.fx@acme.com");
        var customer = await CreateCustomer(client, "Zhang", "zhang.fx@example.com", adviser.Id);
        var account = await CreateAccount(client, customer.Id, "Cash", "Cash", "NZD");

        var response = await client.PostAsJsonAsync(
            HoldingsUrl(account.Id),
            NewHoldingBody("Apple Inc.", "AAPL", 1m, 1m, "USD"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Create_ZeroQuantity_Succeeds()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.zero@acme.com");
        var customer = await CreateCustomer(client, "Zhang", "zhang.zero@example.com", adviser.Id);
        var account = await CreateAccount(client, customer.Id, "Cash", "Cash", "NZD");

        var created = await CreateHolding(client, account.Id, "Empty Lot", null, 0m, 0m, "NZD");
        var holding = await (await client.GetAsync(HoldingUrl(account.Id, created.Id))).Content.ReadFromJsonAsync<HoldingVm>(JsonOptions);
        holding.ShouldNotBeNull();
        holding.Quantity.ShouldBe(0m);
        holding.Instrument.Symbol.ShouldBeNull();
    }

    [Test]
    public async Task Create_NegativeQuantity_Returns400()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.neg@acme.com");
        var customer = await CreateCustomer(client, "Zhang", "zhang.neg@example.com", adviser.Id);
        var account = await CreateAccount(client, customer.Id, "Cash", "Cash", "NZD");

        (await client.PostAsJsonAsync(HoldingsUrl(account.Id), NewHoldingBody("X", "X", -1m, 1m, "NZD")))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Update_EmptyBody_Returns400()
    {
        var (client, accountId, holdingId) = await CreateHoldingFixture("jane.emptyh@acme.com", "zhang.emptyh@example.com");

        (await client.PutAsJsonAsync(HoldingUrl(accountId, holdingId), new { }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Update_Currency_Returns400()
    {
        var (client, accountId, holdingId) = await CreateHoldingFixture("jane.cur@acme.com", "zhang.cur@example.com");

        (await client.PutAsJsonAsync(HoldingUrl(accountId, holdingId), new { costBasis = new { amount = 1m, currency = "NZD" } }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PutAsJsonAsync(HoldingUrl(accountId, holdingId), new { costBasis = new { amount = 1m, currency = "USD" } }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task HoldingUnderWrongAccount_Returns404()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.wrong@acme.com");
        var customer = await CreateCustomer(client, "Zhang", "zhang.wrong@example.com", adviser.Id);
        var accountA = await CreateAccount(client, customer.Id, "A", "Cash", "NZD");
        var accountB = await CreateAccount(client, customer.Id, "B", "Cash", "NZD");
        var holding = await CreateHolding(client, accountA.Id, "Apple Inc.", "AAPL", 1m, 1m, "NZD");

        (await client.GetAsync(HoldingUrl(accountB.Id, holding.Id))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PutAsJsonAsync(HoldingUrl(accountB.Id, holding.Id), new { quantity = 2m }))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.DeleteAsync(HoldingUrl(accountB.Id, holding.Id))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Anonymous_Returns401()
    {
        using var client = TestApp.CreateClient();

        (await client.GetAsync("/accounts/1/holdings")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/accounts/1/holdings/1")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/accounts/1/holdings", NewHoldingBody("X", "X", 1m, 1m, "NZD")))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PutAsJsonAsync("/accounts/1/holdings/1", new { quantity = 1m }))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.DeleteAsync("/accounts/1/holdings/1")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SystemAdmin_Returns403()
    {
        using var client = await CreateRoleClient(UserRole.SystemAdmin);

        await ShouldBeStatus(client, HttpMethod.Get, "/accounts/1/holdings", HttpStatusCode.Forbidden);
        await ShouldBeStatus(client, HttpMethod.Get, "/accounts/1/holdings/1", HttpStatusCode.Forbidden);
        (await client.PostAsJsonAsync("/accounts/1/holdings", NewHoldingBody("X", "X", 1m, 1m, "NZD")))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await client.PutAsJsonAsync("/accounts/1/holdings/1", new { quantity = 1m }))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await client.DeleteAsync("/accounts/1/holdings/1")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task UnknownIds_Return404()
    {
        var (client, _) = await CreateTenantAdminClient();

        (await client.GetAsync("/accounts/999999/holdings")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.GetAsync("/accounts/999999/holdings/1")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PostAsJsonAsync("/accounts/999999/holdings", NewHoldingBody("X", "X", 1m, 1m, "NZD")))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PutAsJsonAsync("/accounts/999999/holdings/1", new { quantity = 1m }))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.DeleteAsync("/accounts/999999/holdings/1")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CrossTenantId_Returns404()
    {
        var (clientA, _) = await CreateTenantAdminClient();
        var (clientB, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(clientB, "Jane", "jane.xt@acme.com");
        var customer = await CreateCustomer(clientB, "Zhang", "zhang.xt@example.com", adviser.Id);
        var account = await CreateAccount(clientB, customer.Id, "Cash", "Cash", "NZD");
        var holding = await CreateHolding(clientB, account.Id, "Apple Inc.", "AAPL", 1m, 1m, "NZD");

        (await clientA.GetAsync(HoldingsUrl(account.Id))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await clientA.GetAsync(HoldingUrl(account.Id, holding.Id))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await clientA.PutAsJsonAsync(HoldingUrl(account.Id, holding.Id), new { quantity = 2m }))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await clientA.DeleteAsync(HoldingUrl(account.Id, holding.Id))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await clientA.PostAsJsonAsync(HoldingsUrl(account.Id), NewHoldingBody("Hijack", "H", 1m, 1m, "NZD")))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static async Task<(HttpClient Client, int AccountId, int HoldingId)> CreateHoldingFixture(
        string adviserEmail,
        string customerEmail)
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", adviserEmail);
        var customer = await CreateCustomer(client, "Zhang", customerEmail, adviser.Id);
        var account = await CreateAccount(client, customer.Id, "Cash", "Cash", "NZD");
        var holding = await CreateHolding(client, account.Id, "Apple Inc.", "AAPL", 1m, 1m, "NZD");
        return (client, account.Id, holding.Id);
    }

    private static string HoldingsUrl(int accountId) => $"/accounts/{accountId}/holdings";

    private static string HoldingUrl(int accountId, int id) => $"/accounts/{accountId}/holdings/{id}";

    private static object NewHoldingBody(string name, string? symbol, decimal quantity, decimal amount, string currency)
        => new
        {
            instrument = new { name, symbol },
            quantity,
            costBasis = new { amount, currency }
        };

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

    private static async Task<CreatedIdVm> CreateHolding(
        HttpClient client,
        int accountId,
        string name,
        string? symbol,
        decimal quantity,
        decimal amount,
        string currency)
    {
        var response = await client.PostAsJsonAsync(
            HoldingsUrl(accountId),
            NewHoldingBody(name, symbol, quantity, amount, currency));
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"POST {HoldingsUrl(accountId)} expected 201 but was {(int)response.StatusCode}: {body}");
        }

        var created = await response.Content.ReadFromJsonAsync<CreatedIdVm>(JsonOptions);
        created.ShouldNotBeNull();
        return created;
    }

    private static async Task<List<HoldingVm>> GetList(HttpClient client, int accountId)
    {
        var response = await client.GetAsync(HoldingsUrl(accountId));
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"GET {HoldingsUrl(accountId)} expected 200 but was {(int)response.StatusCode}: {body}");
        }

        var list = await response.Content.ReadFromJsonAsync<List<HoldingVm>>(JsonOptions);
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
}
