using CounterStrikeSharp.API.Core;
using CS2_Sprays.Models;
using System.Text.Json.Serialization;

namespace CS2_Sprays.Config
{
    public class PluginConfig : BasePluginConfig
    {
        [JsonPropertyName("ConfigVersion")]
        public override int Version { get; set; } = 1;

        // Empty = everyone can use sprays
        [JsonPropertyName("Permission")]
        public string Permission { get; set; } = "";

        [JsonPropertyName("Sprays")]
        public List<SprayModel> Sprays { get; set; } =
        [
            new SprayModel { Name = "Example", Material = "materials/sprays/example.vmat", Image = "example" }
        ];

        [JsonPropertyName("Panorama Layout")]
        public string PanoramaLayout { get; set; } = "panorama/layout/custom_game/sprays.xml";

        [JsonPropertyName("Spray Size")]
        public float SpraySize { get; set; } = 64;

        [JsonPropertyName("Spray Depth")]
        public int SprayDepth { get; set; } = 12;

        // Max distance between the player's eyes and the wall
        [JsonPropertyName("Spray Distance")]
        public float SprayDistance { get; set; } = 150;

        // Seconds between two sprays of the same player
        [JsonPropertyName("Spray Cooldown")]
        public float SprayCooldown { get; set; } = 30;

        [JsonPropertyName("Spray Sound")]
        public string SpraySound { get; set; } = "SprayCan.Paint";

        // Adds a "Random" card to the menu; players who never chose a spray get a random one
        [JsonPropertyName("Random Spray")]
        public bool RandomSpray { get; set; } = true;

        // Round restarts remove the decals; this puts every player's spray back
        [JsonPropertyName("Keep Sprays Between Rounds")]
        public bool KeepSpraysBetweenRounds { get; set; } = true;

        [JsonPropertyName("Reset Cooldown On Round Start")]
        public bool ResetCooldownOnRoundStart { get; set; } = true;

        [JsonPropertyName("Reset Cooldown On Kill")]
        public bool ResetCooldownOnKill { get; set; } = true;

        // E also defuses, picks up weapons and opens doors, so this is off by default
        [JsonPropertyName("Spray With Use Key")]
        public bool SprayWithUseKey { get; set; } = false;

        [JsonPropertyName("Debug Mode")]
        public bool Debug { get; set; } = false;
    }
}
