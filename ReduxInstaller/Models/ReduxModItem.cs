using System.Text.Json.Serialization;

namespace ReduxInstaller.Models
{
    public class ReduxModItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("badge")]
        public string? Badge { get; set; }

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; } = string.Empty;

        [JsonPropertyName("video_url")]
        public string? VideoUrl { get; set; }

        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; } = string.Empty;

        [JsonPropertyName("short_description")]
        public string ShortDescription { get; set; } = string.Empty;

        [JsonPropertyName("full_description")]
        public string FullDescription { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public string? Size { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("date")]
        public string? Date { get; set; }

        // Helper UI properties
        [JsonIgnore]
        public bool HasVideo => !string.IsNullOrWhiteSpace(VideoUrl);

        [JsonIgnore]
        public bool HasBadge => !string.IsNullOrWhiteSpace(Badge);

        [JsonIgnore]
        public bool HasAuthor => !string.IsNullOrWhiteSpace(Author);

        [JsonIgnore]
        public bool HasSize => !string.IsNullOrWhiteSpace(Size);
    }
}
