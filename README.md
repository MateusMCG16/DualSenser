<div align="center">

# 🎮 DualSenser

### *Monitoramento Inteligente de Bateria e Telemetria do PS5 DualSense no Windows*

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0%20%7C%208.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET" />
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/Windows%20API-Win32%20HID-0078D6?style=for-the-badge&logo=windows&logoColor=white" alt="Windows API" />
  <img src="https://img.shields.io/badge/PlayStation%205-DualSense%20%2F%20Edge-003791?style=for-the-badge&logo=playstation&logoColor=white" alt="PS5 DualSense" />
  <img src="https://img.shields.io/badge/Bluetooth-HID%20Report%200x31-0082FC?style=for-the-badge&logo=bluetooth&logoColor=white" alt="Bluetooth HID" />
  <img src="https://img.shields.io/badge/Logging-Serilog-4A90E2?style=for-the-badge&logo=buffer&logoColor=white" alt="Serilog" />
  <img src="https://img.shields.io/badge/Tests-xUnit%20(28%20Passed)-28A745?style=for-the-badge&logo=xunit&logoColor=white" alt="xUnit Tests" />
</p>

<p align="center">
  <a href="#-sobre-o-projeto">Sobre</a> •
  <a href="#-funcionalidades">Funcionalidades</a> •
  <a href="#-arquitetura-do-sistema">Arquitetura</a> •
  <a href="#-estrutura-do-projeto">Estrutura</a> •
  <a href="#-configuração">Configuração</a> •
  <a href="#-como-executar">Como Executar</a> •
  <a href="#-roadmap">Roadmap</a>
</p>

---

</div>

## 📌 Sobre o Projeto

O **DualSenser** é um ecossistema projetado para resolver uma das principais frustrações dos jogadores de PC que utilizam o controle **Sony PlayStation 5 DualSense**: a ausência de indicadores nativos de bateria no Windows e o desligamento repentino do controle no meio de partidas.

O serviço Windows comunica-se em baixo nível com o controle via protocolos **HID Bluetooth e USB**, decodifica o nível exato da bateria e o estado de carregamento em tempo real, gerencia arquivos de configuração e logs rotativos e alimenta uma suíte de alertas resiliente.

---

## ✨ Funcionalidades do MVP (Windows Service Core)

* 🔋 **Decodificação Precisa de Bateria:**
  * Extração em tempo real da porcentagem de carga ($0\%$ a $100\%$ em passos de $10\%$) a partir do *Byte 54* do Input Report `0x31` (Bluetooth) e *Byte 53* do Report `0x01` (USB).
  * Detecção de estados de energia: `Discharging` (em uso na bateria), `Charging` (carregando via cabo), `Full` (carga completa) e anomalias de voltagem/temperatura.
  * Alertas configuráveis de bateria crítica ($\le 15\%$) e bateria baixa ($\le 25\%$).

* 📶 **Handshake Bluetooth & Suporte DualSense Padrão / Edge:**
  * Envio automático do **Feature Report `0x05` (Calibration Report)** para forçar o firmware do DualSense a comutar do modo simples (`0x01` de 10 bytes) para o modo estendido (`0x31` de 78 bytes).
  * Suporte nativo para **DualSense Padrão (`VID 0x054C`, `PID 0x0CE6`)** e **DualSense Edge (`PID 0x0DF2`)**.

* 🤝 **Coexistência com Steam e Jogos:**
  * Handles Win32 abertos com flags de compartilhamento `FILE_SHARE_READ | FILE_SHARE_WRITE`, permitindo que a Steam, jogos e o DualSenser acessem o controle simultaneamente sem bloqueios ou conflitos (`ERROR_SHARING_VIOLATION`).

* ⚡ **I/O Assíncrono com Consumo Zero de CPU:**
  * Uso de `FILE_FLAG_OVERLAPPED` integrado às portas de conclusão de E/S do Windows (*I/O Completion Ports - IOCP*) via `FileStream.ReadAsync`.

* 📝 **Rastreamento de Inputs em Tempo Real (`ShowControllerActivity`):**
  * Telemetria completa opcional: monitora botões analógicos e digitais, gatilhos L2/R2, D-Pad e coordenadas de multitoque e arrasto do Trackpad capacitivo.

* 📂 **Organização Automática de Logs e Configuração:**
  * Pasta `Config/` e `config.ini` gerados automaticamente na raiz do projeto.
  * Pasta `Logs/` com arquivos diários rotativos (`dualsenser-yyyyMMdd.log`) gerenciados pelo **Serilog**.

---

## 🏗️ Arquitetura do Sistema

```
 ┌─────────────────────────────────────────────────────────────┐
 │                     Sony DualSense (PS5)                    │
 └──────────────────────────────┬──────────────────────────────┘
                                │ Bluetooth (HID Report 0x31) / USB (0x01)
                                ▼
 ┌─────────────────────────────────────────────────────────────┐
 │               DualSenser.Service (Windows .NET)             │
 ├─────────────────────────────────────────────────────────────┤
 │                                                             │
 │  [SetupAPI & Win32 P/Invoke]                                │
 │   ├── Enumeração de Interfaces HID (GUID_DEVINTERFACE_HID) │
 │   ├── Handshake Bluetooth (Feature Report 0x05)             │
 │   └── FileStream Assíncrono (Overlapped I/O / IOCP)         │
 │                                                             │
 │  [DualSenseReportParser]                                    │
 │   ├── Decodificação de Bateria (Byte 54 / Nibble 0x0F)      │
 │   ├── Decodificação de Carregamento (Nibble 0xF0)          │
 │   └── Telemetria de Inputs & Trackpad Multitouch            │
 │                                                             │
 │  [Worker Service & Orquestração]                            │
 │   ├── Debounce Inteligente & Detecção de Desconexão         │
 │   ├── Serilog File Sink ────────> Logs/dualsenser-*.log     │
 │   └── Config Manager ───────────> Config/config.ini         │
 └─────────────────────────────────────────────────────────────┘
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
│   └── dualsenser-20260829.log         # Arquivos de log rotativos diários
│
├── server/
│   ├── DualSenser.slnx                 # Solução .NET
│   │
│   ├── DualSenser.Service/             # Projeto do Serviço Windows
│   │   ├── Common/
│   │   │   ├── ConfigManager.cs        # Carregador e criador do config.ini
│   │   │   └── LoggerConfig.cs         # Setup do Serilog (Console + Rolling File)
│   │   ├── Models/
│   │   │   ├── BatteryChargingStatus.cs
│   │   │   ├── ConnectionType.cs
│   │   │   ├── DualSenseBatteryState.cs
│   │   │   ├── DualSenseDeviceInfo.cs
│   │   │   └── DualSenseInputState.cs
│   │   ├── Hid/
│   │   │   ├── Native/                 # Assinaturas Win32 (SetupAPI, hid.dll, kernel32)
│   │   │   ├── DualSenseHidReader.cs   # Scanner, handshake e leitor assíncrono
│   │   │   └── DualSenseReportParser.cs# Decodificador de bateria e telemetria
│   │   ├── Services/
│   │   │   └── DualSenseMonitorWorker.cs# Worker em segundo plano
│   │   └── Program.cs                  # Host & Injeção de Dependências
│   │
│   └── DualSenser.Tests/               # Suíte de Testes Automatizados (xUnit)
│       ├── DualSenseReportParserTests.cs
│       ├── DualSenseInputParsingTests.cs
│       ├── ConfigManagerTests.cs
│       └── LoggerConfigTests.cs
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
; Se true, exibe no log toda atividade do controle (botoes, analogicos, gatilhos e trackpad)
ShowControllerActivity=false
```

* **`ShowControllerActivity=false` (Padrão):** O serviço opera em modo silencioso/econômico, registrando apenas eventos de conexão, desconexão, variações de carga e alertas de bateria fraca.
* **`ShowControllerActivity=true`:** Exibe no terminal e nos arquivos de log qualquer ação executada no controle (movimentos de analógicos, pressão de gatilhos, botões e gestos no trackpad).

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

> **Resultado:** 28 testes unitários cobrindo parsing de bateria (Bluetooth/USB), botões, analógicos, trackpad multitoque e gerenciamento de arquivos de log e configuração.

---

## 🗺️ Roadmap de Desenvolvimento

- [x] **Fase 1: Core do Serviço Windows & HID (Concluído)**
  - [x] Enumeração e leitura assíncrona de dispositivos DualSense (Bluetooth e USB).
  - [x] Handshake do Feature Report `0x05`.
  - [x] Parser de bateria e status de carregamento.
  - [x] Sistema de logging estruturado (Serilog) com pastas na raiz.
  - [x] Arquivo de configuração `Config/config.ini` e modo `ShowControllerActivity`.
  - [x] Suíte de testes unitários com 100% de aprovação.
- [ ] **Fase 2: Camada de Rede & WebSockets (Próxima)**
  - [ ] ASP.NET Core Kestrel Minimal API embutida (`GET /api/status`).
  - [ ] Servidor WebSocket (`WS /ws`) com streaming reativo de bateria.
  - [ ] Serviço de broadcast UDP Beacon para auto-descoberta na rede local.
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
