using MyWealth.Application.Dashboard;
using MyWealth.Application.Dashboard.GetAssetAllocation;
using MyWealth.Application.Dashboard.GetNetWorth;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MyWealth.Web.Endpoints;

public class Dashboard : IEndpointGroup
{
    public static string RoutePrefix => "/dashboard";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetNetWorth, "net-worth");
        groupBuilder.MapGet(GetAssetAllocation, "allocation");
    }

    [EndpointSummary("Get net worth")]
    [EndpointDescription("Returns current net worth (assets minus liabilities) for the caller's visible scope, grouped by currency. TenantAdmin sees the whole tenant; Advisers see only their own customers. Optional customerId filters to one visible customer. Closed accounts are excluded. Credit is treated as a liability.")]
    public static async Task<Ok<NetWorthVm>> GetNetWorth(ISender sender, int? customerId = null)
    {
        var result = await sender.Send(new GetNetWorthQuery { CustomerId = customerId });

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Get asset allocation")]
    [EndpointDescription("Returns asset allocation by account type and currency for the caller's visible scope. TenantAdmin sees the whole tenant; Advisers see only their own customers. Optional customerId filters to one visible customer. Closed accounts are excluded.")]
    public static async Task<Ok<AssetAllocationVm>> GetAssetAllocation(ISender sender, int? customerId = null)
    {
        var result = await sender.Send(new GetAssetAllocationQuery { CustomerId = customerId });

        return TypedResults.Ok(result);
    }
}
