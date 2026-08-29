# DualSenser - Planejamento e Especificações do Projeto

## 1. Visão Geral

O **DualSenser** é um ecossistema composto por duas aplicações com o objetivo de monitorar a bateria do controle **Sony PlayStation 5 DualSense** conectado via Bluetooth no Windows e exibir o status em tempo real em um aplicativo nativo **Android (Kotlin)** através da rede local (Wi-Fi), emitindo alertas e vibrações antes que o controle desligue por falta de carga.

---

## 2. Arquitetura do Sistema

```
  ┌───────────────────────┐
  │   DualSense (PS5)     │
  └──────────┬────────────┘
             │ Bluetooth (HID Input Reports)
             ▼
  ┌───────────────────────────────────────────┐
  │  Windows: C# / .NET Worker Service        │
  │  ───────────────────────────────────────  │
  │  - HID Reader (VID: 0x054C, PID: 0x0CE6)  │
  │  - Parser de Bateria & Status de Carga   │
  │  - Servidor Local (WebSocket / REST API)  │
  │  - Descoberta de Rede (mDNS / UDP Beacon) │
  └──────────────────┬────────────────────────┘
                     │ Rede Local (Wi-Fi / LAN)
                     ▼
  ┌───────────────────────────────────────────┐
  │  Android: Kotlin Native App               │
  │  ───────────────────────────────────────  │
  │  - Interface em Jetpack Compose           │
  │  - Cliente WebSocket / Auto-reconnect     │
  │  - Serviço de Notificações & Vibração     │
  │  - Alertas de Bateria Crítica (20%, 10%) │
  └───────────────────────────────────────────┘
```

---

## 3. Especificação das Stacks e Componentes

### 3.1. Servidor Windows (C# / .NET 8 Worker Service)
* **Tipo:** Windows Background Worker Service (`BackgroundService` / `Microsoft.Extensions.Hosting.WindowsServices`).
* **Comunicação HID:**
  * **Vendor ID (VID):** `0x054C` (Sony Interactive Entertainment)
  * **Product ID (PID):** `0x0CE6` (DualSense padrão) ou `0x0DF2` (DualSense Edge)
  * **Biblioteca HID:** `HidSharp` ou APIs de baixo nível do Windows (`Windows.Devices.HumanInterfaceDevice` / Win32 `CreateFile` + `ReadFile`).
  * **Leitura Bluetooth:** Processamento do Input Report `0x31` (buffer de dados contendo nível de bateria de 0 a 100%, status de carregamento e conectividade).
* **Camada de Rede / API:**
  * **ASP.NET Core Kestrel Minimal APIs & WebSockets** embutido no próprio Worker Service.
  * **Endpoints:**
    * `GET /api/status`: Retorna o status atual em JSON.
    * `WS /ws`: Conexão WebSocket bidirecional para streaming em tempo real do status da bateria.
  * **Descoberta Automática (Opcional/Recomendado):** Broadcast UDP (porta fixa) para o app Android encontrar o IP do PC na rede sem necessidade de digitação manual.

### 3.2. Aplicativo Mobile (Android / Kotlin)
* **Linguagem:** Kotlin
* **Interface (UI):** Jetpack Compose + Material 3 (com suporte a tema escuro/claro e animações de nível de bateria).
* **Camada de Rede:** OkHttp / Ktor Client com suporte a WebSockets e reconexão automática resiliente.
* **Background & Notificações:**
  * Notificações persistentes no painel de notificações do Android com a % atual.
  * Alertas com vibração personalizável e som de aviso quando a bateria atingir níveis críticos (<= 20%, <= 10%, <= 5%).
* **Persistência / Preferências:** DataStore Preferences para salvar IP do servidor, limiares de alerta e preferências de vibração.

---

## 4. Estrutura de Dados e Protocolo de Comunicação

### 4.1. Payload JSON (WebSocket / REST)
```json
{
  "connected": true,
  "controllerName": "DualSense Wireless Controller",
  "batteryPercentage": 75,
  "isCharging": false,
  "batteryStatus": "discharging",
  "timestamp": "2026-08-29T18:50:00Z"
}
```

---

## 5. Estrutura Proposta de Diretórios do Projeto

```
DualSenser/
│
├── PLAN.md                          # Este documento de planejamento
│
├── server/                          # Projeto C# / .NET Worker Service
│   ├── DualSenser.Service/
│   │   ├── Controllers/
│   │   ├── Hid/                     # Leitura de relatórios HID do DualSense
│   │   ├── Services/                # Worker Service e WebSocket Manager
│   │   ├── Models/                  # DTOs e entidades
│   │   └── Program.cs
│   └── DualSenser.sln
│
└── android/                         # Projeto Android Nativo em Kotlin
    ├── app/
    │   ├── src/main/java/com/dualsenser/
    │   │   ├── data/                # WebSocket client, repositórios
    │   │   ├── domain/              # Modelos e regras de alerta
    │   │   ├── ui/                  # Telas em Jetpack Compose
    │   │   ├── service/             # Foreground Service & Notificações
    │   │   └── MainActivity.kt
    │   └── build.gradle.kts
    └── build.gradle.kts
```

---

## 6. Roteiro de Implementação (Roadmap)

1. **Fase 1: Módulo de Leitura HID no Windows (C#)**
   * Identificar o dispositivo DualSense conectado via Bluetooth.
   * Abrir stream HID e decodificar o Report `0x31` para extrair porcentagem e status de carregamento.
2. **Fase 2: Serviço de Rede & Worker Service (C#)**
   * Criar o Worker Service com Kestrel embutido.
   * Implementar o servidor WebSocket para emitir eventos periódicos e sob demanda.
   * Implementar mecanismo de broadcast UDP para auto-descoberta.
3. **Fase 3: Aplicativo Android (Kotlin / Compose)**
   * Configurar projeto Android com Jetpack Compose e OkHttp/Ktor.
   * Implementar tela de visualização do controle e nível de bateria com animações.
   * Implementar conexão WebSocket com auto-reconnect.
4. **Fase 4: Sistema de Alertas e Notificações no Android**
   * Configurar notificações de sistema e canais de notificação.
   * Implementar lógica de alerta sonoro e vibração em níveis críticos de bateria.
