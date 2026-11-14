using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Wholphin.Models;

public record HomeRowViewOptions
{
    [JsonPropertyName("heightDp")]
    public int HeightDp { get; set; } = 172;

    [JsonPropertyName("spacing")]
    public int Spacing { get; set; } = 16;

    [JsonPropertyName("contentScale")]
    public PrefContentScale ContentScale { get; set; } = PrefContentScale.FILL;

    [JsonPropertyName("aspectRatio")]
    public AspectRatio AspectRatio { get; set; }

    [JsonPropertyName("imageType")]
    public ViewOptionImageType ImageType { get; set; }

    [JsonPropertyName("showTitles")]
    public bool ShowTitles { get; set; }

    [JsonPropertyName("useSeries")]
    public bool UseSeries { get; set; } = true;

    [JsonPropertyName("episodeContentScale")]
    public PrefContentScale EpisodeContentScale { get; set; } = PrefContentScale.FILL;

    [JsonPropertyName("episodeAspectRatio")]
    public AspectRatio EpisodeAspectRatio { get; set; }

    [JsonPropertyName("episodeImageType")]
    public ViewOptionImageType EpisodeImageType { get; set; }
}

public enum PrefContentScale
{
    FIT = 0,
    NONE = 1,
    CROP = 2,
    FILL = 3,
    Fill_WIDTH = 4,
    FILL_HEIGHT = 5,
    UNRECOGNIZED = -1,
}

public enum AspectRatio
{
    TALL = 0,
    WIDE = 1,
    FOUR_THREE = 2,
    SQUARE = 3,
}

public enum ViewOptionImageType
{
    PRIMARY = 0,
    THUMB = 1,
}
