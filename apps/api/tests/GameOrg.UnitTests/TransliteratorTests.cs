using GameOrg.Api.Common;
using GameOrg.Api.Features.Identity;
using GameOrg.Api.Features.Venues;

namespace GameOrg.UnitTests;

public class TransliteratorTests
{
    [Theory]
    [InlineData("məktəb", "mekteb")]
    [InlineData("İstiqlaliyyət", "istiqlaliyyet")]
    [InlineData("Привет", "privet")]
    [InlineData("plain", "plain")]
    public void Transliterate_replaces_az_and_ru_letters(string input, string expectedLower)
    {
        var result = Transliterator.Transliterate(input).ToLowerInvariant();

        Assert.Equal(expectedLower, result);
    }

    [Fact]
    public void VenueSlugGenerator_keeps_readable_slug_for_azerbaijani_name()
    {
        var slug = VenueSlugGenerator.Generate("132-134 N-li məktəb");

        Assert.StartsWith("132-134-n-li-mekteb-", slug);
    }

    [Fact]
    public void HandleGenerator_transliterates_cyrillic_source()
    {
        var handle = HandleGenerator.Normalize("Рустам");

        Assert.Equal("rustam", handle);
    }
}
