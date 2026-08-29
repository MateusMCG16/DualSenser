using System;
using System.IO;
using System.Text;
using Serilog;

namespace DualSenser.Service.Common;

public sealed class AppConfig
{
    // [Settings]
    public bool ShowControllerActivity { get; set; } = false;

    // [Network]
    public int HttpPort { get; set; } = 5005;
    public bool EnableUdpBeacon { get; set; } = true;
    public int UdpBeaconPort { get; set; } = 54321;
    public int UdpBeaconIntervalSeconds { get; set; } = 3;
}

public static class ConfigManager
{
    public const string ConfigFileName = "config.ini";

    public static string GetConfigDirectory()
    {
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
            Log.Information("Arquivo de configuração criado em: {ConfigPath} (Porta HTTP={Port}, UDP={Udp})", 
                filePath, config.HttpPort, config.EnableUdpBeacon);
            return config;
        }

        try
        {
            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
            string currentSection = "";

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();

                if (string.IsNullOrEmpty(line) || line.StartsWith(';') || line.StartsWith('#'))
                {
                    continue;
                }

                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    currentSection = line.Substring(1, line.Length - 2).Trim();
                    continue;
                }

                int separatorIndex = line.IndexOf('=');
                if (separatorIndex > 0)
                {
                    string key = line.Substring(0, separatorIndex).Trim();
                    string value = line.Substring(separatorIndex + 1).Trim();

                    // [Settings]
                    if (key.Equals("ShowControllerActivity", StringComparison.OrdinalIgnoreCase))
                    {
                        if (bool.TryParse(value, out bool boolValue))
                            config.ShowControllerActivity = boolValue;
                    }
                    // [Network]
                    else if (key.Equals("HttpPort", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(value, out int port) && port is > 0 and <= 65535)
                            config.HttpPort = port;
                    }
                    else if (key.Equals("EnableUdpBeacon", StringComparison.OrdinalIgnoreCase))
                    {
                        if (bool.TryParse(value, out bool boolVal))
                            config.EnableUdpBeacon = boolVal;
                    }
                    else if (key.Equals("UdpBeaconPort", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(value, out int port) && port is > 0 and <= 65535)
                            config.UdpBeaconPort = port;
                    }
                    else if (key.Equals("UdpBeaconIntervalSeconds", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(value, out int interval) && interval >= 1)
                            config.UdpBeaconIntervalSeconds = interval;
                    }
                }
            }

            // Garante que o arquivo contenha a estrutura atualizada
            CreateDefaultConfigFile(filePath, config);

            Log.Information("Configuração carregada de {ConfigPath} (HTTP={Port}, UDP={Udp}, ShowActivity={Activity})", 
                filePath, config.HttpPort, config.EnableUdpBeacon, config.ShowControllerActivity);
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
        sb.AppendLine();
        sb.AppendLine("[Settings]");
        sb.AppendLine("; Se true, exibe no log toda atividade do controle (botoes, analogicos, gatilhos e trackpad)");
        sb.AppendLine($"ShowControllerActivity={config.ShowControllerActivity.ToString().ToLowerInvariant()}");
        sb.AppendLine();
        sb.AppendLine("[Network]");
        sb.AppendLine("; Porta do servidor HTTP e WebSockets para comunicacao com o app Android");
        sb.AppendLine($"HttpPort={config.HttpPort}");
        sb.AppendLine("; Habilita a emissao de beacons UDP na rede local para auto-descoberta");
        sb.AppendLine($"EnableUdpBeacon={config.EnableUdpBeacon.ToString().ToLowerInvariant()}");
        sb.AppendLine("; Porta de broadcast UDP");
        sb.AppendLine($"UdpBeaconPort={config.UdpBeaconPort}");
        sb.AppendLine("; Intervalo em segundos entre cada transmissao de beacon de descoberta");
        sb.AppendLine($"UdpBeaconIntervalSeconds={config.UdpBeaconIntervalSeconds}");

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }
}
