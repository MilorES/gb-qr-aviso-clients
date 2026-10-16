using System.Buffers;
using System.Text;
using GbQrAvisosCliente.Windows.Models;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Packets;

namespace GbQrAvisosCliente.Windows.Services;

public sealed class MqttAlertListener : IAsyncDisposable
{
    private readonly AppSettings _settings;
    private readonly Action<AlertItem> _onAlert;
    private readonly Action<string, bool> _onStatus;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly IMqttClient _client;
    private Task? _connectionLoop;

    public MqttAlertListener(AppSettings settings, Action<AlertItem> onAlert, Action<string, bool> onStatus)
    {
        _settings = settings;
        _onAlert = onAlert;
        _onStatus = onStatus;
        _client = new MqttClientFactory().CreateMqttClient();
        _client.ApplicationMessageReceivedAsync += HandleMessageAsync;
    }

    public void Start() => _connectionLoop ??= Task.Run(ConnectionLoopAsync);

    private async Task ConnectionLoopAsync()
    {
        var token = _cancellation.Token;
        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithClientId($"gb-qr-avisos-windows-{Environment.MachineName}-{Guid.NewGuid():N}")
            .WithTcpServer(_settings.BrokerHost, _settings.BrokerPort)
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithCleanSession();

        if (!string.IsNullOrWhiteSpace(_settings.Username))
            optionsBuilder.WithCredentials(_settings.Username, _settings.Password);

        var options = optionsBuilder.Build();

        while (!token.IsCancellationRequested)
        {
            try
            {
                _onStatus("Conectando al servidor…", false);
                await _client.ConnectAsync(options, token);

                foreach (var local in _settings.Locals)
                {
                    var subscribeOptions = new MqttClientFactory()
                        .CreateSubscribeOptionsBuilder()
                        .WithTopicFilter(filter => filter.WithTopic($"{local}/+/+").WithAtLeastOnceQoS())
                        .Build();
                    await _client.SubscribeAsync(subscribeOptions, token);
                }

                _onStatus("Conectado · " + string.Join(", ", _settings.Locals.Select(local => local.Replace('_', ' '))), true);
                while (_client.IsConnected && !token.IsCancellationRequested)
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _onStatus($"Sin conexión: {ex.Message}", false);
            }

            if (!token.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(4), token);
        }
    }

    private Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        var topic = args.ApplicationMessage.Topic ?? "";
        var segments = topic.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 3 || !_settings.Locals.Contains(segments[0], StringComparer.Ordinal))
            return Task.CompletedTask;

        var payload = Encoding.UTF8.GetString(args.ApplicationMessage.Payload.ToArray()).Trim();
        if (payload.Length == 0)
            payload = "Aviso sin texto";

        var place = $"{segments[1].Replace('_', ' ')} {segments[2]}".Trim();
        var languageCode = args.ApplicationMessage.UserProperties?
            .FirstOrDefault(property => string.Equals(property.Name, "language", StringComparison.OrdinalIgnoreCase))?
            .ReadValueAsString();
        var languageSuffix = FormatLanguage(languageCode);
        var item = new AlertItem
        {
            ReceivedAt = DateTime.Now,
            DisplayText = $"{place} - {payload}" + (languageSuffix.Length == 0 ? "" : $" [{languageSuffix}]")
        };
        _onAlert(item);
        return Task.CompletedTask;
    }

    private static string FormatLanguage(string? languageCode) => languageCode?.Trim().ToUpperInvariant() ?? "";

    public async ValueTask DisposeAsync()
    {
        await _cancellation.CancelAsync();
        if (_connectionLoop is not null)
        {
            try { await _connectionLoop; }
            catch (OperationCanceledException) { }
        }

        if (_client.IsConnected)
        {
            try { await _client.DisconnectAsync(); }
            catch { /* The broker may already have closed the connection. */ }
        }

        _client.Dispose();
        _cancellation.Dispose();
    }
}
