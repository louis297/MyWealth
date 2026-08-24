using MyWealth.Application.Common.Models;
using MyWealth.Application.Transactions;
using MyWealth.Application.Transactions.CreateTransaction;
using MyWealth.Application.Transactions.GetTransactionById;
using MyWealth.Application.Transactions.GetTransactions;
using MyWealth.Domain.Enums;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MyWealth.Web.Endpoints;

public class Transactions : IEndpointGroup
{
    public static string RoutePrefix => "/transactions";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetTransactions);
        groupBuilder.MapGet(GetTransactionById, "{id}");
        groupBuilder.MapPost(CreateTransaction);
    }

    [EndpointSummary("List transactions")]
    [EndpointDescription("Returns a paginated list of transactions visible to the caller. TenantAdmin sees the whole tenant; Advisers see only accounts belonging to their own customers. Supports filtering by account, date range and type.")]
    public static async Task<Ok<PaginatedList<TransactionVm>>> GetTransactions(
        ISender sender,
        int page = 1,
        int pageSize = 20,
        int? accountId = null,
        DateOnly? from = null,
        DateOnly? to = null,
        TransactionType? type = null)
    {
        var result = await sender.Send(new GetTransactionsQuery
        {
            Page = page,
            PageSize = pageSize,
            AccountId = accountId,
            From = from,
            To = to,
            Type = type
        });

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Get transaction")]
    [EndpointDescription("Returns a single transaction by id, scoped to the caller's visibility.")]
    public static async Task<Ok<TransactionVm>> GetTransactionById(ISender sender, int id)
    {
        var result = await sender.Send(new GetTransactionByIdQuery { Id = id });

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Create transaction")]
    [EndpointDescription("Posts an append-only transaction against an active account. Buy and Sell adjust the related holding using average cost. TenantAdmin and Adviser.")]
    public static async Task<Created<CreatedIdVm>> CreateTransaction(ISender sender, CreateTransactionCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/transactions/{id}", new CreatedIdVm { Id = id });
    }
}
