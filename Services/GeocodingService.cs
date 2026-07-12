using System.Text.Json;

namespace RoomGoHanoi.Services;

public interface IGeocodingService
{
    Task<(double Lat, double Lng)?> FindAsync(string address);
}

public class GeocodingService(HttpClient http) : IGeocodingService
{
    public async Task<(double Lat, double Lng)?> FindAsync(string address)
    {
        try
        {
            var url =
                "https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&q="
                + Uri.EscapeDataString(address + ", Hà Nội, Việt Nam");
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("RoomGoHanoi/1.0 (student-project)");
            using var response = await http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var item = json.RootElement.EnumerateArray().FirstOrDefault();
            if (item.ValueKind == JsonValueKind.Undefined)
                return null;
            return (
                double.Parse(
                    item.GetProperty("lat").GetString()!,
                    System.Globalization.CultureInfo.InvariantCulture
                ),
                double.Parse(
                    item.GetProperty("lon").GetString()!,
                    System.Globalization.CultureInfo.InvariantCulture
                )
            );
        }
        catch
        {
            return null;
        }
    }
}
