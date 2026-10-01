using System.IO;
using System.Text.Json;
using GbQrAvisosCliente.Windows.Models;

namespace GbQrAvisosCliente.Windows.Services;

public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string SettingsDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GrupoBreston", "GbQrAvisosCliente");

    public static string SettingsPath { get; } = Path.Combine(SettingsDirectory, "settings.json");

    public static AppSettings LoadOrCreate()
    {
        if (!File.Exists(SettingsPath))
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new AppSettings(), JsonOptions));
        }

        var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath))
            ?? throw new InvalidDataException("El archivo de configuración está vacío.");

        if (string.IsNullOrWhiteSpace(settings.BrokerHost))
            throw new InvalidDataException("Falta BrokerHost en la configuración.");
        if (settings.BrokerPort is < 1 or > 65535)
            throw new InvalidDataException("BrokerPort debe estar entre 1 y 65535.");
        if (settings.Locals is null || settings.Locals.Count == 0 || settings.Locals.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("Indica al menos un local en Locals.");
        if (settings.MaximumAlerts < 1)
            settings.MaximumAlerts = 500;

        settings.Locals = settings.Locals
            .Select(local => local.Trim().Trim('/'))
            .Where(local => local.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (settings.Locals.Count != 1)
            throw new InvalidDataException("Configura exactamente un local en Locals.");

        return settings;
    }
}
