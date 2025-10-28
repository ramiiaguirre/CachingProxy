using System.Net;

using System.Reflection.Metadata;
using Microsoft.Extensions.Logging;

namespace Caching;

public class ServerListener
{
    HandleCaching _handleCaching;

    private readonly ILogger<ServerListener>? _logger;
    public ServerListener(HandleCaching handleCaching, ILogger<ServerListener>? logger = null)
    {
        _handleCaching = handleCaching;
        _logger = logger;
    }
    public async Task StartProxyServer(int port, string? originUrl)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");

        try
        {
            listener.Start();
            
            _logger?.LogInformation("Servidor proxy iniciado en http://localhost:{Port}", port);
            _logger?.LogInformation("Reenviando solicitudes a: {Origin}", originUrl);
            _logger?.LogInformation("Presiona Ctrl+C para detener el servidor");

            while (true)
            {
                var context = await listener.GetContextAsync();
                _ = Task.Run(() => _handleCaching.HandleCachingRequest(context, originUrl!));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error al iniciar el servidor");
        }
        finally
        {
            listener.Stop();
        }
    }
}
