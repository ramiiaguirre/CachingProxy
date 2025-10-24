// See https://aka.ms/new-console-template for more information
using Caching;

class Program
{

    static string? _originUrl;
    
    static async Task Main(string[] args)
    {
        var arguments = ParseArguments(args);

        if (!arguments.ContainsKey("--port") || !arguments.ContainsKey("--origin"))
        {
            ShowUsage();
            return;
        }

        if (!int.TryParse(arguments["--port"], out int port))
        {
            Console.WriteLine("Error: El puerto debe ser un número válido");
            return;
        }

        _originUrl = arguments["--origin"];

        // Validar la URL de origen
        if (!Uri.TryCreate(_originUrl, UriKind.Absolute, out _))
        {
            Console.WriteLine("Error: La URL de origen no es válida");
            return;
        }

        ServerListener _server = new ServerListener();
        await _server.StartProxyServer(port, arguments["--origin"]);

    }
    
    static Dictionary<string, string> ParseArguments(string[] args)
    {
        var arguments = new Dictionary<string, string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--") && i + 1 < args.Length)
            {
                arguments[args[i]] = args[i + 1];
                i++; // Saltar el valor
            }
        }

        return arguments;
    }

    static void ShowUsage()
    {
        Console.WriteLine("Uso: caching-proxy --port <number> --origin <url>");
        Console.WriteLine();
        Console.WriteLine("Opciones:");
        Console.WriteLine("  --port    Puerto en el que correrá el servidor proxy");
        Console.WriteLine("  --origin  URL del servidor al que se reenviarán las solicitudes");
        Console.WriteLine();
        Console.WriteLine("Ejemplo:");
        Console.WriteLine("  caching-proxy --port 3000 --origin http://dummyjson.com");
    }


}