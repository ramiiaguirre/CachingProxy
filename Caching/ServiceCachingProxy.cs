using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Caching;

public class ServiceCachingProxy
{
    HttpClient? _client;
    HandleCaching? _handle;

    public ServiceCachingProxy()
    {
        _client = new HttpClient();
        _handle = new HandleCaching();
    }

    public async Task GetResponse(string originUrl)
    {
        var response = await _client!.GetAsync(originUrl);
        Console.WriteLine(response.StatusCode);
        Console.WriteLine(response.Content);
    }
}