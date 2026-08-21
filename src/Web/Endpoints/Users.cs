using MyWealth.Application.IdentityAuth.ChangePassword;
using MyWealth.Application.IdentityAuth.GetCurrentUser;
using MyWealth.Application.IdentityAuth.UpdateCurrentUser;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MyWealth.Web.Endpoints;

public class Users : IEndpointGroup
{
    public static string RoutePrefix => "/users";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetCurrentUser, "me");
        groupBuilder.MapPut(UpdateCurrentUser, "me");
        groupBuilder.MapPut(ChangePassword, "me/password");
    }

    [EndpointSummary("Get current user")]
    [EndpointDescription("Returns the profile of the authenticated user.")]
    public static async Task<Ok<CurrentUserVm>> GetCurrentUser(ISender sender)
    {
        var result = await sender.Send(new GetCurrentUserQuery());

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Update current user")]
    [EndpointDescription("Updates non-password profile fields of the authenticated user. Email, role, tenant and enabled status cannot be changed.")]
    public static async Task<NoContent> UpdateCurrentUser(ISender sender, UpdateCurrentUserCommand command)
    {
        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Change password")]
    [EndpointDescription("Changes the authenticated user's password. The current password is required.")]
    public static async Task<NoContent> ChangePassword(ISender sender, ChangePasswordCommand command)
    {
        await sender.Send(command);

        return TypedResults.NoContent();
    }
}
