using System;
using System.IO;
using System.Text;
using Serilog;

namespace DualSenser.Service.Common;

public sealed class AppConfig
{
    public bool ShowControllerActivity { get; set; } = false;
}

public static class ConfigManager
{
    public const string ConfigFileName = "config.ini";

    public static string GetConfigDirectory()
    {
        // Garante que a pasta Config fique na pasta acima de server (raiz do projeto)
        string rootDir = LoggerConfig.GetRootDirectory();
        string configDir = Path.Combine(rootDir, "Config");

        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }

        return configDir;
    }

    public static string GetConfigFilePath()
    {
        return Path.Combine(GetConfigDirectory(), ConfigFileName);
    }

    public static AppConfig LoadOrCreateConfig()
    {
        string filePath = GetConfigFilePath();
        var config = new AppConfig();

        if (!File.Exists(filePath))
        {
            CreateDefaultConfigFile(filePath, config);
            Log.Information("Arquivo de configuração criado em: {ConfigPath} (ShowControllerActivity={Value})", 
                filePath, config.ShowControllerActivity);
            return config;
        }

        try
        {
            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
            bool foundKey = false;

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();

                if (string.IsNullOrEmpty(line) || line.StartsWith(';') || line.StartsWith('#') || line.StartsWith('['))
                {
                    continue;
                }

                int separatorIndex = line.IndexOf('=');
                if (separatorIndex > 0)
                {
                    string key = line.Substring(0, separatorIndex).Trim();
                    string value = line.Substring(separatorIndex + 1).Trim();

                    if (key.Equals("ShowControllerActivity", StringComparison.OrdinalIgnoreCase))
                    {
                        if (bool.TryParse(value, out bool boolValue))
                        {
                            config.ShowControllerActivity = boolValue;
                            foundKey = true;
                        }
                    }
                }
            }

            if (!foundKey)
            {
                CreateDefaultConfigFile(filePath, config);
            }

            Log.Information("Configuração carregada de {ConfigPath} (ShowControllerActivity={Value})", 
                filePath, config.ShowControllerActivity);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Erro ao ler {ConfigPath}. Usando valores padrão.", filePath);
        }

        return config;
    }

    private static void CreateDefaultConfigFile(string filePath, AppConfig config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("; ========================================================");
        sb.AppendLine("; DualSenser - Arquivo de Configuracao");
        sb.AppendLine("; ========================================================");
        sb.AppendLine("[Settings]");
        sb.AppendLine("; Se true, exibe no log toda atividade do controle (botoes, analogicos, gatilhos e trackpad)");
        sb.AppendLine($"ShowControllerActivity={config.ShowControllerActivity.ToString().ToLowerInvariant()}");

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }
}
