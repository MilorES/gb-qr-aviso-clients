# GB QR Avisos · clientes

Aplicaciones cliente para recibir los avisos MQTT del servidor `gb-qr-aviso-server`. La implementación inicial para Windows se encuentra en `windows/`.

Los topics MQTT no llevan barra inicial: `<Local>/<Nombre>/<Identificador>`. Los clientes se conectan con MQTT 5; el cliente de Windows se suscribe a `<Local>/+/+` y deja fuera el topic retained `<Local>/codigo`.

El cliente Windows se configura para un único local y lo muestra en la barra de conexión. Los avisos incluyen el idioma MQTT recibido, por ejemplo `Mesa 01 - Pedir [CA]`. La ventana ocupa toda la pantalla; al tocar o hacer clic en el aviso más reciente, lo marca como atendido quitando la negrita y minimiza la ventana. Al llegar otro aviso, vuelve a primer plano y ocupa toda la pantalla.

Consulta [windows/README.md](windows/README.md) para compilar, configurar el broker y ejecutar el cliente.
