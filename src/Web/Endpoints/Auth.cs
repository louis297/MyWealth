using MyWealth.Application.IdentityAuth.Login;
using MyWealth.Application.IdentityAuth.Logout;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MyWealth.Web.Endpoints;

public class Auth : IEndpointGroup
{
    public static string RoutePrefix => "/auth";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Login, "login")
            .AllowAnonymous()
            .Produces<LoginResultVm>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        groupBuilder.MapPost(Logout, "logout")
            .RequireAuthorization();
    }

    [EndpointSummary("Log in")]
    [EndpointDescription("Authenticates with email and password and returns a JWT. Customer-role accounts are rejected with 403.")]
    public static async Task<Ok<LoginResultVm>> Login(ISender sender, LoginCommand command)
    {
        var result = await sender.Send(command);

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Log out")]
    [EndpointDescription("Ends the current session. JWTs are not blacklisted; the client must discard the token.")]
    public static async Task<NoContent> Logout(ISender sender)
    {
        await sender.Send(new LogoutCommand());

        return TypedResults.NoContent();
    }
}
