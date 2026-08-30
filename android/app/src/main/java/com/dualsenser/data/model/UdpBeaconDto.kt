package com.dualsenser.data.model

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
data class UdpBeaconDto(
    @SerialName("service") val service: String = "",
    @SerialName("version") val version: String = "",
    @SerialName("port") val port: Int = 5005,
    @SerialName("serverName") val serverName: String = "",
    @SerialName("timestamp") val timestamp: String = ""
)
