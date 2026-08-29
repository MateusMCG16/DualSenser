using System;
using System.Threading;
using DualSenser.Service.Common;
using DualSenser.Service.Hid;
using DualSenser.Service.Models.Network;
using DualSenser.Service.Network;
using DualSenser.Service.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DualSenser.Service;

public class Program
{
    public static int Main(string[] args)
    {
        try
        {
            // Carrega as configurações do config.ini antes de inicializar o logger
            var config = ConfigManager.LoadOrCreateConfig();
            LoggerConfig.ConfigureLogger(config);

            Log.Information("Iniciando o host do serviço DualSenser...");

            var builder = WebApplication.CreateBuilder(args);

            // Configurar Kestrel para escutar em todas as interfaces de rede na porta definida
            builder.WebHost.UseKestrel(options =>
            {
                options.ListenAnyIP(config.HttpPort);
            });

            // Suporte para execução nativa como Windows Service ou em console interativo
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "DualSenser";
            });

            // Conectar o Serilog ao Host da aplicação
            builder.Host.UseSerilog();

            // Injetar configurações da aplicação
            builder.Services.AddSingleton(config);

            // Registrar os serviços de leitura HID e WebSocket Manager
            builder.Services.AddSingleton<IDualSenseHidReader, DualSenseHidReader>();
            builder.Services.AddSingleton<IDualSenseWebSocketManager, DualSenseWebSocketManager>();

            // Registrar Workers em segundo plano
            builder.Services.AddHostedService<DualSenseMonitorWorker>();

            if (config.EnableUdpBeacon)
            {
                builder.Services.AddHostedService<UdpBeaconService>();
            }

            var app = builder.Build();

            // Habilitar suporte a WebSockets nativo no pipeline do ASP.NET Core
            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(15)
            };
            app.UseWebSockets(webSocketOptions);

            // Rotas Minimal APIs
            app.MapGet("/", () => Results.Ok(new
            {
                service = "DualSenser",
                version = "1.0",
                endpoints = new[] { "/api/status", "/api/health", "/ws" }
            }));

            app.MapGet("/api/status", (IDualSenseHidReader reader) =>
            {
                var status = ControllerStatusDto.FromState(reader.CurrentState, reader.CurrentDevice);
                return Results.Ok(status);
            });

            app.MapGet("/api/health", (IDualSenseHidReader reader, IDualSenseWebSocketManager wsManager) =>
            {
                var health = new HealthCheckDto(
                    Status: "healthy",
                    Service: "DualSenser",
                    Version: "1.0",
                    ConnectedClients: wsManager.ConnectedClientsCount,
                    ControllerConnected: reader.CurrentState.IsConnected,
                    Timestamp: DateTime.UtcNow
                );
                return Results.Ok(health);
            });

            // Endpoint de streaming WebSocket
            app.Map("/ws", async (HttpContext context, IDualSenseWebSocketManager wsManager, CancellationToken ct) =>
            {
                await wsManager.HandleClientAsync(context, ct);
            });

            Log.Information("DualSenser pronto e escutando em: http://0.0.0.0:{Port} (WS: /ws)", config.HttpPort);
            app.Run();

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
