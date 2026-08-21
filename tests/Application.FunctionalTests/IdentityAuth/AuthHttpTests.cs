using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MyWealth.Application.IdentityAuth.GetCurrentUser;
using MyWealth.Application.IdentityAuth.Login;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Enums;

namespace MyWealth.Application.FunctionalTests.IdentityAuth;

public class AuthHttpTests : TestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Test]
    public async Task Login_SystemAdmin_ReturnsJwtWithNullTenant()
    {
        await TestApp.CreateUserAsync("admin@local", "Administrator1!", UserRole.SystemAdmin, displayName: "System Admin");

        var (status, login) = await PostLogin("admin@local", "Administrator1!");

        status.ShouldBe(HttpStatusCode.OK);
        login.ShouldNotBeNull();
        login.TokenType.ShouldBe("Bearer");
        login.Role.ShouldBe(Roles.SystemAdmin);
        login.TenantId.ShouldBeNull();
        login.DisplayName.ShouldBe("System Admin");
        login.AccessToken.ShouldNotBeNullOrWhiteSpace();

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken);
        jwt.Claims.ShouldNotContain(c => c.Type == "tenant_id" || c.Type == "TenantId");
    }

    [Test]
    public async Task Login_TenantAdmin_IncludesTenantIdClaim()
    {
        await TestApp.CreateUserAsync(
            "tenantadmin@local",
            "TenantAdmin1!",
            UserRole.TenantAdmin,
            tenantId: 7,
            displayName: "Tenant Admin");

        var (status, login) = await PostLogin("tenantadmin@local", "TenantAdmin1!");

        status.ShouldBe(HttpStatusCode.OK);
        login.ShouldNotBeNull();
        login.Role.ShouldBe(Roles.TenantAdmin);
        login.TenantId.ShouldBe(7);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken);
        jwt.Claims.ShouldContain(c => c.Type == "tenant_id" && c.Value == "7");
    }

    [Test]
    public async Task Login_Adviser_Succeeds()
    {
        await TestApp.CreateUserAsync("adviser@local", "Adviser1!", UserRole.Adviser, tenantId: 3);

        var (status, login) = await PostLogin("adviser@local", "Adviser1!");

        status.ShouldBe(HttpStatusCode.OK);
        login.ShouldNotBeNull();
        login.Role.ShouldBe(Roles.Adviser);
        login.TenantId.ShouldBe(3);
    }

    [Test]
    public async Task Login_Customer_Returns403()
    {
        await TestApp.CreateUserAsync("customer@local", "Customer1!", UserRole.Customer, tenantId: 1);

        using var client = TestApp.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new { email = "customer@local", password = "Customer1!" });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("Customer accounts cannot sign in");
    }

    [Test]
    public async Task Login_BadPassword_Returns401()
    {
        await TestApp.CreateUserAsync("admin@local", "Administrator1!", UserRole.SystemAdmin);

        using var client = TestApp.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new { email = "admin@local", password = "WrongPassword1!" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Login_UnknownEmail_Returns401()
    {
        using var client = TestApp.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new { email = "missing@local", password = "Administrator1!" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Login_DisabledUser_Returns401()
    {
        await TestApp.CreateUserAsync(
            "disabled@local",
            "Disabled1!",
            UserRole.Adviser,
            tenantId: 1,
            isEnabled: false);

        using var client = TestApp.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new { email = "disabled@local", password = "Disabled1!" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Login_EmptyBody_Returns400()
    {
        using var client = TestApp.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new { email = "", password = "" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetCurrentUser_Anonymous_Returns401()
    {
        using var client = TestApp.CreateClient();
        var response = await client.GetAsync("/users/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ProfileAndPasswordFlow_Succeeds()
    {
        await TestApp.CreateUserAsync(
            "adviser@local",
            "Adviser1!",
            UserRole.Adviser,
            tenantId: 4,
            displayName: "Old Name");

        using var client = TestApp.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new { email = "adviser@local", password = "Adviser1!" });
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResultVm>(JsonOptions);
        login.ShouldNotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var meResponse = await client.GetAsync("/users/me");
        meResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var me = await meResponse.Content.ReadFromJsonAsync<CurrentUserVm>(JsonOptions);
        me.ShouldNotBeNull();
        me.Email.ShouldBe("adviser@local");
        me.DisplayName.ShouldBe("Old Name");
        me.Role.ShouldBe(Roles.Adviser);
        me.TenantId.ShouldBe(4);

        var updateResponse = await client.PutAsJsonAsync("/users/me", new { displayName = "New Name", email = "hacked@local", role = Roles.SystemAdmin, tenantId = 99, isEnabled = false });
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var meAfterUpdate = await (await client.GetAsync("/users/me")).Content.ReadFromJsonAsync<CurrentUserVm>(JsonOptions);
        meAfterUpdate.ShouldNotBeNull();
        meAfterUpdate.DisplayName.ShouldBe("New Name");
        meAfterUpdate.Email.ShouldBe("adviser@local");
        meAfterUpdate.Role.ShouldBe(Roles.Adviser);
        meAfterUpdate.TenantId.ShouldBe(4);
        meAfterUpdate.IsEnabled.ShouldBeTrue();

        var changePasswordResponse = await client.PutAsJsonAsync("/users/me/password", new { currentPassword = "Adviser1!", newPassword = "Adviser2!" });
        changePasswordResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var reloginClient = TestApp.CreateClient();
        var relogin = await reloginClient.PostAsJsonAsync("/auth/login", new { email = "adviser@local", password = "Adviser2!" });
        relogin.StatusCode.ShouldBe(HttpStatusCode.OK);

        var oldPassword = await reloginClient.PostAsJsonAsync("/auth/login", new { email = "adviser@local", password = "Adviser1!" });
        oldPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var logoutResponse = await client.PostAsync("/auth/logout", null);
        logoutResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task ChangePassword_WrongCurrentPassword_Returns400()
    {
        await TestApp.CreateUserAsync("adviser@local", "Adviser1!", UserRole.Adviser, tenantId: 1);

        var (_, login) = await PostLogin("adviser@local", "Adviser1!");
        login.ShouldNotBeNull();

        using var client = TestApp.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await client.PutAsJsonAsync("/users/me/password", new { currentPassword = "WrongPassword1!", newPassword = "Adviser2!" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
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
