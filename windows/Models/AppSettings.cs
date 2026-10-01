namespace GbQrAvisosCliente.Windows.Models;

public sealed class AppSettings
{
    public string BrokerHost { get; set; } = "mqtt.example.invalid";
    public int BrokerPort { get; set; } = 1883;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public List<string> Locals { get; set; } = ["Breston_Figueres"];
    public int MaximumAlerts { get; set; } = 500;
}
