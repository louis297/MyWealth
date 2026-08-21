using MyWealth.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MyWealth.Application.FunctionalTests.Infrastructure;

public class WebApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .UseSetting("ConnectionStrings:MyWealthDb", connectionString)
            .UseSetting("Jwt:Issuer", "MyWealth")
            .UseSetting("Jwt:Audience", "MyWealth")
            .UseSetting("Jwt:Key", "TEST_ONLY_MyWealth_jwt_signing_key_32+")
            .UseSetting("Jwt:ExpiryMinutes", "480");

        builder.ConfigureTestServices(services =>
        {
            services
                .RemoveAll<IUser>()
                .AddTransient<IUser, TestUser>();
        });
    }
}
