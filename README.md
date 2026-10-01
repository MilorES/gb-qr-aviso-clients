# GB QR Avisos · clientes

Aplicaciones cliente para recibir los avisos MQTT del servidor `gb-qr-aviso-server`. El cliente de Windows está en `windows/` y el cliente de Linux en `linux/`.

Los topics MQTT no llevan barra inicial: `<Local>/<Nombre>/<Identificador>`. Los clientes se conectan con MQTT 5; el cliente de Windows se suscribe a `<Local>/+/+` y deja fuera el topic retained `<Local>/codigo`.

Ambos clientes se configuran para un único local y muestran el idioma MQTT recibido, por ejemplo `Mesa 01 - Pedir [CA]`. Guardan localmente un historial limitado configurable en `MaximumAlerts` (500 por defecto). La ventana ocupa toda la pantalla; al tocar o hacer clic en el aviso más reciente, lo marca como atendido quitando la negrita y minimiza la ventana. Al llegar otro aviso, vuelve a primer plano y ocupa toda la pantalla.

Consulta [windows/README.md](windows/README.md) para compilar y ejecutar el cliente Windows, y [linux/README.md](linux/README.md) para Linux.
