using MyWealth.Domain.Enums;

namespace MyWealth.Application.Dashboard;

public sealed record AccountContribution(AccountType AccountType, string Currency, decimal Value);
