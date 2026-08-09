using GameOrg.Api.Common;

namespace GameOrg.UnitTests;

public class RequestLocaleTests
{
    [Theory]
    [InlineData(null, "az")]
    [InlineData("", "az")]
    [InlineData("ru", "ru")]
    [InlineData("ru-RU", "ru")]
    [InlineData("RU-ru", "ru")]
    [InlineData("fr-FR", "az")]
    [InlineData("fr,en;q=0.8,ru;q=0.5", "en")]
    [InlineData(" en ", "en")]
    public void Resolve_picks_first_supported_language(string? header, string expected)
    {
        Assert.Equal(expected, RequestLocale.Resolve(header));
    }
}

public class LocalizedTests
{
    [Fact]
    public void Resolve_returns_requested_locale_when_present()
    {
        var values = new Dictionary<string, string> { ["az"] = "Salam", ["ru"] = "Привет", ["en"] = "Hello" };

        Assert.Equal("Привет", Localized.Resolve(values, "ru"));
    }

    [Fact]
    public void Resolve_falls_back_to_az_when_requested_missing()
    {
        var values = new Dictionary<string, string> { ["az"] = "Salam", ["en"] = "Hello" };

        Assert.Equal("Salam", Localized.Resolve(values, "ru"));
    }

    [Fact]
    public void Resolve_falls_back_to_en_before_ru_when_az_missing()
    {
        var values = new Dictionary<string, string> { ["ru"] = "Привет", ["en"] = "Hello" };

        Assert.Equal("Hello", Localized.Resolve(values, "az"));
    }

    [Fact]
    public void Resolve_falls_back_to_ru_when_only_ru_present()
    {
        var values = new Dictionary<string, string> { ["ru"] = "Привет" };

        Assert.Equal("Привет", Localized.Resolve(values, "en"));
    }

    [Fact]
    public void Resolve_treats_empty_string_as_missing()
    {
        var values = new Dictionary<string, string> { ["az"] = "", ["ru"] = "Привет" };

        Assert.Equal("Привет", Localized.Resolve(values, "az"));
    }

    [Fact]
    public void Resolve_falls_back_to_any_non_empty_value_for_unknown_key_order()
    {
        // Гипотетический четвёртый язык — не входит в FallbackOrder, но не должен
        // приводить к null, если это единственное непустое значение.
        var values = new Dictionary<string, string> { ["tr"] = "Merhaba" };

        Assert.Equal("Merhaba", Localized.Resolve(values, "ru"));
    }

    [Fact]
    public void Resolve_returns_null_for_null_dictionary()
    {
        Assert.Null(Localized.Resolve(null, "az"));
    }

    [Fact]
    public void Resolve_returns_null_when_everything_empty()
    {
        var values = new Dictionary<string, string> { ["az"] = "", ["ru"] = "" };

        Assert.Null(Localized.Resolve(values, "en"));
    }
}
