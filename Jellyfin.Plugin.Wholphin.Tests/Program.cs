using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Jellyfin.Plugin.Wholphin.Api;
using Jellyfin.Plugin.Wholphin.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

static void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

var controller = new WholphinController(NullLoggerFactory.Instance, null!);
var libraryId = Guid.NewGuid();
var custom = new HomeRowViewOptions { HeightDp = 200, Spacing = 12, ShowTitles = true };
var kidsCustom = new HomeRowViewOptions { HeightDp = 156, Spacing = 19, AspectRatio = AspectRatio.WIDE };
var original = new HomePageSettings
{
    Rows =
    [
        new RecentlyAdded { ParentId = libraryId },
        new Genres { ParentId = libraryId },
        new TvPrograms(),
        new NextUp { ViewOptions = custom },
        new ContinueWatching { ViewOptions = new() { HeightDp = 4, ShowTitles = true } },
        new KidsMovies { Title = "Family", Genres = "Family", ViewOptions = kidsCustom },
        new KidsMovies { Title = "Animation", Genres = "Animation" },
    ],
};

var response = controller.CreateHomeSettingsResponse(original, new PathString("/jellyfin"));
Check(response.Rows.Length == original.Rows.Length && response.Version == original.Version, "The response lost rows or version.");
Check(response.Rows[0] is RecentlyAdded recentlyAdded && recentlyAdded.ParentId == libraryId, "The library row changed.");
Check(response.Rows[0].ViewOptions is null && response.Rows[1].ViewOptions is null && response.Rows[2].ViewOptions is null, "Rows without overrides did not retain client defaults.");
Check(response.Rows[3].ViewOptions == custom && response.Rows[3].ViewOptions != kidsCustom, "Custom options were overwritten by the kids row.");
Check(response.Rows[4].ViewOptions is null && original.Rows[4].ViewOptions!.HeightDp == 4, "Invalid legacy height was not omitted without changing saved settings.");
Check(response.Rows[5] is CustomEndpoint endpoint && endpoint.Endpoint == "/jellyfin/Items" && endpoint.Query!.Any(x => x.Key == "genres" && x.Value == "Family") && endpoint.ViewOptions == kidsCustom, "Custom kids options or endpoint changed.");
Check(response.Rows[6] is CustomEndpoint && response.Rows[6].ViewOptions is null, "Kids row without overrides did not retain client defaults.");
Check(original.Rows[5] is KidsMovies && controller.CreateHomeSettingsResponse(original, PathString.Empty).Rows[5] is CustomEndpoint, "Response generation mutated saved kids rows.");

var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
using var document = JsonDocument.Parse(JsonSerializer.Serialize(response, options));
var rows = document.RootElement.GetProperty("rows");
Check(rows[0].GetProperty("type").GetString() == "RecentlyAdded" && rows[0].GetProperty("parentId").GetGuid() == libraryId, "Library row JSON is incompatible.");
Check(!rows[0].TryGetProperty("viewOptions", out _) && !rows[1].TryGetProperty("viewOptions", out _) && !rows[2].TryGetProperty("viewOptions", out _) && !rows[4].TryGetProperty("viewOptions", out _), "Client defaults require viewOptions to be omitted, not null.");
Check(rows[3].GetProperty("viewOptions").GetProperty("heightDp").GetInt32() == 200, "Custom display options were not serialized.");
Check(rows[5].GetProperty("type").GetString() == "CustomEndpoint" && rows[5].GetProperty("viewOptions").GetProperty("spacing").GetInt32() == 19, "Kids row JSON is incompatible.");
Check(!rows[6].TryGetProperty("viewOptions", out _), "Default kids options were not omitted.");

var xml = new XmlSerializer(typeof(HomePageSettings));
using var writer = new StringWriter();
xml.Serialize(writer, original);
using var reader = new StringReader(writer.ToString());
var roundTrip = (HomePageSettings)xml.Deserialize(reader)!;
Check(roundTrip.Rows[0].ViewOptions is null && roundTrip.Rows[3].ViewOptions == custom && roundTrip.Rows[5] is KidsMovies && roundTrip.Rows[5].ViewOptions == kidsCustom, "Configuration XML did not preserve optional and custom options.");
Console.WriteLine("Home settings response regression tests passed.");
