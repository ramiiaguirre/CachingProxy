using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Caching;

public class ServiceCachingProxy
{
    HttpClient? _client;

    public ServiceCachingProxy()
    {
        _client = new HttpClient();
    }

    public async Task<HttpResponseMessage> Send(HttpRequestMessage originRequest) => await _client!.SendAsync(originRequest);

}