using System;
using System.IO;
using Serilog;
using Serilog.Events;

namespace DualSenser.Service.Common;

public static class LoggerConfig
{
    public static string GetRootDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            // Se encontramos a pasta que contém o subdiretório 'server', este é o diretório raiz (pasta acima de server)
            if (Directory.Exists(Path.Combine(dir.FullName, "server")) || File.Exists(Path.Combine(dir.FullName, "start-service.bat")))
            {
                return dir.FullName;
            }

            if (dir.Name.Equals("server", StringComparison.OrdinalIgnoreCase) && dir.Parent != null)
            {
                return dir.Parent.FullName;
            }

            dir = dir.Parent;
        }

        // Fallback caso não encontre 'server' na hierarquia
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }

    public static string GetLogsDirectory()
    {
        // Garante que a pasta Logs fique na pasta acima de server (raiz do projeto)
        string rootDir = GetRootDirectory();
        string logsDir = Path.Combine(rootDir, "Logs");

        if (!Directory.Exists(logsDir))
        {
            Directory.CreateDirectory(logsDir);
        }

        return logsDir;
    }

    public static void ConfigureLogger()
    {
        string logsDir = GetLogsDirectory();
        string logFilePathFormat = Path.Combine(logsDir, "dualsenser-.log");

        const string outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                restrictedToMinimumLevel: LogEventLevel.Information,
                outputTemplate: outputTemplate
            )
            .WriteTo.File(
                path: logFilePathFormat,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB por arquivo
                rollOnFileSizeLimit: true,
                outputTemplate: outputTemplate,
                restrictedToMinimumLevel: LogEventLevel.Debug
            )
            .CreateLogger();

        Log.Information("DualSenser Logger inicializado com sucesso.");
        Log.Information("Diretório de logs: {LogsDirectory}", logsDir);
    }
}
