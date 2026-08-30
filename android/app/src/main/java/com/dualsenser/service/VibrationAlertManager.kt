package com.dualsenser.service

import android.content.Context
import android.os.Build
import android.os.VibrationEffect
import android.os.Vibrator
import android.os.VibratorManager
import android.util.Log

class VibrationAlertManager(private val context: Context) {

    companion object {
        private const val TAG = "VibrationAlertManager"
    }

    private val vibrator: Vibrator? = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
        val vibratorManager = context.getSystemService(Context.VIBRATOR_MANAGER_SERVICE) as? VibratorManager
        vibratorManager?.defaultVibrator
    } else {
        @Suppress("DEPRECATION")
        context.getSystemService(Context.VIBRATOR_SERVICE) as? Vibrator
    }

    private var lastAlertedPercentage: Int? = null

    fun checkAndTriggerAlert(percentage: Int, isCharging: Boolean, isConnected: Boolean) {
        if (!isConnected || isCharging) {
            lastAlertedPercentage = null
            return
        }

        if (percentage <= 5 && lastAlertedPercentage != 5) {
            lastAlertedPercentage = 5
            triggerEmergencyAlert()
        } else if (percentage <= 10 && lastAlertedPercentage != 10 && (lastAlertedPercentage == null || lastAlertedPercentage!! > 10)) {
            lastAlertedPercentage = 10
            triggerCriticalAlert()
        } else if (percentage <= 20 && lastAlertedPercentage != 20 && (lastAlertedPercentage == null || lastAlertedPercentage!! > 20)) {
            lastAlertedPercentage = 20
            triggerLowBatteryAlert()
        } else if (percentage > 20) {
            lastAlertedPercentage = null
        }
    }

    private fun triggerLowBatteryAlert() {
        Log.d(TAG, "Disparando alerta de bateria fraca (20%)...")
        // Padrão: 2 pulsos
        val timings = longArrayOf(0, 200, 150, 200)
        vibratePattern(timings)
    }

    private fun triggerCriticalAlert() {
        Log.d(TAG, "Disparando alerta de bateria crítica (10%)...")
        // Padrão: 3 pulsos fortes
        val timings = longArrayOf(0, 300, 150, 300, 150, 300)
        vibratePattern(timings)
    }

    private fun triggerEmergencyAlert() {
        Log.d(TAG, "Disparando alerta de emergência (5%)...")
        // Padrão: 1 pulso longo
        val timings = longArrayOf(0, 800)
        vibratePattern(timings)
    }

    private fun vibratePattern(timings: LongArray) {
        vibrator ?: return

        try {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                val effect = VibrationEffect.createWaveform(timings, -1)
                vibrator.vibrate(effect)
            } else {
                @Suppress("DEPRECATION")
                vibrator.vibrate(timings, -1)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Erro ao acionar vibração: ${e.message}")
        }
    }
}
