using BankingApi.Application.Common.Security;
using FluentAssertions;

namespace BankingApi.UnitTests.Application.Security;

public class SensitiveDataMaskerTests
{
    [Theory]
    [InlineData("199012345678", "********5678")]
    [InlineData("12345", "*2345")]
    [InlineData("1234", "****")]
    [InlineData("12", "**")]
    [InlineData("", "")]
    public void MaskNationalId_Always_ShowsAtMostLastFourCharacters(string nationalId, string expected)
    {
        SensitiveDataMasker.MaskNationalId(nationalId).Should().Be(expected);
    }
}
