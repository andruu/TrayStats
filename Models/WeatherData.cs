using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TrayStats.Models;

public partial class WeatherData : ObservableObject
{
    [ObservableProperty] private string _city = "Locating...";
    [ObservableProperty] private float _temperature;
    [ObservableProperty] private float _feelsLike;
    [ObservableProperty] private int _humidity;
    [ObservableProperty] private float _windSpeed;
    [ObservableProperty] private string _weatherCondition = "--";
    [ObservableProperty] private string _weatherIcon = "\u2601"; // default cloud icon
    [ObservableProperty] private int _precipitationProbability;

    public ObservableCollection<DailyForecast> Forecast { get; } = new();
}

public partial class DailyForecast : ObservableObject
{
    [ObservableProperty] private string _dayName = "";
    [ObservableProperty] private float _high;
    [ObservableProperty] private float _low;
    [ObservableProperty] private string _weatherCondition = "";
    [ObservableProperty] private string _weatherIcon = "";
    [ObservableProperty] private int _precipitationProbability;
}

public static class WmoWeatherCodes
{
    public static (string Condition, string Icon) Decode(int code)
    {
        return code switch
        {
            0 => ("Clear", "\u2600"),              // ☀
            1 => ("Mostly Clear", "\u26C5"),        // ⛅
            2 => ("Partly Cloudy", "\u26C5"),       // ⛅
            3 => ("Overcast", "\u2601"),            // ☁
            45 or 48 => ("Foggy", "\u2601"),        // ☁
            51 or 53 or 55 => ("Drizzle", "\uD83C\uDF27"),       // 🌧
            56 or 57 => ("Freezing Drizzle", "\uD83C\uDF27"),    // 🌧
            61 or 63 or 65 => ("Rain", "\uD83C\uDF27"),          // 🌧
            66 or 67 => ("Freezing Rain", "\uD83C\uDF27"),       // 🌧
            71 or 73 or 75 => ("Snow", "\u2744"),   // ❄
            77 => ("Snow Grains", "\u2744"),         // ❄
            80 or 81 or 82 => ("Showers", "\uD83C\uDF27"),       // 🌧
            85 or 86 => ("Snow Showers", "\u2744"),  // ❄
            95 => ("Thunderstorm", "\u26C8"),        // ⛈
            96 or 99 => ("Thunderstorm w/ Hail", "\u26C8"),      // ⛈
            _ => ("Unknown", "\u2601")              // ☁
        };
    }
}
