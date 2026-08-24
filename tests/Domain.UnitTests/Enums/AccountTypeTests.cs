using MyWealth.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Domain.UnitTests.Enums;

public class AccountTypeTests
{
    [Test]
    public void ShouldUseStableIntegerValues()
    {
        ((int)AccountType.Bank).ShouldBe(0);
        ((int)AccountType.Cash).ShouldBe(1);
        ((int)AccountType.Brokerage).ShouldBe(2);
        ((int)AccountType.Property).ShouldBe(3);
        ((int)AccountType.Credit).ShouldBe(4);
        ((int)AccountType.Other).ShouldBe(5);
    }
}
