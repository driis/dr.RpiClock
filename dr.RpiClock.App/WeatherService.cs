using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace dr.RpiClock.App;

public record WeatherData(double Temperature, int WeatherCode, DateTime LastUpdated);

public class WeatherService(HttpClient httpClient, ILogger<WeatherService> logger)
{
    private const string ApiUrl =
        "https://api.open-meteo.com/v1/forecast?latitude=55.6761&longitude=12.5683&current=temperature_2m,weather_code&timezone=Europe/Copenhagen";

    private WeatherData? _cached;
    private DateTime _lastFetch = DateTime.MinValue;
    private static readonly TimeSpan CacheInterval = TimeSpan.FromHours(1);

    public async Task<WeatherData?> GetCurrentWeatherAsync(CancellationToken ct = default)
    {
        if (_cached is not null && DateTime.UtcNow - _lastFetch < CacheInterval)
            return _cached;

        try
        {
            var response = await httpClient.GetFromJsonAsync<OpenMeteoResponse>(ApiUrl, ct);
            if (response?.Current is not null)
            {
                _cached = new WeatherData(
                    response.Current.Temperature2m,
                    (int)response.Current.WeatherCode,
                    DateTime.Now);
                _lastFetch = DateTime.UtcNow;
                logger.LogInformation("Weather updated: {Temp}C, code {Code}", _cached.Temperature, _cached.WeatherCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch weather data, using cached value");
        }

        return _cached;
    }

    public static string GetWeatherDescription(int code) => code switch
    {
        0 => "Clear sky",
        1 or 2 or 3 => "Partly cloudy",
        45 or 48 => "Fog",
        51 or 53 or 55 => "Drizzle",
        61 or 63 or 65 => "Rain",
        66 or 67 => "Freezing rain",
        71 or 73 or 75 => "Snow",
        77 => "Snow grains",
        80 or 81 or 82 => "Rain showers",
        85 or 86 => "Snow showers",
        95 => "Thunderstorm",
        96 or 99 => "Thunderstorm with hail",
        _ => "Unknown"
    };

    private record OpenMeteoResponse
    {
        [JsonPropertyName("current")]
        public CurrentWeather? Current { get; init; }
    }

    private record CurrentWeather
    {
        [JsonPropertyName("temperature_2m")]
        public double Temperature2m { get; init; }

        [JsonPropertyName("weather_code")]
        public double WeatherCode { get; init; }
    }
}
