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
var defaults = new HomeRowViewOptions();
Check(defaults.HeightDp == 172 && defaults.Spacing == 16 && defaults.ContentScale == PrefContentScale.FILL && defaults.UseSeries, "New rows do not match the client defaults.");
var original = new HomePageSettings
{
    Version = 1,
    Rows =
    [
        new RecentlyAdded { ParentId = libraryId, ViewOptions = new() { HeightDp = 0, Spacing = 0, ShowTitles = true } },
        new Genres { ParentId = libraryId, ViewOptions = new() { HeightDp = 0 } },
        new TvPrograms { ViewOptions = new() { HeightDp = 0 } },
        new NextUp { ViewOptions = new() { HeightDp = 240 } },
        new KidsMovies { Title = "Family", Genres = "Family", ViewOptions = new() { HeightDp = 172 } },
    ],
};

var response = controller.CreateHomeSettingsResponse(original, new PathString("/jellyfin"));
Check(response.Rows.Length == 5 && response.Version == 1, "The response lost rows or version.");
Check(response.Rows[0] is RecentlyAdded recentlyAdded && recentlyAdded.ParentId == libraryId, "The library row changed.");
Check(response.Rows[0].ViewOptions.HeightDp == 172 && response.Rows[0].ViewOptions.Spacing == 0 && response.Rows[0].ViewOptions.ShowTitles, "Legacy row options were not repaired correctly.");
Check(response.Rows[1].ViewOptions.HeightDp == 128 && response.Rows[2].ViewOptions.HeightDp == 96, "Row-specific heights were not restored.");
Check(response.Rows[3].ViewOptions.HeightDp == 240, "A configured height was overwritten.");
Check(response.Rows[4] is CustomEndpoint endpoint && endpoint.Endpoint == "/jellyfin/Items" && endpoint.Query!.Any(x => x.Key == "genres" && x.Value == "Family"), "Kids row was not transformed.");
Check(original.Rows[0].ViewOptions.HeightDp == 0 && original.Rows[4] is KidsMovies, "Generating a response mutated the saved configuration.");
Check(controller.CreateHomeSettingsResponse(original, PathString.Empty).Rows[4] is CustomEndpoint, "Repeated requests changed the saved kids row.");

var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
{
    Converters = { new JsonStringEnumConverter() },
});
using var document = JsonDocument.Parse(json);
var rows = document.RootElement.GetProperty("rows");
Check(rows[0].GetProperty("type").GetString() == "RecentlyAdded" && rows[0].GetProperty("parentId").GetGuid() == libraryId, "Library row JSON is incompatible.");
Check(rows[0].GetProperty("viewOptions").GetProperty("heightDp").GetInt32() == 172, "The serialized row height is incorrect.");
Check(rows[0].GetProperty("viewOptions").GetProperty("contentScale").GetString() == "FILL", "The client enum value is incorrect.");
Check(rows[4].GetProperty("type").GetString() == "CustomEndpoint", "The kids row discriminator is incorrect.");

var xml = new XmlSerializer(typeof(HomePageSettings));
using var writer = new StringWriter();
xml.Serialize(writer, original);
using var reader = new StringReader(writer.ToString());
var roundTrip = (HomePageSettings)xml.Deserialize(reader)!;
Check(roundTrip.Rows[0] is RecentlyAdded && roundTrip.Rows[4] is KidsMovies, "Configuration XML did not preserve row types.");
Console.WriteLine("Home settings response regression tests passed.");
