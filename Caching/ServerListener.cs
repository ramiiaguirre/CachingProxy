using System.Net;
using System.Reflection.Metadata;

namespace Caching;

public class ServerListener
{
    HandleCaching _handleCaching;

    public ServerListener(HandleCaching handleCaching)
    {
        _handleCaching = handleCaching;
    }
    public async Task StartProxyServer(int port, string? originUrl)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");

        try
        {
            listener.Start();
            Console.WriteLine($"✓ Servidor proxy iniciado en http://localhost:{port}");
            Console.WriteLine($"✓ Reenviando solicitudes a: {originUrl}");
            Console.WriteLine($"✓ Presiona Ctrl+C para detener el servidor");
            Console.WriteLine();

            while (true)
            {
                var context = await listener.GetContextAsync();
                _ = Task.Run(() => _handleCaching.HandleCachingRequest(context, originUrl!));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al iniciar el servidor: {ex.Message}");
        }
        finally
        {
            listener.Stop();
        }
    }
}
