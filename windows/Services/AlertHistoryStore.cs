using System.IO;
using System.Text.Json;
using GbQrAvisosCliente.Windows.Models;

namespace GbQrAvisosCliente.Windows.Services;

public static class AlertHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string HistoryPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GrupoBreston", "GbQrAvisosCliente", "alerts.json");

    public static List<AlertItem> Load(int maximumAlerts)
    {
        if (!File.Exists(HistoryPath)) return [];

        var savedAlerts = JsonSerializer.Deserialize<List<SavedAlert>>(File.ReadAllText(HistoryPath)) ?? [];
        return savedAlerts
            .OrderByDescending(alert => alert.ReceivedAt)
            .Take(maximumAlerts)
            .Select(alert => new AlertItem
            {
                ReceivedAt = alert.ReceivedAt,
                DisplayText = alert.DisplayText,
                IsNewest = alert.IsNewest
            })
            .ToList();
    }

    public static void Save(IEnumerable<AlertItem> alerts, int maximumAlerts)
    {
        var directory = Path.GetDirectoryName(HistoryPath)!;
        Directory.CreateDirectory(directory);

        var savedAlerts = alerts
            .Take(maximumAlerts)
            .Select(alert => new SavedAlert(alert.ReceivedAt, alert.DisplayText, alert.IsNewest))
            .ToList();

        var temporaryPath = HistoryPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(savedAlerts, JsonOptions));
        File.Move(temporaryPath, HistoryPath, overwrite: true);
    }

    private sealed record SavedAlert(DateTime ReceivedAt, string DisplayText, bool IsNewest);
}
