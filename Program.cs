// See https://aka.ms/new-console-template for more information
using Caching;
using Microsoft.Extensions.Logging;

class Program
{

    static string? _originUrl;
    
    static async Task Main(string[] args)
    {

        HandleCaching _handleCaching = new();
        CachingProcessInformation? info;

        if (args.Length > 0 && args[0] == "--clear-cache")
        {
            info = _handleCaching.ClearCache();
            Console.WriteLine(info.Message);
            return;
        }
        
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

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
        var logger = loggerFactory.CreateLogger<ServerListener>();

        info = _handleCaching.LoadCache();
        Console.WriteLine(info.Message);

        ServerListener _server = new ServerListener(_handleCaching, logger);
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
                i++;
            }
        }

        return arguments;
    }

    static void ShowUsage()
    {
        Console.WriteLine("Uso: caching-proxy [OPCIONES]");
        Console.WriteLine();
        Console.WriteLine("Iniciar servidor:");
        Console.WriteLine("  caching-proxy --port <number> --origin <url>");
        Console.WriteLine();
        Console.WriteLine("Opciones:");
        Console.WriteLine("  --port        Puerto en el que correrá el servidor proxy");
        Console.WriteLine("  --origin      URL del servidor al que se reenviarán las solicitudes");
        Console.WriteLine("  --clear-cache Limpiar todo el caché almacenado");
        Console.WriteLine();
        Console.WriteLine("Ejemplos:");
        Console.WriteLine("  caching-proxy --port 3000 --origin http://dummyjson.com");
        Console.WriteLine("  caching-proxy --clear-cache");
    }


}