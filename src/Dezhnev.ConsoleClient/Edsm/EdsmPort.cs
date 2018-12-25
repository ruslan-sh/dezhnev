using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Dezhnev.ConsoleClient.Edsm
{
    public class EdsmPort
    {
        private const string GetSphereSystemsUrl = "https://www.edsm.net/api-v1/sphere-systems";

        public async Task<List<string>> GetSphereSystemsAsync(Point center, int radius)
        {
            string resultJson;
            using (var client = new HttpClient())
            {
                resultJson = await client.GetStringAsync(
                    $"{GetSphereSystemsUrl}?x={center.X}&y={center.Y}&z={center.Z}&radius={radius}");
            }
            var systems = JsonConvert.DeserializeObject<List<EdsmStarSystem>>(resultJson);
            return systems.Select(s => s.Name).ToList();
        }
    }
}
