using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyWealth.Application.Common.Models;
using MyWealth.Application.Dashboard;
using MyWealth.Application.IdentityAuth.Login;
using MyWealth.Domain.Enums;

namespace MyWealth.Application.FunctionalTests.Dashboard;

public class DashboardHttpTests : TestBase
{
    private const string Password = "P@ssw0rd!";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    [Test]
    public async Task TenantAdmin_SeesWholeTenant_MixedAccountTypesAndCurrencies()
    {
        var fixture = await CreateMixedFixture();

        var netWorth = await GetNetWorth(fixture.Admin);
        netWorth.Items.Count.ShouldBe(2);

        var nzd = netWorth.Items.Single(i => i.Currency == "NZD");
        nzd.Assets.ShouldBe(19460m);
        nzd.Liabilities.ShouldBe(500m);
        nzd.Net.ShouldBe(18960m);

        var usd = netWorth.Items.Single(i => i.Currency == "USD");
        usd.Assets.ShouldBe(75m);
        usd.Liabilities.ShouldBe(0m);
        usd.Net.ShouldBe(75m);

        var allocation = await GetAllocation(fixture.Admin);
        allocation.Items.ShouldContain(i => i.AccountType == AccountType.Bank && i.Currency == "NZD" && i.Value == 860m);
        allocation.Items.ShouldContain(i => i.AccountType == AccountType.Brokerage && i.Currency == "NZD" && i.Value == 18500m);
        allocation.Items.ShouldContain(i => i.AccountType == AccountType.Cash && i.Currency == "NZD" && i.Value == 100m);
        allocation.Items.ShouldContain(i => i.AccountType == AccountType.Cash && i.Currency == "USD" && i.Value == 75m);
        allocation.Items.ShouldContain(i => i.AccountType == AccountType.Credit && i.Currency == "NZD" && i.Value == 500m);
    }

    [Test]
    public async Task CustomerId_FiltersToThatCustomer()
    {
        var fixture = await CreateMixedFixture();

        var netWorth = await GetNetWorth(fixture.Admin, $"?customerId={fixture.OwnCustomerId}");
        netWorth.Items.Count.ShouldBe(2);

        var nzd = netWorth.Items.Single(i => i.Currency == "NZD");
        nzd.Assets.ShouldBe(19360m);
        nzd.Liabilities.ShouldBe(500m);
        nzd.Net.ShouldBe(18860m);

        var allocation = await GetAllocation(fixture.Admin, $"?customerId={fixture.OwnCustomerId}");
        allocation.Items.ShouldNotContain(i => i.AccountType == AccountType.Cash && i.Currency == "NZD");
        allocation.Items.ShouldContain(i => i.AccountType == AccountType.Bank && i.Value == 860m);
    }

    [Test]
    public async Task Adviser_SeesOnlyOwnCustomers()
    {
        var fixture = await CreateMixedFixture();
        using var adviser = await LoginClient("jane.dash@acme.com", Password);

        var netWorth = await GetNetWorth(adviser);
        var nzd = netWorth.Items.Single(i => i.Currency == "NZD");
        nzd.Assets.ShouldBe(19360m);
        nzd.Liabilities.ShouldBe(500m);
        nzd.Net.ShouldBe(18860m);

        var allocation = await GetAllocation(adviser);
        allocation.Items.ShouldNotContain(i => i.AccountType == AccountType.Cash && i.Currency == "NZD" && i.Value == 100m);
    }

    [Test]
    public async Task ClosedAccount_IsExcluded()
    {
        var fixture = await CreateMixedFixture();

        (await fixture.Admin.PostAsync($"/accounts/{fixture.BankAccountId}/close", null))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var netWorth = await GetNetWorth(fixture.Admin);
        var nzd = netWorth.Items.Single(i => i.Currency == "NZD");
        nzd.Assets.ShouldBe(18600m);
        nzd.Liabilities.ShouldBe(500m);
        nzd.Net.ShouldBe(18100m);

        var allocation = await GetAllocation(fixture.Admin);
        allocation.Items.ShouldNotContain(i => i.AccountType == AccountType.Bank);
    }

    [Test]
    public async Task EmptyTenant_ReturnsEmptyArrays()
    {
        var (client, _) = await CreateTenantAdminClient();

        var netWorth = await GetNetWorth(client);
        netWorth.Items.ShouldBeEmpty();

        var allocation = await GetAllocation(client);
        allocation.Items.ShouldBeEmpty();
    }

    [Test]
    public async Task ActiveAccountWithNoData_ReturnsZeroRow()
    {
        var (client, _) = await CreateTenantAdminClient();
        var adviser = await CreateAdviser(client, "Jane", "jane.zero@acme.com");
        var customer = await CreateCustomer(client, "Zhang", "zhang.zero@example.com", adviser.Id);
        await CreateAccount(client, customer.Id, "Empty Bank", "Bank", "NZD");

        var netWorth = await GetNetWorth(client);
        netWorth.Items.Count.ShouldBe(1);
        netWorth.Items[0].Currency.ShouldBe("NZD");
        netWorth.Items[0].Assets.ShouldBe(0m);
        netWorth.Items[0].Liabilities.ShouldBe(0m);
        netWorth.Items[0].Net.ShouldBe(0m);

        var allocation = await GetAllocation(client);
        allocation.Items.Count.ShouldBe(1);
        allocation.Items[0].AccountType.ShouldBe(AccountType.Bank);
        allocation.Items[0].Value.ShouldBe(0m);
    }

    [Test]
    public async Task InvisibleCustomerId_Returns404()
    {
        var fixture = await CreateMixedFixture();
        using var adviser = await LoginClient("jane.dash@acme.com", Password);

        (await adviser.GetAsync($"/dashboard/net-worth?customerId={fixture.OtherCustomerId}"))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await adviser.GetAsync($"/dashboard/allocation?customerId={fixture.OtherCustomerId}"))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await fixture.Admin.GetAsync("/dashboard/net-worth?customerId=999999"))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await fixture.Admin.GetAsync("/dashboard/allocation?customerId=999999"))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var (otherTenant, _) = await CreateTenantAdminClient();
        (await otherTenant.GetAsync($"/dashboard/net-worth?customerId={fixture.OwnCustomerId}"))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await otherTenant.GetAsync($"/dashboard/allocation?customerId={fixture.OwnCustomerId}"))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Anonymous_Returns401()
    {
        using var client = TestApp.CreateClient();

        (await client.GetAsync("/dashboard/net-worth")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/dashboard/allocation")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SystemAdmin_Returns403()
    {
        using var client = await CreateRoleClient(UserRole.SystemAdmin);

        await ShouldBeStatus(client, HttpMethod.Get, "/dashboard/net-worth", HttpStatusCode.Forbidden);
        await ShouldBeStatus(client, HttpMethod.Get, "/dashboard/allocation", HttpStatusCode.Forbidden);
    }

    private static async Task<MixedFixture> CreateMixedFixture()
    {
        var (admin, _) = await CreateTenantAdminClient();
        var jane = await CreateAdviser(admin, "Jane", "jane.dash@acme.com");
        var bob = await CreateAdviser(admin, "Bob", "bob.dash@acme.com");
        var zhang = await CreateCustomer(admin, "Zhang", "zhang.dash@example.com", jane.Id);
        var li = await CreateCustomer(admin, "Li", "li.dash@example.com", bob.Id);

        var bank = await CreateAccount(admin, zhang.Id, "Everyday Bank", "Bank", "NZD");
        var brokerage = await CreateAccount(admin, zhang.Id, "Primary Brokerage", "Brokerage", "NZD");
        var credit = await CreateAccount(admin, zhang.Id, "Visa", "Credit", "NZD");
        var usdCash = await CreateAccount(admin, zhang.Id, "USD Cash", "Cash", "USD");
        var nzdCash = await CreateAccount(admin, li.Id, "Li Cash", "Cash", "NZD");

        await CreateTransaction(admin, CashBody(bank.Id, "TransferIn", 1000m));
        await CreateTransaction(admin, CashBody(bank.Id, "TransferOut", 200m));
        await CreateTransaction(admin, CashBody(bank.Id, "Dividend", 50m));
        await CreateTransaction(admin, CashBody(bank.Id, "Interest", 10m));
        await CreateHolding(admin, brokerage.Id, "Apple Inc.", "AAPL", 100m, 18500m, "NZD");
        await CreateTransaction(admin, CashBody(credit.Id, "TransferIn", 500m));
        await CreateTransaction(admin, CashBody(usdCash.Id, "TransferIn", 75m, "USD"));
        await CreateTransaction(admin, CashBody(nzdCash.Id, "TransferIn", 100m));

        return new MixedFixture(admin, zhang.Id, li.Id, bank.Id);
    }

    private static async Task<NetWorthVm> GetNetWorth(HttpClient client, string query = "")
    {
        var response = await client.GetAsync($"/dashboard/net-worth{query}");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"GET /dashboard/net-worth{query} expected 200 but was {(int)response.StatusCode}: {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<NetWorthVm>(JsonOptions);
        result.ShouldNotBeNull();
        return result;
    }

    private static async Task<AssetAllocationVm> GetAllocation(HttpClient client, string query = "")
    {
        var response = await client.GetAsync($"/dashboard/allocation{query}");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"GET /dashboard/allocation{query} expected 200 but was {(int)response.StatusCode}: {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<AssetAllocationVm>(JsonOptions);
        result.ShouldNotBeNull();
        return result;
    }

    private static object CashBody(int accountId, string type, decimal amount, string currency = "NZD")
        => new
        {
            accountId,
            bookedOn = "2026-08-20",
            type,
            amount = new { amount, currency }
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

    private static async Task CreateTransaction(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/transactions", body);
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var error = await response.Content.ReadAsStringAsync();
            Assert.Fail($"POST /transactions expected 201 but was {(int)response.StatusCode}: {error}");
        }
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

    private sealed record MixedFixture(
        HttpClient Admin,
        int OwnCustomerId,
        int OtherCustomerId,
        int BankAccountId);
}
