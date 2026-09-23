using Haggly.Domain.Modules.Discovery;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Discovery;

public sealed class DiscoveryDomainTests
{
    [Theory]
    [InlineData("  Bún bò Huế  ", "bun bo hue")]
    [InlineData("Đậu-hũ!!!", "dau hu")]
    [InlineData("Cơm   tấm", "com tam")]
    public void Normalize_VietnameseInput_ReturnsStableLookupValue(string value, string expected)
    {
        // Arrange / Act
        var result = VietnameseNameNormalizer.Normalize(value);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CreateCanonicalIngredient_InvalidCode_ThrowsArgumentException()
    {
        // Arrange / Act
        var act = () => CanonicalIngredient.Create(Guid.NewGuid(), "nuoc_mam", "Nước mắm");

        // Assert
        Assert.Throws<ArgumentException>(act);
    }
}
