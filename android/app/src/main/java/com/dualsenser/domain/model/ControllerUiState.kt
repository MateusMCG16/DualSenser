package com.dualsenser.domain.model

data class ControllerUiState(
    val batteryPercentage: Int = 0,
    val statusText: String = "Buscando PC na rede local…",
    val isCharging: Boolean = false,
    val isFullyCharged: Boolean = false,
    val isConnected: Boolean = false,
    val isServerConnected: Boolean = false,
    val serverHost: String = "",
    val isCritical: Boolean = false,
    val isLow: Boolean = false
) {
    val progress: Float
        get() = (batteryPercentage.coerceIn(0, 100) / 100f)

    companion object {
        val Initial = ControllerUiState()
    }
}
