using System.Buffers;
using System.Text;
using GbQrAvisosCliente.Linux.Models;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Packets;

namespace GbQrAvisosCliente.Linux.Services;

public sealed class MqttAlertListener : IAsyncDisposable
{
    private readonly AppSettings _settings;
    private readonly Action<AlertItem> _onAlert;
    private readonly Action<string, bool> _onStatus;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly IMqttClient _client = new MqttClientFactory().CreateMqttClient();
    private Task? _connectionLoop;

    public MqttAlertListener(AppSettings settings, Action<AlertItem> onAlert, Action<string, bool> onStatus)
    {
        _settings = settings;
        _onAlert = onAlert;
        _onStatus = onStatus;
        _client.ApplicationMessageReceivedAsync += HandleMessageAsync;
    }

    public void Start() => _connectionLoop ??= Task.Run(ConnectionLoopAsync);

    private async Task ConnectionLoopAsync()
    {
        var token = _cancellation.Token;
        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithClientId($"gb-qr-avisos-linux-{Environment.MachineName}-{Guid.NewGuid():N}")
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
                    var subscribe = new MqttClientFactory().CreateSubscribeOptionsBuilder()
                        .WithTopicFilter(filter => filter.WithTopic($"{local}/+/+").WithAtLeastOnceQoS())
                        .Build();
                    await _client.SubscribeAsync(subscribe, token);
                }

                _onStatus("Conectado · " + string.Join(", ", _settings.Locals.Select(local => local.Replace('_', ' '))), true);
                while (_client.IsConnected && !token.IsCancellationRequested)
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
            catch (Exception ex) { _onStatus($"Sin conexión: {ex.Message}", false); }

            if (!token.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(4), token);
        }
    }

    private Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        var segments = (args.ApplicationMessage.Topic ?? "").Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 3 || !_settings.Locals.Contains(segments[0], StringComparer.Ordinal))
            return Task.CompletedTask;

        var payload = Encoding.UTF8.GetString(args.ApplicationMessage.Payload.ToArray()).Trim();
        if (payload.Length == 0) payload = "Aviso sin texto";
        var place = $"{segments[1].Replace('_', ' ')} {segments[2]}".Trim();
        var language = args.ApplicationMessage.UserProperties?
            .FirstOrDefault(property => string.Equals(property.Name, "language", StringComparison.OrdinalIgnoreCase))?
            .ReadValueAsString()?.Trim().ToUpperInvariant();
        _onAlert(new AlertItem
        {
            ReceivedAt = DateTime.Now,
            DisplayText = $"{place} - {payload}" + (string.IsNullOrEmpty(language) ? "" : $" [{language}]")
        });
        return Task.CompletedTask;
    }

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
            catch { /* La conexión puede haberse cerrado desde el broker. */ }
        }
        _client.Dispose();
        _cancellation.Dispose();
    }
}
