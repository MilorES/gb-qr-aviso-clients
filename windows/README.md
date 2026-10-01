# Cliente Windows

WPF sobre .NET 8. Abre `GbQrAvisosCliente.Windows.csproj` en Visual Studio o ejecuta:

```powershell
dotnet run --project GbQrAvisosCliente.Windows.csproj
```

Al primer inicio se genera `%APPDATA%\GrupoBreston\GbQrAvisosCliente\settings.json`. Configura allí el host, puerto, credenciales y un único local exacto. El cliente muestra el local conectado en la barra superior y rechaza una configuración con varios locales. Como plantilla, consulta `settings.json.example`; nunca guardes la configuración real en el repositorio. El cliente se conecta con MQTT 5 y se suscribe a `<Local>/+/+`, no al topic retenido de códigos `<Local>/codigo`.

Los avisos aparecen en orden de recepción descendente. La fila superior va en negrita y muestra junto al aviso la hora local del ordenador con el formato `HH:mm:ss dd:MM:yyyy`. El idioma recibido como propiedad MQTT 5 (`language`) aparece al final del mensaje entre corchetes, por ejemplo `Mesa 01 - Pedir [CA]`. El local conectado se muestra en la barra superior. La ventana ocupa toda la pantalla y el botón de maximizar está deshabilitado. Al llegar un aviso, pasa a primer plano y queda encima; al hacer clic o tocar el aviso más reciente, este deja de estar en negrita y la ventana se minimiza. Cuando llega otro aviso, vuelve a ocupar toda la pantalla. Hacer clic, tocar o pulsar una tecla en otra zona desactiva `Topmost` hasta el siguiente aviso. Windows puede impedir que tome el foco de escritura si otra aplicación lo tiene.

La plantilla usa un host reservado de ejemplo que no conecta a ningún broker real, y `Breston_Figueres` como local de muestra. Sustituye el host y las credenciales en el archivo de `%APPDATA%` antes de iniciar el cliente.

Para publicar un ejecutable autocontenido x64:

```powershell
dotnet publish GbQrAvisosCliente.Windows.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
