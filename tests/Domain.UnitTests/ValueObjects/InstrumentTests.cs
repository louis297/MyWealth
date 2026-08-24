using MyWealth.Domain.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Domain.UnitTests.ValueObjects;

public class InstrumentTests
{
    [Test]
    public void Create_TrimsNameAndSymbol()
    {
        var instrument = Instrument.Create("  Apple Inc.  ", "  AAPL  ");

        instrument.Name.ShouldBe("Apple Inc.");
        instrument.Symbol.ShouldBe("AAPL");
    }

    [Test]
    public void Create_AllowsMissingSymbol()
    {
        var instrument = Instrument.Create("Private Property");

        instrument.Name.ShouldBe("Private Property");
        instrument.Symbol.ShouldBeNull();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_TreatsBlankSymbolAsNull(string? symbol)
    {
        var instrument = Instrument.Create("Apple Inc.", symbol);

        instrument.Symbol.ShouldBeNull();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_RejectsMissingName(string? name)
    {
        var action = () => Instrument.Create(name!);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("name");
    }

    [Test]
    public void Create_RejectsNameLongerThanMaxLength()
    {
        var name = new string('a', Instrument.NameMaxLength + 1);

        var action = () => Instrument.Create(name);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("name");
    }

    [Test]
    public void Create_RejectsSymbolLongerThanMaxLength()
    {
        var symbol = new string('A', Instrument.SymbolMaxLength + 1);

        var action = () => Instrument.Create("Apple Inc.", symbol);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("symbol");
    }
}
