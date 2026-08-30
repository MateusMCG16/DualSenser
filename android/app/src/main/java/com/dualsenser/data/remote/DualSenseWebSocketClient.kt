package com.dualsenser.data.remote

import android.util.Log
import com.dualsenser.data.model.ControllerStatusDto
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import kotlinx.serialization.json.Json
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.Response
import okhttp3.WebSocket
import okhttp3.WebSocketListener
import java.util.concurrent.TimeUnit

class DualSenseWebSocketClient(
    private val client: OkHttpClient = OkHttpClient.Builder()
        .pingInterval(10, TimeUnit.SECONDS)
        .readTimeout(0, TimeUnit.MILLISECONDS)
        .build(),
    private val json: Json = Json { ignoreUnknownKeys = true }
) {
    companion object {
        private const val TAG = "DualSenseWebSocket"
    }

    private var webSocket: WebSocket? = null
    private var reconnectJob: Job? = null
    private var currentUrl: String? = null
    private var shouldReconnect = true

    private val _statusFlow = MutableStateFlow<ControllerStatusDto?>(null)
    val statusFlow: StateFlow<ControllerStatusDto?> = _statusFlow.asStateFlow()

    private val _isConnectedFlow = MutableStateFlow(false)
    val isConnectedFlow: StateFlow<Boolean> = _isConnectedFlow.asStateFlow()

    fun connect(serverIp: String, port: Int, scope: CoroutineScope) {
        val url = "ws://$serverIp:$port/ws"
        if (currentUrl == url && _isConnectedFlow.value) return

        currentUrl = url
        shouldReconnect = true

        reconnectJob?.cancel()
        reconnectJob = scope.launch(Dispatchers.IO) {
            var backoffDelay = 1000L

            while (isActive && shouldReconnect) {
                if (!_isConnectedFlow.value) {
                    Log.d(TAG, "Tentando conectar em $url...")
                    openWebSocket(url)
                }

                delay(backoffDelay)
                if (!_isConnectedFlow.value) {
                    backoffDelay = (backoffDelay * 1.5).toLong().coerceAtMost(5000L)
                } else {
                    backoffDelay = 1000L
                }
            }
        }
    }

    private fun openWebSocket(url: String) {
        try {
            val request = Request.Builder().url(url).build()
            webSocket?.cancel()
            webSocket = client.newWebSocket(request, createListener())
        } catch (e: Exception) {
            Log.e(TAG, "Falha ao criar WebSocket para $url: ${e.message}")
            _isConnectedFlow.value = false
        }
    }

    private fun createListener(): WebSocketListener {
        return object : WebSocketListener() {
            override fun onOpen(ws: WebSocket, response: Response) {
                Log.d(TAG, "WebSocket conectado com sucesso!")
                _isConnectedFlow.value = true
            }

            override fun onMessage(ws: WebSocket, text: String) {
                try {
                    val status = json.decodeFromString<ControllerStatusDto>(text)
                    _statusFlow.value = status
                } catch (e: Exception) {
                    Log.e(TAG, "Erro ao deserializar mensagem do WebSocket: ${e.message}")
                }
            }

            override fun onClosing(ws: WebSocket, code: Int, reason: String) {
                Log.d(TAG, "WebSocket fechando: $reason ($code)")
                _isConnectedFlow.value = false
            }

            override fun onClosed(ws: WebSocket, code: Int, reason: String) {
                Log.d(TAG, "WebSocket fechado: $reason ($code)")
                _isConnectedFlow.value = false
                _statusFlow.value = null
            }

            override fun onFailure(ws: WebSocket, t: Throwable, response: Response?) {
                Log.w(TAG, "Falha no WebSocket: ${t.message}")
                _isConnectedFlow.value = false
                _statusFlow.value = null
            }
        }
    }

    fun disconnect() {
        shouldReconnect = false
        reconnectJob?.cancel()
        webSocket?.close(1000, "App desconectado")
        webSocket = null
        _isConnectedFlow.value = false
        _statusFlow.value = null
    }
}
