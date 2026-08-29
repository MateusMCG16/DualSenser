using System;
using DualSenser.Service.Common;
using DualSenser.Service.Hid;
using DualSenser.Service.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DualSenser.Service;

public class Program
{
    public static int Main(string[] args)
    {
        LoggerConfig.ConfigureLogger();

        try
        {
            Log.Information("Carregando configurações do DualSenser...");
            var config = ConfigManager.LoadOrCreateConfig();

            Log.Information("Iniciando o host do serviço DualSenser...");

            var builder = Host.CreateApplicationBuilder(args);

            // Suporte para execução nativa como Windows Service ou em modo interativo de console
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "DualSenser";
            });

            // Conectar o Serilog ao Host da aplicação
            builder.Services.AddSerilog();

            // Injetar configurações da aplicação
            builder.Services.AddSingleton(config);

            // Registrar os serviços de leitura HID e o Worker de monitoramento
            builder.Services.AddSingleton<IDualSenseHidReader, DualSenseHidReader>();
            builder.Services.AddHostedService<DualSenseMonitorWorker>();

            var host = builder.Build();
            host.Run();

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "O serviço DualSenser encerrou inesperadamente.");
            return 1;
        }
        finally
        {
            Log.Information("Finalizando o logger do DualSenser.");
            Log.CloseAndFlush();
        }
    }
}
