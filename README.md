<div align="center">

# 🎮 DualSenser

### *Monitoramento Inteligente de Bateria e Telemetria do PS5 DualSense no Windows & Android*

<br />

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0%20%7C%208.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET" />
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/Android-Kotlin%20%2B%20Compose-3DDC84?style=for-the-badge&logo=android&logoColor=white" alt="Android Kotlin" />
  <img src="https://img.shields.io/badge/ASP.NET%20Core-Kestrel%20WebSockets-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="ASP.NET Core Kestrel" />
  <img src="https://img.shields.io/badge/Windows%20API-Win32%20HID-0078D6?style=for-the-badge&logo=windows&logoColor=white" alt="Windows API" />
  <img src="https://img.shields.io/badge/PlayStation%205-DualSense%20%2F%20Edge-003791?style=for-the-badge&logo=playstation&logoColor=white" alt="PS5 DualSense" />
  <img src="https://img.shields.io/badge/Network-Directed%20UDP%20Beacon-FF6F00?style=for-the-badge&logo=fastapi&logoColor=white" alt="UDP Beacon" />
  <img src="https://img.shields.io/badge/Logging-Serilog%20(Dedicated%20Sinks)-4A90E2?style=for-the-badge&logo=buffer&logoColor=white" alt="Serilog" />
  <img src="https://img.shields.io/badge/Tests-xUnit%20(34%20Passed)-28A745?style=for-the-badge&logo=xunit&logoColor=white" alt="xUnit Tests" />
</p>

<br />

<p align="center">
  <a href="#-sobre-o-projeto">Sobre</a> •
  <a href="#-tecnologias-utilizadas">Tecnologias</a> •
  <a href="#-funcionalidades">Funcionalidades</a> •
  <a href="#-aplicativo-android-kotlin--jetpack-compose">App Android</a> •
  <a href="#-sistema-de-logs-e-modos-de-opera%C3%A7%C3%A3o">Sistema de Logs</a> •
  <a href="#-endpoints-e-rede">Endpoints & Rede</a> •
  <a href="#-estrutura-do-reposit%C3%B3rio">Estrutura</a> •
  <a href="#-configura%C3%A7%C3%A3o">Configuração</a> •
  <a href="#-como-executar-e-compilar">Como Executar & Compilar</a> •
  <a href="#-roadmap-de-desenvolvimento">Roadmap</a>
</p>

---

</div>

## 📌 Sobre o Projeto

O **DualSenser** é um ecossistema completo composto por um **serviço em segundo plano no Windows (C# / .NET)** e um **aplicativo nativo Android (Kotlin / Jetpack Compose)** projetado para monitorar a bateria e telemetria do controle **Sony PlayStation 5 DualSense / DualSense Edge** em tempo real via rede local (Wi-Fi), emitindo alertas visuais e vibrações táteis antes que o controle desligue por falta de carga.

---

## 🛠️ Tecnologias Utilizadas

<table align="center" width="100%">
  <tr>
    <td align="center" width="25%">
      <img src="https://skillicons.dev/icons?i=dotnet" width="48" height="48" alt=".NET"/><br/>
      <b>.NET 10 / .NET 8</b><br/>
      <sub>Worker Service & Kestrel WebSockets</sub>
    </td>
    <td align="center" width="25%">
      <img src="https://skillicons.dev/icons?i=cs" width="48" height="48" alt="C#"/><br/>
      <b>C# & Win32</b><br/>
      <sub>Leitura HID Não-Bloqueante (IOCP)</sub>
    </td>
    <td align="center" width="25%">
      <img src="https://skillicons.dev/icons?i=kotlin" width="48" height="48" alt="Kotlin"/><br/>
      <b>Kotlin 2.0</b><br/>
      <sub>App Nativo Android (Gradle KTS)</sub>
    </td>
    <td align="center" width="25%">
      <img src="https://img.icons8.com/color/48/android-os.png" width="48" height="48" alt="Compose"/><br/>
      <b>Jetpack Compose</b><br/>
      <sub>UI Minimalista com Safe Area</sub>
    </td>
  </tr>
  <tr>
    <td align="center" width="25%">
      <img src="https://img.icons8.com/color/48/bluetooth.png" width="48" height="48" alt="Bluetooth"/><br/>
      <b>Bluetooth HID 0x31</b><br/>
      <sub>Handshake Feature Report 0x05</sub>
    </td>
    <td align="center" width="25%">
      <img src="https://img.icons8.com/fluency/48/network-cable.png" width="48" height="48" alt="WebSockets"/><br/>
      <b>WebSockets & UDP</b><br/>
      <sub>Streaming em Tempo Real & Directed Beacon</sub>
    </td>
    <td align="center" width="25%">
      <img src="https://img.icons8.com/fluency/48/vibration.png" width="48" height="48" alt="Haptics"/><br/>
      <b>Haptic Alerts</b><br/>
      <sub>Vibração nos limiares 20%, 10% e 5%</sub>
    </td>
    <td align="center" width="25%">
      <img src="https://img.icons8.com/fluency/48/console.png" width="48" height="48" alt="Serilog"/><br/>
      <b>Serilog</b><br/>
      <sub>Sinks dedicados para telemetria de inputs</sub>
    </td>
  </tr>
</table>

---

## ✨ Funcionalidades

* 🔋 **Decodificação Precisa de Bateria:**
  * Extração em tempo real da porcentagem de carga ($0\%$ a $100\%$ em passos de $10\%$) do *Byte 54* do Report `0x31` (Bluetooth) e *Byte 53* do Report `0x01` (USB).
  * Detecção de estados de energia: `Discharging`, `Charging`, `Full` e anomalias de temperatura/voltagem.

* 📱 **Aplicativo Android Minimalista:**
  * Logo oficial estilizada do DualSense.
  * Título centralizado **Status** e estado dinâmico (`Descarregando`, `Carregando ⚡`, `Carga Completa`, `Desconectado`).
  * Barra de bateria horizontal estilo pílula arredondada com porcentagem ao lado.
  * Cores dinâmicas: 🟢 Verde ($>40\%$), 🟡 Amarelo ($20\%-40\%$), 🔴 Vermelho ($\le 20\%$).
  * Suporte rigoroso a **Safe Area / Insets** (`enableEdgeToEdge()` + `Modifier.safeDrawingPadding()`).

* 🌐 **Streaming WebSockets & Auto-Descoberta Resiliente:**
  * Endpoint `WS /ws` com envio imediato de estado no handshake e broadcast reativo.
  * **Directed UDP Beacon:** O servidor envia broadcasts para o IP de broadcast específico de cada placa de rede (ex: `192.168.15.255`), superando restrições de roteadores.
  * **WifiManager.MulticastLock:** O app adquire o lock de rádio Wi-Fi para recepção ininterrupta de pacotes UDP no Android.
  * **Diálogo de IP Manual & Auto-Save:** Toque em "Status" ou no rodapé para inserir o IP do PC manualmente caso o roteador utilize isolamento de clientes (com persistência automática).

* 📳 **Segundo Plano & Alertas Táteis:**
  * `Foreground Service` que mantém o app ativo mesmo com a tela do smartphone desligada.
  * Notificação contínua na barra do sistema com a porcentagem atual.
  * Padrões distintos de vibração em níveis críticos ($\le 20\%$, $\le 10\%$, $\le 5\%$).

* 🤝 **Coexistência com Steam e Jogos:**
  * Handles Win32 compartilhados (`FILE_SHARE_READ | FILE_SHARE_WRITE`) e I/O assíncrono com portas de conclusão do Windows (IOCP - 0% de impacto na CPU).

---

## 📱 Aplicativo Android (Kotlin & Jetpack Compose)

```
 ┌─────────────────────────────────────────────────────────────┐
 │ [Safe Area Top / Status Bar]                                │
 │                                                             │
 │                            🎮                               │
 │                          Status                             │
 │                       Descarregando                         │
 │                                                             │
 │                                                             │
 │          ┌───────────────────────────────────┐              │
 │          │███████████████████████████████████│ 100%         │
 │          └───────────────────────────────────┘              │
 │               (Barra Pílula Animada Verde/Amarelo/Vermelho) │
 │                                                             │
 │                                                             │
 │ [Safe Area Bottom / Navigation Bar]                         │
 └─────────────────────────────────────────────────────────────┘
```

---

## 📝 Sistema de Logs e Modos de Operação

O DualSenser conta com separação inteligente de arquivos de log através do **Serilog**:

```
Logs/
├── dualsenser-20260829.log          <-- Log principal do sistema (Rede, Kestrel, Bateria)
└── dualsenser-activity-20260829.log <-- Log dedicado de inputs (Criado se ShowControllerActivity=true)
```

| Configuração (`config.ini`) | Saída no Terminal / `.bat` | Gravação em Arquivo |
| :--- | :--- | :--- |
| **`ShowControllerActivity=false`** | Exibe logs normais de sistema, conexões de rede, status da bateria e clientes WebSocket. | Grava apenas em `Logs/dualsenser-yyyyMMdd.log`. |
| **`ShowControllerActivity=true`** | Exibe no terminal em tempo real cada ação realizada no controle (`[INPUT] ...`). | Grava os inputs exclusivamente em `Logs/dualsenser-activity-yyyyMMdd.log` e mantém o log do sistema limpo. |

---

## 🌐 Endpoints e Rede

| Protocolo | Rota / Porta | Descrição |
| :--- | :--- | :--- |
| **HTTP REST** | `GET /api/status` | Retorna o status atual do controle em JSON. |
| **HTTP REST** | `GET /api/health` | Health check do serviço e contagem de clientes conectados. |
| **WebSocket** | `WS /ws` | Canal bidirecional de streaming em tempo real do status da bateria e conexão. |
| **UDP Beacon**| `UDP :54321` | Broadcast periódico de auto-descoberta na rede local (`255.255.255.255` e `subnet.255`). |

---

## 📁 Estrutura do Repositório

```
DualSenser/
│
├── Config/                             # Gerado na inicialização
│   └── config.ini                      # Arquivo de configuração da aplicação
│
├── Logs/                               # Gerado na inicialização
│   ├── dualsenser-20260829.log         # Log diário principal do sistema
│   └── dualsenser-activity-20260829.log# Log diário dedicado de inputs (se ativo)
│
├── server/                             # Servidor Windows (.NET 10 / C#)
│   ├── DualSenser.slnx                 # Solução .NET
│   ├── DualSenser.Service/             # Kestrel Web API, WebSockets e Leitura HID
│   │   ├── Common/ (ConfigManager, LoggerConfig)
│   │   ├── Hid/ (DualSenseHidReader, DualSenseReportParser, Native)
│   │   ├── Models/ (BatteryState, DeviceInfo, InputState, Network DTOs)
│   │   ├── Network/ (DualSenseWebSocketManager, UdpBeaconService)
│   │   └── Services/ (DualSenseMonitorWorker)
│   └── DualSenser.Tests/               # 34 Testes Unitários Automatizados (xUnit)
│
├── android/                            # Aplicativo Nativo Android (Kotlin / Compose)
│   ├── build.gradle.kts
│   ├── settings.gradle.kts
│   ├── gradlew.bat
│   └── app/
│       ├── build.gradle.kts
│       └── src/main/
│           ├── AndroidManifest.xml
│           ├── res/ (layouts, temas, cores, drawables, mipmap com a nova logo)
│           └── java/com/dualsenser/
│               ├── DualSenserApp.kt
│               ├── MainActivity.kt
│               ├── data/ (Modelos, WebSocketClient, UdpDiscoveryListener)
│               ├── domain/ (ControllerUiState)
│               ├── service/ (ForegroundService, NotificationHelper, VibrationAlertManager)
│               └── ui/ (MainScreen, MainViewModel, BatteryPillProgressBar, Theme)
│
├── start-service.bat                   # Script para iniciar o serviço no Windows
├── build-app.bat                       # Script para compilar o APK do Android
├── PLAN.md                             # Especificação técnica do ecossistema
├── .gitignore
└── README.md
```

---

## ⚙️ Configuração (`Config/config.ini`)

```ini
; ========================================================
; DualSenser - Arquivo de Configuracao
; ========================================================

[Settings]
; Se true, exibe no log e no terminal toda atividade do controle (botoes, analogicos, gatilhos e trackpad)
; e grava no arquivo dedicado Logs/dualsenser-activity-*.log
ShowControllerActivity=false

[Network]
; Porta do servidor HTTP e WebSockets para comunicacao com o app Android
HttpPort=5005
; Habilita a emissao de beacons UDP na rede local para auto-descoberta
EnableUdpBeacon=true
; Porta de broadcast UDP
UdpBeaconPort=54321
; Intervalo em segundos entre cada transmissao de beacon de descoberta
UdpBeaconIntervalSeconds=3
```

---

## 🚀 Como Executar e Compilar

### 1. Iniciar o Servidor Windows
Basta dar um duplo clique no arquivo [`start-service.bat`](file:///c:/Users/Mateus/Documents/Code/DualSenser/start-service.bat) ou executar no terminal:

```cmd
.\start-service.bat
```

Para rodar a suíte de testes unitários:
```bash
dotnet test server/DualSenser.slnx
```

### 2. Compilar o Aplicativo Android (Gerar APK)
Basta dar um duplo clique no arquivo [`build-app.bat`](file:///c:/Users/Mateus/Documents/Code/DualSenser/build-app.bat) ou executar no terminal:

```cmd
.\build-app.bat
```

> **Localização do APK:** [`android\app\build\outputs\apk\debug\app-debug.apk`](file:///c:/Users/Mateus/Documents/Code/DualSenser/android/app/build/outputs/apk/debug/app-debug.apk)

---

## 🗺️ Roadmap de Desenvolvimento

- [x] **Fase 1: Core do Serviço Windows & HID (Concluído)**
- [x] **Fase 2: Camada de Rede, WebSockets & UDP Beacon (Concluído)**
- [x] **Fase 3: Aplicativo Mobile Android em Kotlin + Jetpack Compose (Concluído)**
- [x] **Fase 4: Sistema de Notificações em Segundo Plano & Vibração (Concluído)**
- [x] **Fase 5: Automação de Build (.bat) e Geração de APK (Concluído)**
- [x] **Fase 6: Identidade Visual e Nova Logo Oficial Integrada (Concluído)**

---

<div align="center">
  <sub>Desenvolvido com foco em desempenho, baixo consumo de recursos e precisão de hardware.</sub>
</div>
