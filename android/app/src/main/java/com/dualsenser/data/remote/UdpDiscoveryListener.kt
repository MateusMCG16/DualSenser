package com.dualsenser.data.remote

import android.content.Context
import android.net.wifi.WifiManager
import android.util.Log
import com.dualsenser.data.model.UdpBeaconDto
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.flowOn
import kotlinx.coroutines.isActive
import kotlinx.serialization.json.Json
import java.net.DatagramPacket
import java.net.DatagramSocket
import java.net.InetAddress
import java.net.InetSocketAddress
import kotlin.coroutines.coroutineContext

class UdpDiscoveryListener(
    private val context: Context,
    private val listeningPort: Int = 54321
) {
    companion object {
        private const val TAG = "UdpDiscoveryListener"
        const val DEFAULT_PORT = 54321
    }

    private val json = Json { 
        ignoreUnknownKeys = true 
        isLenient = true
    }

    data class DiscoveredServer(
        val ipAddress: String,
        val httpPort: Int,
        val serverName: String
    )

    fun listenForBeacons(): Flow<DiscoveredServer> = flow {
        var socket: DatagramSocket? = null
        var multicastLock: WifiManager.MulticastLock? = null
        val buffer = ByteArray(2048)

        try {
            // Adquire o MulticastLock para permitir recebimento de pacotes UDP broadcast no Wi-Fi
            try {
                val wifiManager = context.applicationContext.getSystemService(Context.WIFI_SERVICE) as? WifiManager
                multicastLock = wifiManager?.createMulticastLock("DualSenserMulticastLock")?.apply {
                    setReferenceCounted(true)
                    acquire()
                }
                Log.d(TAG, "MulticastLock adquirido com sucesso.")
            } catch (e: Exception) {
                Log.w(TAG, "Não foi possível adquirir MulticastLock: ${e.message}")
            }

            val portToUse = if (listeningPort in 1024..65535) listeningPort else DEFAULT_PORT
            Log.d(TAG, "Iniciando listener UDP na porta $portToUse...")

            socket = DatagramSocket(null).apply {
                reuseAddress = true
                broadcast = true
                bind(InetSocketAddress(InetAddress.getByName("0.0.0.0"), portToUse))
                soTimeout = 3000
            }

            Log.d(TAG, "Socket UDP aberto com sucesso na porta $portToUse")

            while (coroutineContext.isActive) {
                try {
                    val packet = DatagramPacket(buffer, buffer.size)
                    socket.receive(packet)

                    val senderIp = packet.address.hostAddress ?: continue
                    val payloadText = String(packet.data, 0, packet.length, Charsets.UTF_8)

                    val beacon = json.decodeFromString<UdpBeaconDto>(payloadText)
                    if (beacon.service.equals("DualSenser", ignoreCase = true)) {
                        Log.d(TAG, "Beacon recebido com sucesso de $senderIp: ${beacon.serverName}")
                        val discovered = DiscoveredServer(
                            ipAddress = senderIp,
                            httpPort = if (beacon.port in 1..65535) beacon.port else 5005,
                            serverName = beacon.serverName
                        )
                        emit(discovered)
                    }
                } catch (e: java.net.SocketTimeoutException) {
                    // Timeout esperado para checar cancelamento
                } catch (e: Exception) {
                    Log.w(TAG, "Aviso ao processar pacote UDP: ${e.message}")
                }
            }
        } catch (e: Exception) {
            Log.e(TAG, "Erro fatal ao inicializar socket UDP: ${e.message}", e)
        } finally {
            try {
                socket?.close()
            } catch (_: Exception) {}
            try {
                if (multicastLock?.isHeld == true) {
                    multicastLock.release()
                }
            } catch (_: Exception) {}
            Log.d(TAG, "Socket de descoberta UDP finalizado.")
        }
    }.flowOn(Dispatchers.IO)
}
