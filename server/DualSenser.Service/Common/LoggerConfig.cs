using System;
using System.IO;
using Serilog;
using Serilog.Events;
using Serilog.Filters;

namespace DualSenser.Service.Common;

public static class LoggerConfig
{
    public const string ActivitySourceContext = "DualSenseActivity";

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
        string activityLogFilePathFormat = Path.Combine(logsDir, "dualsenser-activity-.log");

        const string systemOutputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";
        const string activityOutputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Message:lj}{NewLine}{Exception}";

        bool showActivity = config?.ShowControllerActivity ?? false;

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .Enrich.FromLogContext()
            // 1. Arquivo de log principal do sistema (dualsenser-yyyyMMdd.log)
            // Não inclui logs de atividade de inputs para manter o log do sistema limpo
            .WriteTo.Logger(lc => lc
                .Filter.ByExcluding(Matching.FromSource(ActivitySourceContext))
                .WriteTo.File(
                    path: systemLogFilePathFormat,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    fileSizeLimitBytes: 10 * 1024 * 1024,
                    rollOnFileSizeLimit: true,
                    outputTemplate: systemOutputTemplate,
                    restrictedToMinimumLevel: LogEventLevel.Debug
                )
            );

        // 2. Arquivo de log dedicado exclusivamente à atividade do controle (dualsenser-activity-yyyyMMdd.log)
        // Somente grava se ShowControllerActivity for true
        if (showActivity)
        {
            loggerConfig.WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(Matching.FromSource(ActivitySourceContext))
                .WriteTo.File(
                    path: activityLogFilePathFormat,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    fileSizeLimitBytes: 20 * 1024 * 1024,
                    rollOnFileSizeLimit: true,
                    outputTemplate: activityOutputTemplate,
                    restrictedToMinimumLevel: LogEventLevel.Information
                )
            );
        }

        // 3. Saída no Console
        // Se ShowControllerActivity=false, o console mostra os logs normais do sistema
        // Se ShowControllerActivity=true, o console mostra tanto os logs do sistema quanto os [INPUT] em tempo real
        loggerConfig.WriteTo.Console(
            restrictedToMinimumLevel: LogEventLevel.Information,
            outputTemplate: systemOutputTemplate
        );

        Log.Logger = loggerConfig.CreateLogger();

        Log.Information("DualSenser Logger inicializado com sucesso.");
        Log.Information("Diretório de logs: {LogsDirectory}", logsDir);
        if (showActivity)
        {
            Log.Information("Log de atividade do controle gravando em: {ActivityLogPath}", 
                Path.Combine(logsDir, "dualsenser-activity-*.log"));
        }
    }
}
