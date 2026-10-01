using System.Text.Json;
using GbQrAvisosCliente.Linux.Models;

namespace GbQrAvisosCliente.Linux.Services;

public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string SettingsDirectory { get; } = Path.Combine(GetConfigHome(), "gb-qr-avisos");

    public static string SettingsPath { get; } = Path.Combine(SettingsDirectory, "settings.json");

    private static string GetConfigHome()
    {
        var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrWhiteSpace(xdgConfigHome) && Path.IsPathRooted(xdgConfigHome))
            return xdgConfigHome;

        var home = Environment.GetEnvironmentVariable("HOME");
        if (string.IsNullOrWhiteSpace(home))
            home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(home))
            throw new InvalidOperationException("No se pudo determinar la carpeta personal del usuario Linux.");

        return Path.Combine(home, ".config");
    }

    public static AppSettings LoadOrCreate()
    {
        Directory.CreateDirectory(SettingsDirectory);
        if (!File.Exists(SettingsPath))
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new AppSettings(), JsonOptions));

        var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath))
            ?? throw new InvalidDataException("El archivo de configuración está vacío.");

        if (string.IsNullOrWhiteSpace(settings.BrokerHost))
            throw new InvalidDataException("Falta BrokerHost en la configuración.");
        if (settings.BrokerPort is < 1 or > 65535)
            throw new InvalidDataException("BrokerPort debe estar entre 1 y 65535.");
        if (settings.Locals is null || settings.Locals.Count == 0 || settings.Locals.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("Indica al menos un local en Locals.");

        settings.MaximumAlerts = settings.MaximumAlerts < 1 ? 500 : settings.MaximumAlerts;
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
