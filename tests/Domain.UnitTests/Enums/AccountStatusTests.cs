using MyWealth.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Domain.UnitTests.Enums;

public class AccountStatusTests
{
    [Test]
    public void ShouldUseStableIntegerValues()
    {
        ((int)AccountStatus.Active).ShouldBe(0);
        ((int)AccountStatus.Closed).ShouldBe(1);
    }
}
