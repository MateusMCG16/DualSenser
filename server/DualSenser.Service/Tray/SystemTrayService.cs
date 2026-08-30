using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using DualSenser.Service.Common;
using DualSenser.Service.Hid;
using DualSenser.Service.Models;
using DualSenser.Service.Network;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DualSenser.Service.Tray;

[SupportedOSPlatform("windows")]
public sealed class SystemTrayService : IHostedService, IDisposable
{
    private const string WindowClassName = "DualSenser_TrayWindowClass";
    private const int TrayIconId = 1001;

    // IDs dos itens do Menu de Contexto
    private const uint CmdStatus = 101;
    private const uint CmdConnection = 102;
    private const uint CmdClients = 103;
    private const uint CmdOpenConfig = 201;
    private const uint CmdOpenLogs = 202;
    private const uint CmdExit = 999;

    private readonly IDualSenseHidReader _hidReader;
    private readonly IDualSenseWebSocketManager _webSocketManager;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<SystemTrayService> _logger;

    private Thread? _trayThread;
    private IntPtr _hwnd = IntPtr.Zero;
    private IntPtr _hIcon = IntPtr.Zero;
    private ShellNotifyIconNative.WndProc? _wndProcDelegate;
    private bool _isDisposed;
    private int? _lastAlertedPercentage;

    public SystemTrayService(
        IDualSenseHidReader hidReader,
        IDualSenseWebSocketManager webSocketManager,
        IHostApplicationLifetime appLifetime,
        ILogger<SystemTrayService> logger)
    {
        _hidReader = hidReader;
        _webSocketManager = webSocketManager;
        _appLifetime = appLifetime;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando serviço de ícone na bandeja do sistema (Windows System Tray)...");

        var readyEvent = new ManualResetEventSlim(false);

        _trayThread = new Thread(() => RunTrayMessageLoop(readyEvent))
        {
            IsBackground = true,
            Name = "DualSenser_SystemTray_Thread"
        };
        _trayThread.SetApartmentState(ApartmentState.STA);
        _trayThread.Start();

        readyEvent.Wait(TimeSpan.FromSeconds(5), cancellationToken);

        _hidReader.DeviceConnected += OnDeviceConnected;
        _hidReader.DeviceDisconnected += OnDeviceDisconnected;
        _hidReader.BatteryStateChanged += OnBatteryStateChanged;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Encerrando serviço de ícone na bandeja...");

        _hidReader.DeviceConnected -= OnDeviceConnected;
        _hidReader.DeviceDisconnected -= OnDeviceDisconnected;
        _hidReader.BatteryStateChanged -= OnBatteryStateChanged;

        if (_hwnd != IntPtr.Zero)
        {
            RemoveTrayIcon();
            ShellNotifyIconNative.PostMessageW(_hwnd, 0x0012 /* WM_QUIT */, IntPtr.Zero, IntPtr.Zero);
            ShellNotifyIconNative.PostMessageW(_hwnd, ShellNotifyIconNative.WM_DESTROY, IntPtr.Zero, IntPtr.Zero);
        }

        return Task.CompletedTask;
    }

    private void RunTrayMessageLoop(ManualResetEventSlim readyEvent)
    {
        try
        {
            IntPtr hInstance = ShellNotifyIconNative.GetModuleHandleW(null);
            _wndProcDelegate = WindowProc;

            _hIcon = LoadAppIcon();

            var wndClass = new ShellNotifyIconNative.WNDCLASSEXW
            {
                cbSize = Marshal.SizeOf<ShellNotifyIconNative.WNDCLASSEXW>(),
                style = 0,
                lpfnWndProc = _wndProcDelegate,
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = hInstance,
                hIcon = _hIcon,
                hCursor = IntPtr.Zero,
                hbrBackground = IntPtr.Zero,
                lpszMenuName = null!,
                lpszClassName = WindowClassName,
                hIconSm = _hIcon
            };

            ShellNotifyIconNative.RegisterClassExW(ref wndClass);

            _hwnd = ShellNotifyIconNative.CreateWindowExW(
                0,
                WindowClassName,
                "DualSenser Tray Window",
                0,
                0, 0, 0, 0,
                IntPtr.Zero,
                IntPtr.Zero,
                hInstance,
                IntPtr.Zero
            );

            if (_hwnd != IntPtr.Zero)
            {
                AddTrayIcon();
                _logger.LogInformation("Ícone oficial do DualSense adicionado à bandeja do sistema com sucesso.");
            }
            else
            {
                _logger.LogWarning("Não foi possível criar a janela Win32 para a bandeja do sistema.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inicializar o ícone na bandeja do sistema.");
        }
        finally
        {
            readyEvent.Set();
        }

        // Loop de mensagens Win32
        while (ShellNotifyIconNative.GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
        {
            ShellNotifyIconNative.TranslateMessage(ref msg);
            ShellNotifyIconNative.DispatchMessage(ref msg);
        }

        RemoveTrayIcon();
    }

    private IntPtr LoadAppIcon()
    {
        try
        {
            string baseDir = AppContext.BaseDirectory;
            string iconPath = Path.Combine(baseDir, "Resources", "app_icon.ico");

            if (!File.Exists(iconPath))
            {
                string rootDir = LoggerConfig.GetRootDirectory();
                iconPath = Path.Combine(rootDir, "server", "DualSenser.Service", "Resources", "app_icon.ico");
            }

            if (File.Exists(iconPath))
            {
                IntPtr hIcon = ShellNotifyIconNative.LoadImageW(
                    IntPtr.Zero,
                    iconPath,
                    ShellNotifyIconNative.IMAGE_ICON,
                    0,
                    0,
                    ShellNotifyIconNative.LR_LOADFROMFILE | ShellNotifyIconNative.LR_DEFAULTSIZE
                );

                if (hIcon != IntPtr.Zero)
                {
                    _logger.LogDebug("Ícone customizado carregado com sucesso de: {Path}", iconPath);
                    return hIcon;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao carregar app_icon.ico. Usando ícone padrão.");
        }

        return ShellNotifyIconNative.LoadIconW(IntPtr.Zero, (IntPtr)ShellNotifyIconNative.IDI_APPLICATION);
    }

    private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case ShellNotifyIconNative.WM_TRAYICON:
                int eventId = lParam.ToInt32();
                if (eventId == ShellNotifyIconNative.WM_RBUTTONUP || eventId == ShellNotifyIconNative.WM_CONTEXTMENU)
                {
                    ShowContextMenu();
                }
                else if (eventId == ShellNotifyIconNative.WM_LBUTTONUP || eventId == ShellNotifyIconNative.WM_LBUTTONDBLCLK)
                {
                    OpenLogsFolder();
                }
                return IntPtr.Zero;

            case ShellNotifyIconNative.WM_DESTROY:
                ShellNotifyIconNative.PostQuitMessage(0);
                return IntPtr.Zero;

            default:
                return ShellNotifyIconNative.DefWindowProcW(hWnd, msg, wParam, lParam);
        }
    }

    private void AddTrayIcon()
    {
        if (_hwnd == IntPtr.Zero) return;

        var nid = new ShellNotifyIconNative.NOTIFYICONDATAW
        {
            cbSize = Marshal.SizeOf<ShellNotifyIconNative.NOTIFYICONDATAW>(),
            hWnd = _hwnd,
            uID = TrayIconId,
            uFlags = ShellNotifyIconNative.NIF_MESSAGE | ShellNotifyIconNative.NIF_ICON | ShellNotifyIconNative.NIF_TIP,
            uCallbackMessage = ShellNotifyIconNative.WM_TRAYICON,
            hIcon = _hIcon,
            szTip = BuildTooltipText(_hidReader.CurrentState, _hidReader.CurrentDevice)
        };

        ShellNotifyIconNative.Shell_NotifyIconW(ShellNotifyIconNative.NIM_ADD, ref nid);
    }

    private void UpdateTrayIcon(bool showBalloon = false, string balloonTitle = "", string balloonText = "")
    {
        if (_hwnd == IntPtr.Zero) return;

        int flags = ShellNotifyIconNative.NIF_TIP;
        if (showBalloon)
        {
            flags |= ShellNotifyIconNative.NIF_INFO;
        }

        var nid = new ShellNotifyIconNative.NOTIFYICONDATAW
        {
            cbSize = Marshal.SizeOf<ShellNotifyIconNative.NOTIFYICONDATAW>(),
            hWnd = _hwnd,
            uID = TrayIconId,
            uFlags = flags,
            szTip = BuildTooltipText(_hidReader.CurrentState, _hidReader.CurrentDevice),
            szInfoTitle = balloonTitle,
            szInfo = balloonText,
            dwInfoFlags = ShellNotifyIconNative.NIIF_WARNING,
            uTimeoutOrVersion = 5000
        };

        ShellNotifyIconNative.Shell_NotifyIconW(ShellNotifyIconNative.NIM_MODIFY, ref nid);
    }

    private void RemoveTrayIcon()
    {
        if (_hwnd == IntPtr.Zero) return;

        var nid = new ShellNotifyIconNative.NOTIFYICONDATAW
        {
            cbSize = Marshal.SizeOf<ShellNotifyIconNative.NOTIFYICONDATAW>(),
            hWnd = _hwnd,
            uID = TrayIconId
        };

        ShellNotifyIconNative.Shell_NotifyIconW(ShellNotifyIconNative.NIM_DELETE, ref nid);
    }

    private void ShowContextMenu()
    {
        if (_hwnd == IntPtr.Zero) return;

        IntPtr hMenu = ShellNotifyIconNative.CreatePopupMenu();
        if (hMenu == IntPtr.Zero) return;

        try
        {
            var state = _hidReader.CurrentState;
            var device = _hidReader.CurrentDevice;
            int connectedClients = _webSocketManager.ConnectedClientsCount;

            // Linha 1: Status da Bateria
            string statusLine = state.IsConnected
                ? $"🎮 DualSense: {state.Percentage}% ({(state.IsCharging ? "Carregando ⚡" : "Descarregando")})"
                : "🎮 DualSense: Desconectado";
            ShellNotifyIconNative.AppendMenuW(hMenu, ShellNotifyIconNative.MF_STRING | ShellNotifyIconNative.MF_DISABLED, (UIntPtr)CmdStatus, statusLine);

            // Linha 2: Tipo de Conexão
            string connLine = state.IsConnected
                ? $"📶 Conexão: {device?.ConnectionType.ToString() ?? "Desconhecido"}"
                : "📶 Conexão: Nenhuma";
            ShellNotifyIconNative.AppendMenuW(hMenu, ShellNotifyIconNative.MF_STRING | ShellNotifyIconNative.MF_DISABLED, (UIntPtr)CmdConnection, connLine);

            // Linha 3: Clientes Mobile
            string clientsLine = $"📱 Mobile Conectados: {connectedClients}";
            ShellNotifyIconNative.AppendMenuW(hMenu, ShellNotifyIconNative.MF_STRING | ShellNotifyIconNative.MF_DISABLED, (UIntPtr)CmdClients, clientsLine);

            // Separador
            ShellNotifyIconNative.AppendMenuW(hMenu, ShellNotifyIconNative.MF_SEPARATOR, UIntPtr.Zero, string.Empty);

            // Ações
            ShellNotifyIconNative.AppendMenuW(hMenu, ShellNotifyIconNative.MF_STRING, (UIntPtr)CmdOpenConfig, "⚙️ Abrir Configurações (config.ini)");
            ShellNotifyIconNative.AppendMenuW(hMenu, ShellNotifyIconNative.MF_STRING, (UIntPtr)CmdOpenLogs, "📁 Abrir Pasta de Logs");

            // Separador
            ShellNotifyIconNative.AppendMenuW(hMenu, ShellNotifyIconNative.MF_SEPARATOR, UIntPtr.Zero, string.Empty);

            // Sair
            ShellNotifyIconNative.AppendMenuW(hMenu, ShellNotifyIconNative.MF_STRING, (UIntPtr)CmdExit, "❌ Encerrar DualSenser");

            ShellNotifyIconNative.GetCursorPos(out var pt);
            ShellNotifyIconNative.SetForegroundWindow(_hwnd);

            uint selectedCmd = ShellNotifyIconNative.TrackPopupMenuEx(
                hMenu,
                ShellNotifyIconNative.TPM_RIGHTBUTTON | ShellNotifyIconNative.TPM_RETURNCMD,
                pt.x,
                pt.y,
                _hwnd,
                IntPtr.Zero
            );

            HandleMenuCommand(selectedCmd);
        }
        finally
        {
            ShellNotifyIconNative.DestroyMenu(hMenu);
        }
    }

    private void HandleMenuCommand(uint cmd)
    {
        switch (cmd)
        {
            case CmdOpenConfig:
                OpenConfigFile();
                break;
            case CmdOpenLogs:
                OpenLogsFolder();
                break;
            case CmdExit:
                _logger.LogInformation("Encerramento solicitado pelo menu da bandeja.");
                _appLifetime.StopApplication();
                break;
        }
    }

    private void OpenConfigFile()
    {
        try
        {
            string rootDir = LoggerConfig.GetRootDirectory();
            string configPath = Path.Combine(rootDir, "Config", "config.ini");
            if (File.Exists(configPath))
            {
                Process.Start(new ProcessStartInfo("notepad.exe", $"\"{configPath}\"") { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao abrir arquivo config.ini.");
        }
    }

    private void OpenLogsFolder()
    {
        try
        {
            string logsDir = LoggerConfig.GetLogsDirectory();
            if (Directory.Exists(logsDir))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{logsDir}\"") { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao abrir diretório de logs.");
        }
    }

    private static string BuildTooltipText(DualSenseBatteryState state, DualSenseDeviceInfo? device)
    {
        if (!state.IsConnected)
        {
            return "DualSenser\nStatus: Desconectado";
        }

        string chargeStr = state.IsCharging ? "Carregando ⚡" : (state.IsFullyCharged ? "Carga Completa" : "Descarregando");
        string connStr = device?.ConnectionType.ToString() ?? "HID";
        string tip = $"DualSenser\nBateria: {state.Percentage}% ({chargeStr})\nConexão: {connStr}";

        return tip.Length > 127 ? tip[..127] : tip;
    }

    private void OnDeviceConnected(DualSenseDeviceInfo device)
    {
        _lastAlertedPercentage = null;
        UpdateTrayIcon();
    }

    private void OnDeviceDisconnected()
    {
        _lastAlertedPercentage = null;
        UpdateTrayIcon();
    }

    private void OnBatteryStateChanged(DualSenseBatteryState state)
    {
        bool showBalloon = false;
        string title = string.Empty;
        string message = string.Empty;

        if (state.IsConnected && !state.IsCharging)
        {
            if (state.Percentage <= 10 && _lastAlertedPercentage != 10)
            {
                _lastAlertedPercentage = 10;
                showBalloon = true;
                title = "⚠️ Bateria Crítica do DualSense!";
                message = $"Bateria em {state.Percentage}%. Conecte o cabo USB imediatamente.";
            }
            else if (state.Percentage <= 20 && _lastAlertedPercentage != 20 && (_lastAlertedPercentage == null || _lastAlertedPercentage > 20))
            {
                _lastAlertedPercentage = 20;
                showBalloon = true;
                title = "DualSense - Bateria Baixa";
                message = $"Nível de carga em {state.Percentage}%.";
            }
            else if (state.Percentage > 20)
            {
                _lastAlertedPercentage = null;
            }
        }
        else
        {
            _lastAlertedPercentage = null;
        }

        UpdateTrayIcon(showBalloon, title, message);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_hwnd != IntPtr.Zero)
        {
            RemoveTrayIcon();
            ShellNotifyIconNative.DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }

        if (_hIcon != IntPtr.Zero)
        {
            ShellNotifyIconNative.DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
        }
    }
}
