using Newtonsoft.Json;

namespace Dezhnev.ConsoleClient.Edsm
{
    public class EdsmStarSystem
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
