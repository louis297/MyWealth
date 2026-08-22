using MyWealth.Application.Common.Models;
using MyWealth.Application.Tenants;
using MyWealth.Application.Tenants.CreateTenant;
using MyWealth.Application.Tenants.GetTenantById;
using MyWealth.Application.Tenants.GetTenants;
using MyWealth.Application.Tenants.UpdateTenant;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MyWealth.Web.Endpoints;

public class Tenants : IEndpointGroup
{
    public static string RoutePrefix => "/tenants";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetTenants);
        groupBuilder.MapGet(GetTenantById, "{id}");
        groupBuilder.MapPost(CreateTenant);
        groupBuilder.MapPut(UpdateTenant, "{id}");
    }

    [EndpointSummary("List tenants")]
    [EndpointDescription("Returns a paginated list of tenants. Supports filtering by enabled state and searching by id or name. SystemAdmin only.")]
    public static async Task<Ok<PaginatedList<TenantVm>>> GetTenants(
        ISender sender,
        int page = 1,
        int pageSize = 20,
        bool? isEnabled = null,
        string? search = null)
    {
        var result = await sender.Send(new GetTenantsQuery
        {
            Page = page,
            PageSize = pageSize,
            IsEnabled = isEnabled,
            Search = search
        });

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Get tenant")]
    [EndpointDescription("Returns a single tenant by id. SystemAdmin only.")]
    public static async Task<Ok<TenantVm>> GetTenantById(ISender sender, int id)
    {
        var result = await sender.Send(new GetTenantByIdQuery { Id = id });

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Create tenant")]
    [EndpointDescription("Creates a tenant. Does not create any user account. Name must be unique (case-insensitive). SystemAdmin only.")]
    public static async Task<Created<CreatedIdVm>> CreateTenant(ISender sender, CreateTenantCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/tenants/{id}", new CreatedIdVm { Id = id });
    }

    [EndpointSummary("Update tenant")]
    [EndpointDescription("Updates a tenant's name and/or enabled state. Route id must match the body id. SystemAdmin only.")]
    public static async Task<Results<NoContent, BadRequest>> UpdateTenant(
        ISender sender,
        int id,
        UpdateTenantCommand command)
    {
        if (id != command.Id)
        {
            return TypedResults.BadRequest();
        }

        await sender.Send(command);

        return TypedResults.NoContent();
    }
}
