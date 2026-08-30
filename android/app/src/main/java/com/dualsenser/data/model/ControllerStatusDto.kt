package com.dualsenser.data.model

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
data class ControllerStatusDto(
    @SerialName("connected") val connected: Boolean = false,
    @SerialName("modelName") val modelName: String = "None",
    @SerialName("connectionType") val connectionType: String = "Unknown",
    @SerialName("batteryPercentage") val batteryPercentage: Int = 0,
    @SerialName("chargingStatus") val chargingStatus: String = "Unknown",
    @SerialName("isCharging") val isCharging: Boolean = false,
    @SerialName("isFullyCharged") val isFullyCharged: Boolean = false,
    @SerialName("isCritical") val isCritical: Boolean = false,
    @SerialName("isLow") val isLow: Boolean = false,
    @SerialName("timestamp") val timestamp: String = ""
)
