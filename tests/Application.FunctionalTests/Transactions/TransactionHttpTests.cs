using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyWealth.Application.Common.Models;
using MyWealth.Application.Holdings;
using MyWealth.Application.IdentityAuth.Login;
using MyWealth.Application.Transactions;
using MyWealth.Domain.Enums;

namespace MyWealth.Application.FunctionalTests.Transactions;

public class TransactionHttpTests : TestBase
{
    private const string Password = "P@ssw0rd!";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    [Test]
    public async Task TenantAdmin_CanCreateListAndGetTransactions()
    {
        var (client, accountId, holdingId) = await CreateHoldingFixture(
            "jane.tx@acme.com",
            "zhang.tx@example.com");

        var buy = await CreateTransaction(client, BuyBody(accountId, holdingId, "2026-08-20", 100m, 18500m, "Initial purchase"));
        buy.Id.ShouldBeGreaterThan(0);

        var holdingResponse = await client.GetAsync($"/accounts/{accountId}/holdings/{holdingId}");
        holdingResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var holding = await holdingResponse.Content.ReadFromJsonAsync<HoldingVm>(JsonOptions);
        holding.ShouldNotBeNull();
        holding.Quantity.ShouldBe(101m);
        holding.CostBasis.Amount.ShouldBe(18501m);

        var getResponse = await client.GetAsync($"/transactions/{buy.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var transaction = await getResponse.Content.ReadFromJsonAsync<TransactionVm>(JsonOptions);
        transaction.ShouldNotBeNull();
        transaction.Id.ShouldBe(buy.Id);
        transaction.AccountId.ShouldBe(accountId);
        transaction.HoldingId.ShouldBe(holdingId);
        transaction.Type.ShouldBe(TransactionType.Buy);
        transaction.Quantity.ShouldBe(100m);
        transaction.Amount.Amount.ShouldBe(18500m);
        transaction.Amount.Currency.ShouldBe("NZD");
        transaction.Note.ShouldBe("Initial purchase");

        var dividend = await CreateTransaction(client, CashBody(accountId, "Dividend", "2026-08-21", 120.50m, "Q2"));

        var list = await GetList(client, $"?accountId={accountId}");
        list.Items.ShouldContain(t => t.Id == buy.Id);
        list.Items.ShouldContain(t => t.Id == dividend.Id);

        var byType = await GetList(client, $"?accountId={accountId}&type=Buy");
        byType.Items.ShouldContain(t => t.Id == buy.Id);
        byType.Items.ShouldNotContain(t => t.Id == dividend.Id);

        var byDate = await GetList(client, $"?accountId={accountId}&from=2026-08-21&to=2026-08-21");
        byDate.Items.ShouldContain(t => t.Id == dividend.Id);
        byDate.Items.ShouldNotContain(t => t.Id == buy.Id);

        var page = await GetList(client, $"?accountId={accountId}&page=1&pageSize=1");
        page.Items.Count.ShouldBe(1);
        page.PageNumber.ShouldBe(1);
        page.HasNextPage.ShouldBeTrue();
        page.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task CashTypes_LeaveHoldingUntouched()
    {
        var (client, accountId, holdingId) = await CreateHoldingFixture(
            "jane.cash@acme.com",
            "zhang.cash@example.com");

        foreach (var type in new[] { "TransferIn", "TransferOut", "Dividend", "Interest" })
        {
            await CreateTransaction(client, CashBody(accountId, type, "2026-08-20", 10m, type));
        }

        var holding = await (await client.GetAsync($"/accounts/{accountId}/holdings/{holdingId}"))
            .Content.ReadFromJsonAsync<HoldingVm>(JsonOptions);
        holding.ShouldNotBeNull();
        holding.Quantity.ShouldBe(1m);
        holding.CostBasis.Amount.ShouldBe(1m);
    }

    [Test]
    public async Task SellOverQuantity_Returns400()
    {
        var (client, accountId, holdingId) = await CreateHoldingFixture(
            "jane.over@acme.com",
            "zhang.over@example.com");

        var response = await client.PostAsJsonAsync(
            "/transactions",
            BuyBody(accountId, holdingId, "2026-08-20", 2m, 1m, null, "Sell"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CurrencyMismatch_Returns400()
    {
        var (client, accountId, holdingId) = await CreateHoldingFixture(
            "jane.fx@acme.com",
            "zhang.fx@example.com");

        var response = await client.PostAsJsonAsync(
            "/transactions",
            new
            {
                accountId,
                holdingId,
                bookedOn = "2026-08-20",
                type = "Buy",
                amount = new { amount = 1m, currency = "USD" },
                quantity = 1m
            });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ClosedAccount_RejectsPost()
    {
        var (client, accountId, holdingId) = await CreateHoldingFixture(
            "jane.closedtx@acme.com",
            "zhang.closedtx@example.com");

        (await client.PostAsync($"/accounts/{accountId}/close", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await client.PostAsJsonAsync("/transactions", BuyBody(accountId, holdingId, "2026-08-20", 1m, 1m, null)))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task DeleteHolding_WhenTransactionsExist_Returns400()
    {
        var (client, accountId, holdingId) = await CreateHoldingFixture(
            "jane.guard@acme.com",
            "zhang.guard@example.com");

        await CreateTransaction(client, BuyBody(accountId, holdingId, "2026-08-20", 1m, 1m, "buy"));

        (await client.DeleteAsync($"/accounts/{accountId}/holdings/{holdingId}"))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.GetAsync($"/accounts/{accountId}/holdings/{holdingId}"))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task FutureBookedOn_Succeeds()
    {
        var (client, accountId, _) = await CreateHoldingFixture(
            "jane.future@acme.com",
            "zhang.future@example.com");

        var created = await CreateTransaction(client, CashBody(accountId, "Interest", "2099-01-01", 5m, "future"));
        var transaction = await (await client.GetAsync($"/transactions/{created.Id}"))
            .Content.ReadFromJsonAsync<TransactionVm>(JsonOptions);

        transaction.ShouldNotBeNull();
        transaction.BookedOn.ShouldBe(new DateOnly(2099, 1, 1));
    }

    [Test]
    public async Task Adviser_CanOnlySeeOwnCustomersTransactions()
    {
        var (admin, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(admin, "Jane Smith", "jane.owntx@acme.com");
        var otherAdviser = await CreateAdviser(admin, "Bob Jones", "bob.owntx@acme.com");
        var ownCustomer = await CreateCustomer(admin, "Zhang San", "zhang.owntx@example.com", adviser.Id);
        var otherCustomer = await CreateCustomer(admin, "Other Client", "other.owntx@example.com", otherAdviser.Id);
        var ownAccount = await CreateAccount(admin, ownCustomer.Id, "Own Brokerage", "Brokerage", "NZD");
        var otherAccount = await CreateAccount(admin, otherCustomer.Id, "Other Cash", "Cash", "NZD");
        var ownHolding = await CreateHolding(admin, ownAccount.Id, "Own", "OWN", 1m, 1m, "NZD");
        var otherTx = await CreateTransaction(admin, CashBody(otherAccount.Id, "Dividend", "2026-08-20", 1m, "other"));

        using var adviserClient = await LoginClient("jane.owntx@acme.com", Password);

        var created = await CreateTransaction(
            adviserClient,
            BuyBody(ownAccount.Id, ownHolding.Id, "2026-08-20", 1m, 1m, "own"));

        var list = await GetList(adviserClient);
        list.Items.ShouldContain(t => t.Id == created.Id);
        list.Items.ShouldNotContain(t => t.Id == otherTx.Id);

        (await adviserClient.GetAsync($"/transactions/{otherTx.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await adviserClient.PostAsJsonAsync(
                "/transactions",
                CashBody(otherAccount.Id, "Dividend", "2026-08-20", 1m, "hijack")))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var filtered = await GetList(adviserClient, $"?accountId={otherAccount.Id}");
        filtered.Items.ShouldBeEmpty();
    }

    [Test]
    public async Task Anonymous_Returns401()
    {
        using var client = TestApp.CreateClient();

        (await client.GetAsync("/transactions")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/transactions/1")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/transactions", CashBody(1, "Dividend", "2026-08-20", 1m, null)))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SystemAdmin_Returns403()
    {
        using var client = await CreateRoleClient(UserRole.SystemAdmin);

        await ShouldBeStatus(client, HttpMethod.Get, "/transactions", HttpStatusCode.Forbidden);
        await ShouldBeStatus(client, HttpMethod.Get, "/transactions/1", HttpStatusCode.Forbidden);
        (await client.PostAsJsonAsync("/transactions", CashBody(1, "Dividend", "2026-08-20", 1m, null)))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task UnknownId_Returns404()
    {
        var (client, _) = await CreateTenantAdminClient();

        (await client.GetAsync("/transactions/999999")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PostAsJsonAsync("/transactions", CashBody(999999, "Dividend", "2026-08-20", 1m, null)))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CrossTenantId_Returns404()
    {
        var (clientA, _) = await CreateTenantAdminClient();
        var (clientB, accountId, _) = await CreateHoldingFixture(
            "jane.xttx@acme.com",
            "zhang.xttx@example.com");
        var created = await CreateTransaction(clientB, CashBody(accountId, "Dividend", "2026-08-20", 1m, "x"));

        (await clientA.GetAsync($"/transactions/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await clientA.PostAsJsonAsync("/transactions", CashBody(accountId, "Dividend", "2026-08-20", 1m, "hijack")))
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

    private static object BuyBody(
        int accountId,
        int holdingId,
        string bookedOn,
        decimal quantity,
        decimal amount,
        string? note,
        string type = "Buy")
        => new
        {
            accountId,
            holdingId,
            bookedOn,
            type,
            amount = new { amount, currency = "NZD" },
            quantity,
            note
        };

    private static object CashBody(int accountId, string type, string bookedOn, decimal amount, string? note)
        => new
        {
            accountId,
            bookedOn,
            type,
            amount = new { amount, currency = "NZD" },
            note
        };

    private static async Task<CreatedIdVm> CreateTransaction(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/transactions", body);
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var error = await response.Content.ReadAsStringAsync();
            Assert.Fail($"POST /transactions expected 201 but was {(int)response.StatusCode}: {error}");
        }

        var created = await response.Content.ReadFromJsonAsync<CreatedIdVm>(JsonOptions);
        created.ShouldNotBeNull();
        return created;
    }

    private static async Task<PaginatedTransactions> GetList(HttpClient client, string query = "")
    {
        var response = await client.GetAsync($"/transactions{query}");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"GET /transactions{query} expected 200 but was {(int)response.StatusCode}: {body}");
        }

        var list = await response.Content.ReadFromJsonAsync<PaginatedTransactions>(JsonOptions);
        list.ShouldNotBeNull();
        return list;
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
            $"/accounts/{accountId}/holdings",
            new
            {
                instrument = new { name, symbol },
                quantity,
                costBasis = new { amount, currency }
            });
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"POST /accounts/{accountId}/holdings expected 201 but was {(int)response.StatusCode}: {body}");
        }

        var created = await response.Content.ReadFromJsonAsync<CreatedIdVm>(JsonOptions);
        created.ShouldNotBeNull();
        return created;
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

    private sealed class PaginatedTransactions
    {
        public List<TransactionVm> Items { get; set; } = [];

        public int PageNumber { get; set; }

        public int TotalPages { get; set; }

        public int TotalCount { get; set; }

        public bool HasPreviousPage { get; set; }

        public bool HasNextPage { get; set; }
    }
}
