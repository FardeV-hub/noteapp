using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace NoteApp;

public class SmartDevice
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Room { get; set; } = "ห้องนั่งเล่น";
    public string Type { get; set; } = "light";
    public string Platform { get; set; } = "http";
    public string ApiUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public bool IsOn { get; set; }
    public int Brightness { get; set; } = 100;
    public int Temperature { get; set; } = 25;
    public string Color { get; set; } = "#ffffff";
    public string Icon => Type switch
    {
        "light" => IsOn ? "💡" : "🔅",
        "ac" => IsOn ? "❄️" : "🌡️",
        "fan" => IsOn ? "🌀" : "💨",
        "tv" => IsOn ? "📺" : "🖥️",
        "plug" => IsOn ? "🔌" : "⭕",
        "lock" => IsOn ? "🔓" : "🔒",
        "camera" => "📷",
        "speaker" => IsOn ? "🔊" : "🔇",
        "curtain" => IsOn ? "🪟" : "🪟",
        _ => "📱"
    };
    public string StatusText => IsOn ? "เปิด" : "ปิด";
    public string StatusColor => IsOn ? "#27ae60" : "#e74c3c";
}

public class SmartHomeData
{
    public List<SmartDevice> Devices { get; set; } = new();
    public string HomeAssistantUrl { get; set; } = "";
    public string HomeAssistantToken { get; set; } = "";
    public string TuyaClientId { get; set; } = "";
    public string TuyaSecret { get; set; } = "";
    public string MqttBroker { get; set; } = "";
    public string MqttUser { get; set; } = "";
    public string MqttPass { get; set; } = "";
}

public static class SmartHomeStore
{
    private static readonly string FilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "smarthome.json");
    private static readonly JsonSerializerOptions Opts = new()
    { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static SmartHomeData Load()
    {
        if (!File.Exists(FilePath)) return new SmartHomeData();
        return JsonSerializer.Deserialize<SmartHomeData>(File.ReadAllText(FilePath), Opts) ?? new();
    }
    public static void Save(SmartHomeData data) =>
        File.WriteAllText(FilePath, JsonSerializer.Serialize(data, Opts));
}

public static class SmartHomeAPI
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };

    // Generic HTTP toggle
    public static async Task<bool> SendHTTP(string url, string method, string? body, string? apiKey)
    {
        try
        {
            var req = new HttpRequestMessage(new HttpMethod(method), url);
            if (!string.IsNullOrEmpty(body))
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");
            if (!string.IsNullOrEmpty(apiKey))
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            var resp = await Http.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // Home Assistant
    public static async Task<bool> HomeAssistantToggle(string baseUrl, string token, string entityId, bool turnOn)
    {
        var service = turnOn ? "turn_on" : "turn_off";
        var domain = entityId.Split('.')[0];
        var url = $"{baseUrl.TrimEnd('/')}/api/services/{domain}/{service}";
        var body = JsonSerializer.Serialize(new { entity_id = entityId });
        return await SendHTTP(url, "POST", body, token);
    }

    public static async Task<bool> HomeAssistantSetTemp(string baseUrl, string token, string entityId, int temp)
    {
        var url = $"{baseUrl.TrimEnd('/')}/api/services/climate/set_temperature";
        var body = JsonSerializer.Serialize(new { entity_id = entityId, temperature = temp });
        return await SendHTTP(url, "POST", body, token);
    }

    // Tuya
    public static async Task<bool> TuyaToggle(string deviceId, string clientId, string secret, bool turnOn)
    {
        var url = $"https://openapi.tuyaus.com/v1.0/devices/{deviceId}/commands";
        var body = JsonSerializer.Serialize(new { commands = new[] { new { code = "switch_1", value = turnOn } } });
        return await SendHTTP(url, "POST", body, secret);
    }

    // Generic device command
    public static async Task<bool> ToggleDevice(SmartDevice device, SmartHomeData config)
    {
        var newState = !device.IsOn;
        bool ok;

        switch (device.Platform)
        {
            case "homeassistant":
                ok = await HomeAssistantToggle(config.HomeAssistantUrl, config.HomeAssistantToken, device.DeviceId, newState);
                break;
            case "tuya":
                ok = await TuyaToggle(device.DeviceId, config.TuyaClientId, config.TuyaSecret, newState);
                break;
            case "http":
                var url = device.ApiUrl.Replace("{state}", newState ? "on" : "off")
                    .Replace("{value}", newState ? "1" : "0");
                ok = await SendHTTP(url, "POST", null, device.ApiKey);
                break;
            default:
                ok = true;
                break;
        }

        if (ok) device.IsOn = newState;
        return ok;
    }
}
