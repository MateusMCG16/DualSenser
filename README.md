<div align="center">

# 🎮 DualSenser

### *Monitoramento Inteligente de Bateria e Telemetria do PS5 DualSense no Windows*

<br />

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0%20%7C%208.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET" />
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/ASP.NET%20Core-Kestrel%20WebSockets-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="ASP.NET Core Kestrel" />
  <img src="https://img.shields.io/badge/Windows%20API-Win32%20HID-0078D6?style=for-the-badge&logo=windows&logoColor=white" alt="Windows API" />
  <img src="https://img.shields.io/badge/PlayStation%205-DualSense%20%2F%20Edge-003791?style=for-the-badge&logo=playstation&logoColor=white" alt="PS5 DualSense" />
  <img src="https://img.shields.io/badge/Bluetooth-HID%20Report%200x31-0082FC?style=for-the-badge&logo=bluetooth&logoColor=white" alt="Bluetooth HID" />
  <img src="https://img.shields.io/badge/Network-UDP%20Beacon-FF6F00?style=for-the-badge&logo=fastapi&logoColor=white" alt="UDP Beacon" />
  <img src="https://img.shields.io/badge/Logging-Serilog%20(Dedicated%20Sinks)-4A90E2?style=for-the-badge&logo=buffer&logoColor=white" alt="Serilog" />
  <img src="https://img.shields.io/badge/Tests-xUnit%20(34%20Passed)-28A745?style=for-the-badge&logo=xunit&logoColor=white" alt="xUnit Tests" />
</p>

<br />

<p align="center">
  <a href="#-sobre-o-projeto">Sobre</a> •
  <a href="#-tecnologias-utilizadas">Tecnologias</a> •
  <a href="#-funcionalidades">Funcionalidades</a> •
  <a href="#-sistema-de-logs-e-modos-de-opera%C3%A7%C3%A3o">Sistema de Logs</a> •
  <a href="#-endpoints-e-comunica%C3%A7%C3%A3o-de-rede">Endpoints & Rede</a> •
  <a href="#-arquitetura-do-sistema">Arquitetura</a> •
  <a href="#-estrutura-do-reposit%C3%B3rio">Estrutura</a> •
  <a href="#-configura%C3%A7%C3%A3o-configconfigini">Configuração</a> •
  <a href="#-como-executar">Como Executar</a> •
  <a href="#-roadmap-de-desenvolvimento">Roadmap</a>
</p>

---

</div>

## 📌 Sobre o Projeto

O **DualSenser** é um ecossistema projetado para resolver uma das principais frustrações dos jogadores de PC que utilizam o controle **Sony PlayStation 5 DualSense**: a ausência de indicadores nativos de nível de bateria no Windows e o desligamento repentino do controle durante partidas.

O serviço Windows comunica-se em baixo nível com o controle via protocolos **HID Bluetooth e USB**, decodifica o nível exato da bateria e o estado de carregamento em tempo real, emite beacons UDP de auto-descoberta na rede local (Wi-Fi) e transmite os dados via **WebSockets e REST APIs** para o aplicativo móvel.

---

## 🛠️ Tecnologias Utilizadas

<table align="center" width="100%">
  <tr>
    <td align="center" width="33%">
      <img src="https://skillicons.dev/icons?i=dotnet" width="48" height="48" alt=".NET"/><br/>
      <b>.NET 10 / .NET 8</b><br/>
      <sub>Worker Service & Kestrel Minimal APIs</sub>
    </td>
    <td align="center" width="33%">
      <img src="https://skillicons.dev/icons?i=cs" width="48" height="48" alt="C#"/><br/>
      <b>C#</b><br/>
      <sub>Win32 P/Invoke & Leitura HID Não-Bloqueante</sub>
    </td>
    <td align="center" width="33%">
      <img src="https://skillicons.dev/icons?i=windows" width="48" height="48" alt="Windows"/><br/>
      <b>Windows SetupAPI</b><br/>
      <sub>Enumeração HID e Overlapped I/O (IOCP)</sub>
    </td>
  </tr>
  <tr>
    <td align="center" width="33%">
      <img src="https://img.icons8.com/color/48/bluetooth.png" width="48" height="48" alt="Bluetooth"/><br/>
      <b>Bluetooth HID 0x31</b><br/>
      <sub>Handshake Feature 0x05 e Telemetria</sub>
    </td>
    <td align="center" width="33%">
      <img src="https://img.icons8.com/fluency/48/network-cable.png" width="48" height="48" alt="WebSockets"/><br/>
      <b>WebSockets & UDP</b><br/>
      <sub>Streaming em Tempo Real & Auto-Descoberta</sub>
    </td>
    <td align="center" width="33%">
      <img src="https://img.icons8.com/fluency/48/console.png" width="48" height="48" alt="Serilog"/><br/>
      <b>Serilog</b><br/>
      <sub>Logs estruturados e sinks dedicados de atividade</sub>
    </td>
  </tr>
</table>

---

## ✨ Funcionalidades

* 🔋 **Decodificação Precisa de Bateria:**
  * Extração em tempo real da porcentagem de carga ($0\%$ a $100\%$ em passos de $10\%$) a partir do *Byte 54* do Input Report `0x31` (Bluetooth) e *Byte 53* do Report `0x01` (USB).
  * Detecção de estados de energia: `Discharging` (em uso na bateria), `Charging` (carregando via cabo), `Full` (carga completa) e anomalias de voltagem/temperatura.
  * Alertas automáticos de bateria crítica ($\le 15\%$) e bateria baixa ($\le 25\%$).

* 🌐 **Servidor Web Kestrel & Streaming WebSocket:**
  * Endpoint de streaming `WS /ws` com push imediato de estado e broadcast em tempo real para múltiplos smartphones conectados simultaneamente.
  * Minimal APIs REST: `GET /api/status` e `GET /api/health`.

* 📡 **Auto-Descoberta via UDP Beacon:**
  * Emissão periódica de pacotes de broadcast UDP na porta `54321` para permitir que o app Android localize o computador automaticamente na rede local sem digitação manual de IP.

* 📶 **Handshake Bluetooth & Suporte DualSense Padrão / Edge:**
  * Envio automático do **Feature Report `0x05` (Calibration Report)** para forçar o firmware do DualSense a comutar para o modo estendido (`0x31` de 78 bytes).
  * Suporte nativo para **DualSense Padrão (`VID 0x054C`, `PID 0x0CE6`)** e **DualSense Edge (`PID 0x0DF2`)**.

* 🤝 **Coexistência com Steam e Jogos:**
  * Handles Win32 abertos com flags de compartilhamento `FILE_SHARE_READ | FILE_SHARE_WRITE`, permitindo que a Steam, jogos e o DualSenser acessem o controle simultaneamente sem conflitos (`ERROR_SHARING_VIOLATION`).

* ⚡ **I/O Assíncrono com Consumo Zero de CPU:**
  * Uso de `FILE_FLAG_OVERLAPPED` integrado às portas de conclusão de E/S do Windows (*I/O Completion Ports - IOCP*) via `FileStream.ReadAsync`.

* 📝 **Rastreamento de Inputs em Tempo Real (`ShowControllerActivity`):**
  * Telemetria completa opcional: monitora botões analógicos e digitais, gatilhos L2/R2, D-Pad e coordenadas de multitoque e arrasto do Trackpad capacitivo.

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

## 🌐 Endpoints e Comunicação de Rede

| Protocolo | Rota / Porta | Descrição |
| :--- | :--- | :--- |
| **HTTP REST** | `GET /api/status` | Retorna o status atual do controle em JSON. |
| **HTTP REST** | `GET /api/health` | Health check do serviço e contagem de clientes conectados. |
| **WebSocket** | `WS /ws` | Canal bidirecional de streaming em tempo real do status da bateria e conexão. |
| **UDP Beacon**| `UDP :54321` | Broadcast periódico de auto-descoberta na rede local (`255.255.255.255`). |

### Exemplo de Payload JSON (`/api/status` e `/ws`):
```json
{
  "connected": true,
  "modelName": "DualSense Wireless Controller",
  "connectionType": "Bluetooth",
  "batteryPercentage": 80,
  "chargingStatus": "Discharging",
  "isCharging": false,
  "isFullyCharged": false,
  "isCritical": false,
  "isLow": false,
  "timestamp": "2026-08-29T22:20:00.123Z"
}
```

### Exemplo de Pacote UDP Beacon (`UDP :54321`):
```json
{
  "service": "DualSenser",
  "version": "1.0",
  "port": 5005,
  "serverName": "DESKTOP-MATEUS",
  "timestamp": "2026-08-29T22:20:00.123Z"
}
```

---

## 🏗️ Arquitetura do Sistema

```
                                  ┌───────────────────────────┐
                                  │   App Android (Mobile)    │
                                  └─────────────┬─────────────┘
                                                │
                 ┌──────────────────────────────┼──────────────────────────────┐
                 │ 1. UDP Discovery             │ 2. WebSocket Streaming       │ 3. REST Fallback
                 │    (Porta 54321)             │    (WS /ws)                  │    (GET /api/status)
                 ▼                              ▼                              ▼
 ┌─────────────────────────────────────────────────────────────────────────────────────────────┐
 │ DualSenser.Service (ASP.NET Core Kestrel + Worker Service)                                  │
 ├─────────────────────────────────────────────────────────────────────────────────────────────┤
 │ [UdpBeaconService] ──────────> Broadcast periódico na LAN (255.255.255.255:54321)          │
 │                                                                                             │
 │ [WebSocketManager] ──────────> Gerencia clientes conectados e envia updates em tempo real   │
 │                                                                                             │
 │ [Kestrel Minimal APIs] ──────> Endpoints HTTP (/api/status, /api/health, /ws)               │
 │                                                                                             │
 │ [DualSenseHidReader] ────────> Atualiza o estado da bateria e dispara o broadcast           │
 └─────────────────────────────────────────────────────────────────────────────────────────────┘
```

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
├── server/
│   ├── DualSenser.slnx                 # Solução .NET
│   │
│   ├── DualSenser.Service/             # Projeto do Serviço Windows (Kestrel Web SDK)
│   │   ├── Common/
│   │   │   ├── ConfigManager.cs        # Carregador do config.ini (Settings e Network)
│   │   │   └── LoggerConfig.cs         # Setup do Serilog (Sinks dedicados)
│   │   ├── Models/
│   │   │   ├── BatteryChargingStatus.cs
│   │   │   ├── ConnectionType.cs
│   │   │   ├── DualSenseBatteryState.cs
│   │   │   ├── DualSenseDeviceInfo.cs
│   │   │   ├── DualSenseInputState.cs
│   │   │   └── Network/                # DTOs de rede (Status, UdpBeacon, Health)
│   │   │       ├── ControllerStatusDto.cs
│   │   │       ├── UdpBeaconPayloadDto.cs
│   │   │       └── HealthCheckDto.cs
│   │   ├── Hid/
│   │   │   ├── Native/                 # Assinaturas Win32 (SetupAPI, hid.dll, kernel32)
│   │   │   ├── DualSenseHidReader.cs   # Scanner, handshake e leitor assíncrono
│   │   │   └── DualSenseReportParser.cs# Decodificador de bateria e telemetria
│   │   ├── Network/
│   │   │   ├── IDualSenseWebSocketManager.cs
│   │   │   ├── DualSenseWebSocketManager.cs # Gerenciador de WebSockets e broadcast
│   │   │   └── UdpBeaconService.cs     # Emissor de broadcast UDP Beacon
│   │   ├── Services/
│   │   │   └── DualSenseMonitorWorker.cs# Worker integrado com WebSockets
│   │   └── Program.cs                  # Host Kestrel, Rotas HTTP e WebSockets
│   │
│   └── DualSenser.Tests/               # Suíte de Testes Automatizados (xUnit)
│       ├── DualSenseReportParserTests.cs
│       ├── DualSenseInputParsingTests.cs
│       ├── ConfigManagerTests.cs
│       ├── LoggerConfigTests.cs
│       └── NetworkTests.cs
│
├── start-service.bat                   # Script para iniciar o serviço
├── PLAN.md                             # Especificação técnica do ecossistema
├── .gitignore
└── README.md
```

---

## ⚙️ Configuração (`Config/config.ini`)

Na primeira inicialização do serviço, o arquivo `Config/config.ini` é criado automaticamente com a configuração padrão:

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

## 🚀 Como Executar

### Pré-requisitos
* **Sistema Operacional:** Windows 10 ou Windows 11 (64-bit).
* **SDK:** [.NET 8.0 SDK](https://dotnet.microsoft.com/download) ou superior instalado.
* **Controle:** Sony DualSense pareado via Bluetooth ou conectado via cabo USB.

### 1. Inicialização Rápida (Script Batch)
Basta dar um duplo clique no arquivo [`start-service.bat`](file:///c:/Users/Mateus/Documents/Code/DualSenser/start-service.bat) ou executá-lo no terminal:

```cmd
.\start-service.bat
```

### 2. Inicialização via .NET CLI
```bash
dotnet run --project server/DualSenser.Service/DualSenser.Service.csproj
```

### 3. Executando a Suíte de Testes Automatizados
```bash
dotnet test server/DualSenser.slnx
```

> **Resultado:** 34 testes unitários cobrindo parsing de bateria, botões, analógicos, trackpad multitoque, contratos de rede (JSON/DTOs) e separação de logs.

---

## 🗺️ Roadmap de Desenvolvimento

- [x] **Fase 1: Core do Serviço Windows & HID (Concluído)**
  - [x] Enumeração e leitura assíncrona de dispositivos DualSense (Bluetooth e USB).
  - [x] Handshake do Feature Report `0x05`.
  - [x] Parser de bateria e status de carregamento.
  - [x] Sistema de logging estruturado (Serilog) com pastas na raiz.
  - [x] Arquivo de configuração `Config/config.ini` e modo `ShowControllerActivity`.
- [x] **Fase 2: Camada de Rede & WebSockets (Concluído)**
  - [x] ASP.NET Core Kestrel Minimal APIs (`GET /api/status`, `GET /api/health`, `GET /`).
  - [x] Servidor WebSocket (`WS /ws`) com streaming reativo em tempo real.
  - [x] Serviço de broadcast UDP Beacon para auto-descoberta na rede local.
  - [x] Seção `[Network]` integrada ao `config.ini`.
  - [x] 34 testes unitários automatizados com 100% de aprovação.
- [ ] **Fase 3: Aplicativo Mobile Android**
  - [ ] App nativo em Kotlin + Jetpack Compose + Material 3.
  - [ ] Auto-reconnect via OkHttp WebSocket e escuta de UDP Beacon.
  - [ ] Interface visual dinâmica com nível de bateria e animações de carregamento.
- [ ] **Fase 4: Sistema de Notificações & Vibração**
  - [ ] Foreground Service com notificação persistente da porcentagem atual.
  - [ ] Alertas sonoros e vibrações táteis para níveis críticos ($\le 20\%$, $\le 10\%$, $\le 5\%$).

---

<div align="center">
  <sub>Desenvolvido com foco em desempenho, baixo consumo de recursos e precisão de hardware.</sub>
</div>
