using BankingApi.Infrastructure.Accounts;
using FluentAssertions;

namespace BankingApi.UnitTests.Infrastructure;

public class RandomAccountNumberGeneratorTests
{
    private readonly RandomAccountNumberGenerator _generator = new();

    [Fact]
    public void Generate_Always_ReturnsTwelveDigitsWithoutLeadingZero()
    {
        for (var i = 0; i < 1_000; i++)
        {
            var accountNumber = _generator.Generate();

            accountNumber.Should().MatchRegex("^[1-9][0-9]{11}$");
        }
    }

    [Fact]
    public void Generate_CalledRepeatedly_ProducesDistinctNumbers()
    {
        var numbers = Enumerable.Range(0, 1_000).Select(_ => _generator.Generate()).ToList();

        numbers.Should().OnlyHaveUniqueItems();
    }
}
