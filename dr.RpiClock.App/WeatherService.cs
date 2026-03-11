using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace dr.RpiClock.App;

public record WeatherData(double Temperature, int WeatherCode, DateTime LastUpdated);

public class WeatherService(HttpClient httpClient, ILogger<WeatherService> logger)
{
    private const string ApiUrl =
        "https://api.open-meteo.com/v1/forecast?latitude=56.27&longitude=9.82&current=temperature_2m,weather_code&timezone=Europe/Copenhagen";

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
        0 => "Klar himmel",
        1 or 2 or 3 => "Delvist skyet",
        45 or 48 => "Tåge",
        51 or 53 or 55 => "Støvregn",
        61 or 63 or 65 => "Regn",
        66 or 67 => "Isslag",
        71 or 73 or 75 => "Sne",
        77 => "Snekorn",
        80 or 81 or 82 => "Regnbyger",
        85 or 86 => "Snebyger",
        95 => "Tordenvejr",
        96 or 99 => "Tordenvejr m. hagl",
        _ => "Ukendt"
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
