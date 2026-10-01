# GB QR Avisos · cliente Linux

Aplicación de escritorio .NET 8 con Avalonia. Recibe avisos MQTT 5 del servidor `gb-qr-aviso-server`.

## Ejecutar

Requisitos: .NET 8 SDK y un escritorio Linux con X11 o XWayland.

```bash
cd linux
dotnet restore
dotnet run
```

Al iniciar por primera vez se crea `~/.config/gb-qr-avisos/settings.json`. Configura allí `BrokerHost`, `BrokerPort`, `Username`, `Password` y un único local. En Linux, el engranaje abre esa carpeta; en WSL muestra la ruta para buscarla manualmente. `MaximumAlerts` establece el límite del historial (500 por defecto) y `alerts.json` guarda los avisos en la misma carpeta.

El cliente usa MQTT 5, se suscribe a `<Local>/+/+` y muestra `Lugar Identificador - Aviso [IDIOMA]`. Los avisos más nuevos aparecen arriba; al llegar uno, la ventana se maximiza y solicita permanecer encima. Al hacer clic o tocar el aviso más reciente, se marca como atendido y la ventana se minimiza. La compatibilidad de `Topmost` y el foco depende del gestor de ventanas de Linux.

Para publicar una versión autocontenida x64 desde Linux:

```bash
dotnet publish GbQrAvisosCliente.Linux.csproj -c Release -r linux-x64 --self-contained true
```
