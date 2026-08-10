using FluentAssertions;
using OLS.Business.Common;

namespace OLS.Business.Tests;

/// <summary>
/// TurkishDecimal, para/hacim/ağırlık gibi alanların FormData'dan doğru
/// ayrıştırılmasından sorumlu — yanlış ayrıştırma parayı 10x/100x büyütüp
/// küçültebilir (bkz. TurkishDecimal.cs üstündeki DATA-002 sınıfı not).
/// </summary>
public sealed class TurkishDecimalTests
{
    [Theory]
    [InlineData("1234.56", 1234.56)]
    [InlineData("1234,56", 1234.56)]
    [InlineData("32,5", 32.5)]
    [InlineData("0,5", 0.5)]
    [InlineData("100", 100)]
    public void Parse_SimpleDecimalOrCommaForm_ReturnsExpectedValue(string input, double expected)
    {
        TurkishDecimal.Parse(input).Should().Be((decimal)expected);
    }

    [Theory]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1,234.56", 1234.56)]
    public void Parse_ThousandsSeparatorWithDecimal_ParsesLastSeparatorAsDecimalPoint(string input, double expected)
    {
        TurkishDecimal.Parse(input).Should().Be((decimal)expected);
    }

    [Fact]
    public void Parse_ThousandsSeparatorOnly_TreatsAsThousandsNotDecimal()
    {
        // Tek ayraç + tam 3 haneli kesir -> binlik ayraç ("1.250" = bin iki yüz elli,
        // 1.25 DEĞİL). Bu, olsold'daki str_replace(',', '.') hatasının düzeltmesi.
        TurkishDecimal.Parse("1.250").Should().Be(1250m);
        TurkishDecimal.Parse("1,250").Should().Be(1250m);
    }

    [Fact]
    public void Parse_SingleSeparatorWithTwoFractionDigits_TreatsAsDecimalNotThousands()
    {
        // Tam 3 hane değilse (ör. 2 hane) ayraç ondalık kabul edilir.
        TurkishDecimal.Parse("1.25").Should().Be(1.25m);
        TurkishDecimal.Parse("1,25").Should().Be(1.25m);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrWhitespace_ReturnsNull(string? input)
    {
        TurkishDecimal.Parse(input).Should().BeNull();
    }

    [Fact]
    public void Parse_NonNumericGarbage_ReturnsNullInsteadOfThrowing()
    {
        TurkishDecimal.Parse("abc").Should().BeNull();
    }

    [Fact]
    public void Parse_NegativeValue_ParsesCorrectly()
    {
        TurkishDecimal.Parse("-45,90").Should().Be(-45.90m);
    }

    [Fact]
    public void ParseInt_WhitespaceOrNull_ReturnsNull()
    {
        TurkishDecimal.ParseInt(null).Should().BeNull();
        TurkishDecimal.ParseInt("  ").Should().BeNull();
    }

    [Fact]
    public void ParseInt_PlainInteger_ParsesDirectly()
    {
        TurkishDecimal.ParseInt("42").Should().Be(42);
    }

    [Fact]
    public void ParseInt_CommaFollowedByThreeDigits_TreatsCommaAsThousandsSeparator()
    {
        // int.TryParse(..., NumberStyles.Any, InvariantCulture) virgülü binlik ayraç
        // sayar ve grup uzunluğunu doğrulamaz: "3,0" -> 30 olarak ayrıştırılır,
        // TurkishDecimal.Parse fallback'ine hiç düşmez. Doğrulanmış gerçek davranış.
        TurkishDecimal.ParseInt("3,0").Should().Be(30);
    }

    [Fact]
    public void ParseInt_MultipleDifferentSeparators_FallsBackToDecimalParse()
    {
        // int.TryParse(..., NumberStyles.Any) tek bir virgülü binlik ayraç sayacak
        // kadar hoşgörülü, ama nokta VE virgülü birlikte kabul etmiyor -> başarısız
        // olur, ParseInt bu durumda TurkishDecimal.Parse'a düşer (son ayracı ondalık
        // sayar) ve tam sayıya yuvarlar.
        TurkishDecimal.ParseInt("1.234,56").Should().Be(1234);
    }
}
