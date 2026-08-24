using MyWealth.Application.Accounts;
using MyWealth.Application.Accounts.CloseAccount;
using MyWealth.Application.Accounts.CreateAccount;
using MyWealth.Application.Accounts.GetAccountById;
using MyWealth.Application.Accounts.GetAccounts;
using MyWealth.Application.Accounts.UpdateAccount;
using MyWealth.Application.Common.Models;
using MyWealth.Domain.Enums;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MyWealth.Web.Endpoints;

public class Accounts : IEndpointGroup
{
    public static string RoutePrefix => "/accounts";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetAccounts);
        groupBuilder.MapGet(GetAccountById, "{id}");
        groupBuilder.MapPost(CreateAccount);
        groupBuilder.MapPut(UpdateAccount, "{id}");
        groupBuilder.MapPost(CloseAccount, "{id}/close");
    }

    [EndpointSummary("List accounts")]
    [EndpointDescription("Returns a paginated list of accounts visible to the caller. TenantAdmin sees the whole tenant; Advisers see only accounts belonging to their own customers. Supports filtering by status and customer, and searching by id or name.")]
    public static async Task<Ok<PaginatedList<AccountVm>>> GetAccounts(
        ISender sender,
        int page = 1,
        int pageSize = 20,
        AccountStatus? status = null,
        int? customerId = null,
        string? search = null)
    {
        var result = await sender.Send(new GetAccountsQuery
        {
            Page = page,
            PageSize = pageSize,
            Status = status,
            CustomerId = customerId,
            Search = search
        });

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Get account")]
    [EndpointDescription("Returns a single account by id, scoped to the caller's visibility. Includes the owning customer's name.")]
    public static async Task<Ok<AccountVm>> GetAccountById(ISender sender, int id)
    {
        var result = await sender.Send(new GetAccountByIdQuery { Id = id });

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Create account")]
    [EndpointDescription("Creates an account for a customer. Currency is immutable after create. Advisers may only create accounts for their own customers. TenantAdmin and Adviser.")]
    public static async Task<Created<CreatedIdVm>> CreateAccount(ISender sender, CreateAccountCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/accounts/{id}", new CreatedIdVm { Id = id });
    }

    [EndpointSummary("Update account")]
    [EndpointDescription("Updates an account's name and/or type. Currency, customer and status cannot be changed. Route id must match the body id.")]
    public static async Task<Results<NoContent, BadRequest>> UpdateAccount(
        ISender sender,
        int id,
        UpdateAccountCommand command)
    {
        if (id != command.Id)
        {
            return TypedResults.BadRequest();
        }

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Close account")]
    [EndpointDescription("Permanently closes an account (Status = Closed). Irreversible. Existing history is retained. TenantAdmin and Adviser.")]
    public static async Task<NoContent> CloseAccount(ISender sender, int id)
    {
        await sender.Send(new CloseAccountCommand { Id = id });

        return TypedResults.NoContent();
    }
}
