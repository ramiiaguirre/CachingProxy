using System.Net;
using System.Text;

namespace Caching;

public class HandleCaching
{

    private readonly Dictionary<string, CachedResponse> _cache = new();

    // ServiceCachingProxy _service = new();


    HttpClient _httpClient = new HttpClient();

    public async Task HandleCachingRequest(HttpListenerContext context, string originUrl)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            string path = request.Url?.PathAndQuery ?? "/";
            string cacheKey = $"{request.HttpMethod}:{path}";

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {request.HttpMethod} {path}");

            // Verificar si la respuesta está en caché
            if (_cache.TryGetValue(cacheKey, out var cachedResponse))
            {
                Console.WriteLine($"  → Cache HIT");
                response.StatusCode = cachedResponse.StatusCode;
                response.ContentType = cachedResponse.ContentType;
                response.Headers.Add("X-Cache", "HIT");

                await response.OutputStream.WriteAsync(cachedResponse.Body);
                response.OutputStream.Close();
                return;
            }

            Console.WriteLine($"  → Cache MISS");

            // Construir la URL completa
            string targetUrl = $"{originUrl?.TrimEnd('/')}{path}";

            // Crear solicitud al servidor de origen
            var originRequest = new HttpRequestMessage(
                new HttpMethod(request.HttpMethod),
                targetUrl
            );

            // Copiar headers relevantes
            foreach (string? headerName in request.Headers.AllKeys)
            {
                if (headerName != null && 
                    !headerName.Equals("Host", StringComparison.OrdinalIgnoreCase) &&
                    !headerName.Equals("Connection", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        originRequest.Headers.TryAddWithoutValidation(
                            headerName, 
                            request.Headers[headerName]
                        );
                    }
                    catch { }
                }
            }

            // Enviar solicitud al origen
            var originResponse = await _httpClient.SendAsync(originRequest);

            // Leer el contenido de la respuesta
            var responseBody = await originResponse.Content.ReadAsByteArrayAsync();

            // Guardar en caché si la respuesta es exitosa
            if (originResponse.IsSuccessStatusCode)
            {
                _cache[cacheKey] = new CachedResponse
                {
                    StatusCode = (int)originResponse.StatusCode,
                    ContentType = originResponse.Content.Headers.ContentType?.ToString() ?? "text/plain",
                    Body = responseBody
                };
            }

            // Enviar respuesta al cliente
            response.StatusCode = (int)originResponse.StatusCode;
            response.ContentType = originResponse.Content.Headers.ContentType?.ToString() ?? "text/plain";
            response.Headers.Add("X-Cache", "MISS");

            await response.OutputStream.WriteAsync(responseBody);
            response.OutputStream.Close();

            Console.WriteLine($"  → Status: {originResponse.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  → Error: {ex.Message}");
            
            response.StatusCode = 500;
            var errorBytes = Encoding.UTF8.GetBytes($"Error del proxy: {ex.Message}");
            await response.OutputStream.WriteAsync(errorBytes);
            response.OutputStream.Close();
        }
    }

}