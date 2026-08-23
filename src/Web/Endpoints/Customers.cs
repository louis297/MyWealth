using MyWealth.Application.Common.Models;
using MyWealth.Application.Customers;
using MyWealth.Application.Customers.CreateCustomer;
using MyWealth.Application.Customers.DisableCustomer;
using MyWealth.Application.Customers.GetCustomerById;
using MyWealth.Application.Customers.GetCustomers;
using MyWealth.Application.Customers.UpdateCustomer;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MyWealth.Web.Endpoints;

public class Customers : IEndpointGroup
{
    public static string RoutePrefix => "/customers";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetCustomers);
        groupBuilder.MapGet(GetCustomerById, "{id}");
        groupBuilder.MapPost(CreateCustomer);
        groupBuilder.MapPut(UpdateCustomer, "{id}");
        groupBuilder.MapDelete(DisableCustomer, "{id}");
    }

    [EndpointSummary("List customers")]
    [EndpointDescription("Returns a paginated list of customers visible to the caller. TenantAdmin sees the whole tenant; Advisers see only their own customers. Supports filtering by enabled state and searching by id, name or email.")]
    public static async Task<Ok<PaginatedList<CustomerVm>>> GetCustomers(
        ISender sender,
        int page = 1,
        int pageSize = 20,
        bool? isEnabled = null,
        string? search = null)
    {
        var result = await sender.Send(new GetCustomersQuery
        {
            Page = page,
            PageSize = pageSize,
            IsEnabled = isEnabled,
            Search = search
        });

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Get customer")]
    [EndpointDescription("Returns a single customer by id, scoped to the caller's visibility. Includes the assigned adviser's name.")]
    public static async Task<Ok<CustomerVm>> GetCustomerById(ISender sender, int id)
    {
        var result = await sender.Send(new GetCustomerByIdQuery { Id = id });

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Create customer")]
    [EndpointDescription("Creates a customer bound to an adviser. Does not create a login. Advisers may only assign customers to themselves. TenantAdmin and Adviser.")]
    public static async Task<Created<CreatedIdVm>> CreateCustomer(ISender sender, CreateCustomerCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/customers/{id}", new CreatedIdVm { Id = id });
    }

    [EndpointSummary("Update customer")]
    [EndpointDescription("Updates a customer's name, enabled state and/or assigned adviser. Route id must match the body id. Advisers may only reassign to themselves.")]
    public static async Task<Results<NoContent, BadRequest>> UpdateCustomer(
        ISender sender,
        int id,
        UpdateCustomerCommand command)
    {
        if (id != command.Id)
        {
            return TypedResults.BadRequest();
        }

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Disable customer")]
    [EndpointDescription("Soft-disables a customer (IsEnabled = false). TenantAdmin and Adviser.")]
    public static async Task<NoContent> DisableCustomer(ISender sender, int id)
    {
        await sender.Send(new DisableCustomerCommand { Id = id });

        return TypedResults.NoContent();
    }
}
