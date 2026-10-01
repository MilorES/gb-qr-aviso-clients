using System.Text.Json;
using GbQrAvisosCliente.Linux.Models;

namespace GbQrAvisosCliente.Linux.Services;

public static class AlertHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public static string HistoryPath { get; } = Path.Combine(SettingsStore.SettingsDirectory, "alerts.json");

    public static List<AlertItem> Load(int maximumAlerts)
    {
        if (!File.Exists(HistoryPath)) return [];

        var saved = JsonSerializer.Deserialize<List<SavedAlert>>(File.ReadAllText(HistoryPath)) ?? [];
        return saved.OrderByDescending(alert => alert.ReceivedAt).Take(maximumAlerts)
            .Select(alert => new AlertItem
            {
                ReceivedAt = alert.ReceivedAt,
                DisplayText = alert.DisplayText,
                IsNewest = alert.IsNewest
            }).ToList();
    }

    public static void Save(IEnumerable<AlertItem> alerts, int maximumAlerts)
    {
        Directory.CreateDirectory(SettingsStore.SettingsDirectory);
        var saved = alerts.Take(maximumAlerts)
            .Select(alert => new SavedAlert(alert.ReceivedAt, alert.DisplayText, alert.IsNewest)).ToList();
        var temporaryPath = HistoryPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(saved, JsonOptions));
        File.Move(temporaryPath, HistoryPath, overwrite: true);
    }

    private sealed record SavedAlert(DateTime ReceivedAt, string DisplayText, bool IsNewest);
}
