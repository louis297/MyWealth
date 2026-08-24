using MyWealth.Domain.Entities;

namespace MyWealth.Application.Holdings;

internal static class HoldingMappings
{
    public static IQueryable<HoldingVm> ProjectToVm(IQueryable<Holding> holdings)
        => holdings.Select(h => new HoldingVm
        {
            Id = h.Id,
            AccountId = h.AccountId,
            Instrument = new InstrumentVm
            {
                Name = h.Instrument.Name,
                Symbol = h.Instrument.Symbol
            },
            Quantity = h.Quantity,
            CostBasis = new MoneyVm
            {
                Amount = h.CostBasis.Amount,
                Currency = h.CostBasis.Currency
            }
        });
}
