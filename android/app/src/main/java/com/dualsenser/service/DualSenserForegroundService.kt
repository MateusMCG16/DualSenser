package com.dualsenser.service

import android.app.Service
import android.content.Context
import android.content.Intent
import android.content.SharedPreferences
import android.os.IBinder
import android.util.Log
import androidx.core.content.ContextCompat
import com.dualsenser.data.remote.DualSenseWebSocketClient
import com.dualsenser.data.remote.UdpDiscoveryListener
import com.dualsenser.domain.model.ControllerUiState
import kotlinx.coroutines.CoroutineExceptionHandler
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.catch
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch

class DualSenserForegroundService : Service() {

    companion object {
        private const val TAG = "DualSenserService"
        private const val PREFS_NAME = "dualsenser_prefs"
        private const val KEY_LAST_IP = "last_server_ip"
        private const val KEY_LAST_PORT = "last_server_port"

        const val ACTION_START = "ACTION_START"
        const val ACTION_STOP = "ACTION_STOP"
        const val ACTION_CONNECT_MANUAL = "ACTION_CONNECT_MANUAL"
        const val EXTRA_IP = "EXTRA_IP"
        const val EXTRA_PORT = "EXTRA_PORT"

        private val _uiStateFlow = MutableStateFlow(ControllerUiState.Initial)
        val uiStateFlow: StateFlow<ControllerUiState> = _uiStateFlow.asStateFlow()

        fun startService(context: Context) {
            val intent = Intent(context, DualSenserForegroundService::class.java).apply {
                action = ACTION_START
            }
            ContextCompat.startForegroundService(context, intent)
        }

        fun stopService(context: Context) {
            val intent = Intent(context, DualSenserForegroundService::class.java).apply {
                action = ACTION_STOP
            }
            context.stopService(intent)
        }

        fun connectManual(context: Context, ip: String, port: Int = 5005) {
            val intent = Intent(context, DualSenserForegroundService::class.java).apply {
                action = ACTION_CONNECT_MANUAL
                putExtra(EXTRA_IP, ip)
                putExtra(EXTRA_PORT, port)
            }
            ContextCompat.startForegroundService(context, intent)
        }

        fun getLastKnownIp(context: Context): String {
            val prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
            return prefs.getString(KEY_LAST_IP, "") ?: ""
        }

        fun saveServerIp(context: Context, ip: String, port: Int = 5005) {
            val prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
            prefs.edit().putString(KEY_LAST_IP, ip).putInt(KEY_LAST_PORT, port).apply()
        }
    }

    private val exceptionHandler = CoroutineExceptionHandler { _, throwable ->
        Log.e(TAG, "Exceção não tratada capturada no CoroutineScope: ${throwable.message}", throwable)
    }

    private val serviceScope = CoroutineScope(Dispatchers.Main + SupervisorJob() + exceptionHandler)
    private lateinit var notificationHelper: NotificationHelper
    private lateinit var vibrationAlertManager: VibrationAlertManager
    private val webSocketClient = DualSenseWebSocketClient()
    private lateinit var udpDiscoveryListener: UdpDiscoveryListener
    private lateinit var prefs: SharedPreferences

    override fun onCreate() {
        super.onCreate()
        prefs = getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
        notificationHelper = NotificationHelper(this)
        vibrationAlertManager = VibrationAlertManager(this)
        udpDiscoveryListener = UdpDiscoveryListener(this)

        try {
            notificationHelper.createNotificationChannels()
            startForeground(
                NotificationHelper.NOTIFICATION_ID_FOREGROUND,
                notificationHelper.buildForegroundNotification(_uiStateFlow.value)
            )
        } catch (e: Exception) {
            Log.e(TAG, "Erro ao iniciar foreground notification: ${e.message}", e)
        }

        // Tentar conectar imediatamente ao último IP conhecido caso exista
        val lastIp = prefs.getString(KEY_LAST_IP, "") ?: ""
        val lastPort = prefs.getInt(KEY_LAST_PORT, 5005)
        if (lastIp.isNotBlank()) {
            Log.d(TAG, "Tentando conectar ao último IP salvo: $lastIp:$lastPort")
            webSocketClient.connect(lastIp, lastPort, serviceScope)
            _uiStateFlow.value = _uiStateFlow.value.copy(
                serverHost = lastIp
            )
        }

        observeDiscoveryAndWebSocket()
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        when (intent?.action) {
            ACTION_STOP -> {
                stopSelf()
                return START_NOT_STICKY
            }
            ACTION_CONNECT_MANUAL -> {
                val ip = intent.getStringExtra(EXTRA_IP) ?: ""
                val port = intent.getIntExtra(EXTRA_PORT, 5005)
                if (ip.isNotBlank()) {
                    Log.d(TAG, "Conectando manualmente ao servidor: $ip:$port")
                    saveServerIp(this, ip, port)
                    webSocketClient.connect(ip, port, serviceScope)
                    _uiStateFlow.value = _uiStateFlow.value.copy(
                        serverHost = ip
                    )
                }
            }
        }
        return START_STICKY
    }

    private fun observeDiscoveryAndWebSocket() {
        // 1. Escuta Beacons UDP para auto-descoberta do PC na rede local
        serviceScope.launch(Dispatchers.IO) {
            udpDiscoveryListener.listenForBeacons()
                .catch { e -> Log.e(TAG, "Erro capturado no fluxo UDP: ${e.message}") }
                .collectLatest { server ->
                    Log.d(TAG, "Servidor descoberto via UDP: ${server.ipAddress}:${server.httpPort} (${server.serverName})")
                    saveServerIp(this@DualSenserForegroundService, server.ipAddress, server.httpPort)
                    webSocketClient.connect(server.ipAddress, server.httpPort, serviceScope)

                    _uiStateFlow.value = _uiStateFlow.value.copy(
                        serverHost = "${server.serverName} (${server.ipAddress})"
                    )
                }
        }

        // 2. Escuta mudanças de status recebidas via WebSocket
        serviceScope.launch {
            webSocketClient.statusFlow
                .catch { e -> Log.e(TAG, "Erro capturado no fluxo do WebSocket: ${e.message}") }
                .collectLatest { dto ->
                    val newState = if (dto != null && dto.connected) {
                        val statusLabel = when (dto.chargingStatus.lowercase()) {
                            "charging" -> "Carregando"
                            "full" -> "Carga Completa"
                            else -> "Descarregando"
                        }

                        ControllerUiState(
                            batteryPercentage = dto.batteryPercentage,
                            statusText = statusLabel,
                            isCharging = dto.isCharging,
                            isFullyCharged = dto.isFullyCharged,
                            isConnected = true,
                            isServerConnected = true,
                            serverHost = _uiStateFlow.value.serverHost,
                            isCritical = dto.isCritical,
                            isLow = dto.isLow
                        )
                    } else if (webSocketClient.isConnectedFlow.value) {
                        ControllerUiState(
                            batteryPercentage = 0,
                            statusText = "Desconectado",
                            isCharging = false,
                            isFullyCharged = false,
                            isConnected = false,
                            isServerConnected = true,
                            serverHost = _uiStateFlow.value.serverHost,
                            isCritical = false,
                            isLow = false
                        )
                    } else {
                        ControllerUiState(
                            batteryPercentage = 0,
                            statusText = "Buscando PC na rede local…",
                            isCharging = false,
                            isFullyCharged = false,
                            isConnected = false,
                            isServerConnected = false,
                            serverHost = "",
                            isCritical = false,
                            isLow = false
                        )
                    }

                    _uiStateFlow.value = newState

                    // Atualizar notificação e disparar vibração de alerta se necessário
                    try {
                        notificationHelper.updateForegroundNotification(newState)
                        if (newState.isConnected) {
                            vibrationAlertManager.checkAndTriggerAlert(
                                percentage = newState.batteryPercentage,
                                isCharging = newState.isCharging,
                                isConnected = newState.isConnected
                            )
                        }
                    } catch (e: Exception) {
                        Log.e(TAG, "Erro ao atualizar notificação/vibração: ${e.message}")
                    }
                }
        }
    }

    override fun onDestroy() {
        super.onDestroy()
        webSocketClient.disconnect()
        serviceScope.cancel()
        Log.d(TAG, "DualSenserForegroundService destruído.")
    }

    override fun onBind(intent: Intent?): IBinder? = null
}
