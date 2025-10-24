public class CachedResponse
{
    public int StatusCode { get; set; }
    public string ContentType { get; set; } = "text/plain";
    public byte[] Body { get; set; } = Array.Empty<byte>();
}