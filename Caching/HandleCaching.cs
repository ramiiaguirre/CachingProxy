using System.Net;
using System.Text;
using System.Text.Json;

namespace Caching;

public class HandleCaching
{

    string? _originURL;

    HttpListenerRequest? _request;
    HttpListenerResponse? _response;
    private readonly Dictionary<string, CachedResponse> _cache = new();

    private readonly string _cacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CachingProxy",
        "cache"
    );
    private string _cacheFilePath;

    HttpClient _httpClient = new HttpClient();
    ServiceCachingProxy _service = new();

    public HandleCaching()
    {
        _cacheFilePath = Path.Combine(_cacheDirectory, "cache.json");
    }
    public async Task HandleCachingRequest(HttpListenerContext context, string originUrl)
    {
        _request = context.Request;
        _response = context.Response;
        _originURL = originUrl;

        try
        {
            string path = _request.Url?.PathAndQuery ?? "/";
            string cacheKey = $"{_request.HttpMethod}:{path}";

            if (await ResponseInCache(_response, cacheKey))
            {
                return;
            }

            await ExecuteAPICall(cacheKey, path);

        }
        catch (Exception ex)
        {
            _response.StatusCode = 500;
            var errorBytes = Encoding.UTF8.GetBytes($"Error del proxy: {ex.Message}");
            await _response.OutputStream.WriteAsync(errorBytes);
            _response.OutputStream.Close();
        }
    }

    /// <summary>
    /// If response is Cached, X-Cache header is changed to HIT
    /// </summary>
    /// <param name="response"></param>
    /// <param name="cacheKey"></param>
    /// <returns></returns>
    async Task<bool> ResponseInCache(HttpListenerResponse response, string cacheKey)
    {
        if (_cache.TryGetValue(cacheKey, out var cachedResponse))
        {
            response.StatusCode = cachedResponse.StatusCode;
            response.ContentType = cachedResponse.ContentType;
            response.Headers.Add("X-Cache", "HIT");

            await response.OutputStream.WriteAsync(cachedResponse.Body);
            response.OutputStream.Close();
            return true;
        }
        return false;
    }

    async Task ExecuteAPICall(string cacheKey, string path)
    {
        // Create full URL
        string targetUrl = $"{_originURL?.TrimEnd('/')}{path}";

        // Create origin server request 
        var originRequest = new HttpRequestMessage(
            new HttpMethod(_request!.HttpMethod),
            targetUrl
        );

        CopyRelevantHeaders(originRequest);

        HttpResponseMessage originResponse = await _service.Send(originRequest);

        byte[] responseBody = await originResponse.Content.ReadAsByteArrayAsync();

        if (originResponse.IsSuccessStatusCode)
            SaveToCache(cacheKey, originResponse, responseBody);

        _response!.StatusCode = (int)originResponse.StatusCode;
        _response.ContentType = originResponse.Content.Headers.ContentType?.ToString() ?? "text/plain";
        _response.Headers.Add("X-Cache", "MISS");

        await _response.OutputStream.WriteAsync(responseBody);
        _response.OutputStream.Close();
    }

    void CopyRelevantHeaders(HttpRequestMessage originRequest)
    {
        foreach (string? headerName in _request!.Headers.AllKeys)
        {
            if (headerName != null &&
                !headerName.Equals("Host", StringComparison.OrdinalIgnoreCase) &&
                !headerName.Equals("Connection", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    originRequest.Headers.TryAddWithoutValidation(
                        headerName,
                        _request.Headers[headerName]
                    );
                }
                catch { }
            }
        }
    }

    void SaveToCache(string cacheKey, HttpResponseMessage originResponse, byte[] responseBody)
    {
        _cache[cacheKey] = new CachedResponse
        {
            StatusCode = (int)originResponse.StatusCode,
            ContentType = originResponse.Content.Headers.ContentType?.ToString() ?? "text/plain",
            Body = responseBody
        };

        SaveCache();
    }

    public CachingProcessInformation LoadCache()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                var json = File.ReadAllText(_cacheFilePath);
                var cacheData = JsonSerializer.Deserialize<Dictionary<string, CachedResponse>>(json);

                if (cacheData != null)
                {
                    foreach (var item in cacheData)
                    {
                        _cache[item.Key] = item.Value;
                    }
                }
            }
            return new CachingProcessInformation()
            {
                Message = $"✓ Caché cargado: {_cache.Count} entradas",
                ExitProcess = true
            };

        }
        catch (Exception ex)
        {
            return new CachingProcessInformation()
            {
                Message = $"⚠ Advertencia: No se pudo cargar el caché: {ex.Message}",
                ExitProcess = false
            };
        }
    }

    public CachingProcessInformation ClearCache()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                File.Delete(_cacheFilePath);
                return new CachingProcessInformation()
                {
                    Message = $"✓ Caché limpiado exitosamente \nArchivo eliminado: {_cacheFilePath}",
                    ExitProcess = true
                };
            }
            else
            {
                return new CachingProcessInformation()
                {
                    Message = $"✓ No hay caché para limpiar",
                    ExitProcess = true
                };
            }
        }
        catch (Exception ex)
        {
            return new CachingProcessInformation()
            {
                Message = $"✗ Error al limpiar el caché: {ex.Message}",
                ExitProcess = false
            };
        }
    }

    public CachingProcessInformation SaveCache()
    {
        try
        {
            // Crear directorio si no existe
            Directory.CreateDirectory(_cacheDirectory);

            var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            File.WriteAllText(_cacheFilePath, json);
            return new CachingProcessInformation()
            {
                Message = $"Información guardada correctamente.",
                ExitProcess = true
            };
        }
        catch (Exception ex)
        {
            return new CachingProcessInformation()
            {
                Message = $"⚠ Advertencia: No se pudo guardar el caché: {ex.Message}",
                ExitProcess = false
            };
        }
    }
}