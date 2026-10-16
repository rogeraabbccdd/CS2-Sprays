using System.Text.Json.Serialization;

namespace CS2_Sprays.Models
{
    public class SprayModel
    {
        // Shown in the menu and used as the saved selection, so keep it unique
        [JsonPropertyName("Name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("Material")]
        public string Material { get; set; } = "";

        // Menu picture: the addon's sprays_images.css maps ".spray-i-<Image>" to a png
        [JsonPropertyName("Image")]
        public string Image { get; set; } = "";

        [JsonIgnore]
        public string ImageClass => "spray-i-" + new string([.. Image.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) || c == '_' || c == '-' ? c : '_')]);
    }
}
