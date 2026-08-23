using MyWealth.Application.Common.Models;
using MyWealth.Application.TenantAdmins;
using MyWealth.Application.TenantAdmins.CreateTenantAdmin;
using MyWealth.Application.TenantAdmins.DisableTenantAdmin;
using MyWealth.Application.TenantAdmins.GetTenantAdminById;
using MyWealth.Application.TenantAdmins.GetTenantAdmins;
using MyWealth.Application.TenantAdmins.UpdateTenantAdmin;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MyWealth.Web.Endpoints;

public class TenantAdmins : IEndpointGroup
{
    public static string RoutePrefix => "/tenant-admins";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetTenantAdmins);
        groupBuilder.MapGet(GetTenantAdminById, "{id}");
        groupBuilder.MapPost(CreateTenantAdmin);
        groupBuilder.MapPut(UpdateTenantAdmin, "{id}");
        groupBuilder.MapDelete(DisableTenantAdmin, "{id}");
    }

    [EndpointSummary("List tenant admins")]
    [EndpointDescription("Returns a paginated list of tenant admins. Supports filtering by enabled state and tenant, and searching by id, name or email. SystemAdmin only.")]
    public static async Task<Ok<PaginatedList<TenantAdminVm>>> GetTenantAdmins(
        ISender sender,
        int page = 1,
        int pageSize = 20,
        bool? isEnabled = null,
        int? tenantId = null,
        string? search = null)
    {
        var result = await sender.Send(new GetTenantAdminsQuery
        {
            Page = page,
            PageSize = pageSize,
            IsEnabled = isEnabled,
            TenantId = tenantId,
            Search = search
        });

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Get tenant admin")]
    [EndpointDescription("Returns a single tenant admin by id. SystemAdmin only.")]
    public static async Task<Ok<TenantAdminVm>> GetTenantAdminById(ISender sender, int id)
    {
        var result = await sender.Send(new GetTenantAdminByIdQuery { Id = id });

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Create tenant admin")]
    [EndpointDescription("Creates a tenant admin and a login-capable identity user for an enabled tenant. Email must be globally unique. SystemAdmin only.")]
    public static async Task<Created<CreatedIdVm>> CreateTenantAdmin(
        ISender sender,
        CreateTenantAdminCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/tenant-admins/{id}", new CreatedIdVm { Id = id });
    }

    [EndpointSummary("Update tenant admin")]
    [EndpointDescription("Updates a tenant admin's name and/or enabled state. Route id must match the body id. Disabling the last admin of a tenant is allowed. SystemAdmin only.")]
    public static async Task<Results<NoContent, BadRequest>> UpdateTenantAdmin(
        ISender sender,
        int id,
        UpdateTenantAdminCommand command)
    {
        if (id != command.Id)
        {
            return TypedResults.BadRequest();
        }

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Disable tenant admin")]
    [EndpointDescription("Soft-disables a tenant admin (IsEnabled = false). Disabling the last admin of a tenant is allowed. SystemAdmin only.")]
    public static async Task<NoContent> DisableTenantAdmin(ISender sender, int id)
    {
        await sender.Send(new DisableTenantAdminCommand { Id = id });

        return TypedResults.NoContent();
    }
}
