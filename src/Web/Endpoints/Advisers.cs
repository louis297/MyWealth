using MyWealth.Application.Advisers;
using MyWealth.Application.Advisers.CreateAdviser;
using MyWealth.Application.Advisers.DisableAdviser;
using MyWealth.Application.Advisers.GetAdviserById;
using MyWealth.Application.Advisers.GetAdvisers;
using MyWealth.Application.Advisers.UpdateAdviser;
using MyWealth.Application.Common.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MyWealth.Web.Endpoints;

public class Advisers : IEndpointGroup
{
    public static string RoutePrefix => "/advisers";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetAdvisers);
        groupBuilder.MapGet(GetAdviserById, "{id}");
        groupBuilder.MapPost(CreateAdviser);
        groupBuilder.MapPut(UpdateAdviser, "{id}");
        groupBuilder.MapDelete(DisableAdviser, "{id}");
    }

    [EndpointSummary("List advisers")]
    [EndpointDescription("Returns a paginated list of advisers in the current tenant. Supports filtering by enabled state and searching by id, name or email. TenantAdmin only.")]
    public static async Task<Ok<PaginatedList<AdviserVm>>> GetAdvisers(
        ISender sender,
        int page = 1,
        int pageSize = 20,
        bool? isEnabled = null,
        string? search = null)
    {
        var result = await sender.Send(new GetAdvisersQuery
        {
            Page = page,
            PageSize = pageSize,
            IsEnabled = isEnabled,
            Search = search
        });

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Get adviser")]
    [EndpointDescription("Returns a single adviser by id, scoped to the current tenant. TenantAdmin only.")]
    public static async Task<Ok<AdviserVm>> GetAdviserById(ISender sender, int id)
    {
        var result = await sender.Send(new GetAdviserByIdQuery { Id = id });

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Create adviser")]
    [EndpointDescription("Creates an adviser and a login-capable identity user. Email must be globally unique. TenantAdmin only.")]
    public static async Task<Created<CreatedIdVm>> CreateAdviser(ISender sender, CreateAdviserCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/advisers/{id}", new CreatedIdVm { Id = id });
    }

    [EndpointSummary("Update adviser")]
    [EndpointDescription("Updates an adviser's name and/or enabled state. Route id must match the body id. Disabling fails if customers are still assigned. TenantAdmin only.")]
    public static async Task<Results<NoContent, BadRequest>> UpdateAdviser(
        ISender sender,
        int id,
        UpdateAdviserCommand command)
    {
        if (id != command.Id)
        {
            return TypedResults.BadRequest();
        }

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Disable adviser")]
    [EndpointDescription("Soft-disables an adviser (IsEnabled = false). Fails with 400 if any customer is still assigned. TenantAdmin only.")]
    public static async Task<NoContent> DisableAdviser(ISender sender, int id)
    {
        await sender.Send(new DisableAdviserCommand { Id = id });

        return TypedResults.NoContent();
    }
}
