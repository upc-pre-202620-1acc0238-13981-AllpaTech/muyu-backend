using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Muyu.Modules.ClimateAlerts.Application;

namespace Muyu.Modules.ClimateAlerts.Infrastructure;

public sealed class OpenWeatherAdapter(HttpClient httpClient, IConfiguration configuration) : IWeatherProvider
{
    public async Task<WeatherSnapshot> GetCurrentAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["OpenWeather:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Configure OpenWeather:ApiKey para consultar el clima.");
        }

        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lon = longitude.ToString(CultureInfo.InvariantCulture);
        var endpoint = $"data/2.5/weather?lat={lat}&lon={lon}&appid={Uri.EscapeDataString(apiKey)}&units=metric";
        var response = await httpClient.GetFromJsonAsync<OpenWeatherResponse>(endpoint, cancellationToken)
            ?? throw new InvalidOperationException("OpenWeather devolvió una respuesta vacía.");

        return new WeatherSnapshot(
            decimal.Round(response.Main.Temperature, 2),
            decimal.Round(response.Rain?.LastHour ?? 0, 2),
            decimal.Round(response.Wind.SpeedMetersPerSecond * 3.6m, 2),
            response.Main.Humidity,
            DateTimeOffset.FromUnixTimeSeconds(response.Timestamp));
    }

    private sealed record OpenWeatherResponse(
        [property: JsonPropertyName("main")] MainData Main,
        [property: JsonPropertyName("wind")] WindData Wind,
        [property: JsonPropertyName("rain")] RainData? Rain,
        [property: JsonPropertyName("dt")] long Timestamp);

    private sealed record MainData(
        [property: JsonPropertyName("temp")] decimal Temperature,
        [property: JsonPropertyName("humidity")] int Humidity);

    private sealed record WindData(
        [property: JsonPropertyName("speed")] decimal SpeedMetersPerSecond);

    private sealed record RainData(
        [property: JsonPropertyName("1h")] decimal LastHour);
}
