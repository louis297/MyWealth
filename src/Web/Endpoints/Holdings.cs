using MyWealth.Application.Common.Models;
using MyWealth.Application.Holdings;
using MyWealth.Application.Holdings.CreateHolding;
using MyWealth.Application.Holdings.DeleteHolding;
using MyWealth.Application.Holdings.GetHoldingById;
using MyWealth.Application.Holdings.GetHoldingsByAccount;
using MyWealth.Application.Holdings.UpdateHolding;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MyWealth.Web.Endpoints;

public class Holdings : IEndpointGroup
{
    public static string RoutePrefix => "/accounts/{accountId}/holdings";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetHoldingsByAccount);
        groupBuilder.MapGet(GetHoldingById, "{id}");
        groupBuilder.MapPost(CreateHolding);
        groupBuilder.MapPut(UpdateHolding, "{id}");
        groupBuilder.MapDelete(DeleteHolding, "{id}");
    }

    [EndpointSummary("List holdings")]
    [EndpointDescription("Returns every holding that belongs to the account. TenantAdmin sees the whole tenant; Advisers see only accounts belonging to their own customers. No pagination or search.")]
    public static async Task<Ok<List<HoldingVm>>> GetHoldingsByAccount(ISender sender, int accountId)
    {
        var result = await sender.Send(new GetHoldingsByAccountQuery { AccountId = accountId });

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Get holding")]
    [EndpointDescription("Returns a single holding by id, scoped to the parent account and the caller's visibility.")]
    public static async Task<Ok<HoldingVm>> GetHoldingById(ISender sender, int accountId, int id)
    {
        var result = await sender.Send(new GetHoldingByIdQuery { AccountId = accountId, Id = id });

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Create holding")]
    [EndpointDescription("Creates a holding under an active account. Cost-basis currency must match the account. TenantAdmin and Adviser.")]
    public static async Task<Created<CreatedIdVm>> CreateHolding(
        ISender sender,
        int accountId,
        CreateHoldingCommand command)
    {
        var id = await sender.Send(command with { AccountId = accountId });

        return TypedResults.Created($"/accounts/{accountId}/holdings/{id}", new CreatedIdVm { Id = id });
    }

    [EndpointSummary("Update holding")]
    [EndpointDescription("Partially updates a holding's instrument, quantity and/or cost-basis amount. Currency cannot be changed. Parent account must be Active.")]
    public static async Task<NoContent> UpdateHolding(
        ISender sender,
        int accountId,
        int id,
        UpdateHoldingCommand command)
    {
        await sender.Send(command with { AccountId = accountId, Id = id });

        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete holding")]
    [EndpointDescription("Physically deletes a holding. Parent account must be Active. TenantAdmin and Adviser.")]
    public static async Task<NoContent> DeleteHolding(ISender sender, int accountId, int id)
    {
        await sender.Send(new DeleteHoldingCommand { AccountId = accountId, Id = id });

        return TypedResults.NoContent();
    }
}
