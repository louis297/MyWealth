using MyWealth.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Domain.UnitTests.Enums;

public class TransactionTypeTests
{
    [Test]
    public void ShouldUseStableIntegerValues()
    {
        ((int)TransactionType.Buy).ShouldBe(0);
        ((int)TransactionType.Sell).ShouldBe(1);
        ((int)TransactionType.TransferIn).ShouldBe(2);
        ((int)TransactionType.TransferOut).ShouldBe(3);
        ((int)TransactionType.Dividend).ShouldBe(4);
        ((int)TransactionType.Interest).ShouldBe(5);
    }
}
