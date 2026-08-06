using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GameOrg.Infrastructure.Common;

/// <summary>Generic jsonb &lt;-&gt; POCO conversion for the ad-hoc JSON columns in the schema.</summary>
internal static class JsonConversions
{
    public static ValueConverter<T, string> For<T>() where T : class => new(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<T>(v, (JsonSerializerOptions?)null)!);
}
