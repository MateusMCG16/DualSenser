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

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }

    public static string GetLogsDirectory()
    {
        string rootDir = GetRootDirectory();
        string logsDir = Path.Combine(rootDir, "Logs");

        if (!Directory.Exists(logsDir))
        {
            Directory.CreateDirectory(logsDir);
        }

        return logsDir;
    }

    public static void ConfigureLogger(AppConfig? config = null)
    {
        string logsDir = GetLogsDirectory();
        string systemLogFilePathFormat = Path.Combine(logsDir, "dualsenser-.log");

        const string systemOutputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .Enrich.FromLogContext()
            // Arquivo de log principal do sistema (dualsenser-yyyyMMdd.log)
            .WriteTo.File(
                path: systemLogFilePathFormat,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                rollOnFileSizeLimit: true,
                outputTemplate: systemOutputTemplate,
                restrictedToMinimumLevel: LogEventLevel.Debug
            )
            // Saída no Console
            .WriteTo.Console(
                restrictedToMinimumLevel: LogEventLevel.Information,
                outputTemplate: systemOutputTemplate
            );

        Log.Logger = loggerConfig.CreateLogger();

        Log.Information("DualSenser Logger inicializado com sucesso.");
        Log.Information("Diretório de logs: {LogsDirectory}", logsDir);
    }
}
