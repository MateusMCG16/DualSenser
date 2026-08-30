package com.dualsenser.service

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.os.Build
import androidx.core.app.NotificationCompat
import com.dualsenser.MainActivity
import com.dualsenser.R
import com.dualsenser.domain.model.ControllerUiState

class NotificationHelper(private val context: Context) {

    companion object {
        const val CHANNEL_ID_BATTERY = "channel_dualsense_battery"
        const val CHANNEL_ID_ALERT = "channel_dualsense_alert"
        const val NOTIFICATION_ID_FOREGROUND = 1001
        const val NOTIFICATION_ID_CRITICAL = 1002
    }

    private val notificationManager = context.getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager

    fun createNotificationChannels() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val batteryChannel = NotificationChannel(
                CHANNEL_ID_BATTERY,
                context.getString(R.string.channel_name_battery),
                NotificationManager.IMPORTANCE_LOW
            ).apply {
                description = context.getString(R.string.channel_desc_battery)
                setShowBadge(false)
            }

            val alertChannel = NotificationChannel(
                CHANNEL_ID_ALERT,
                context.getString(R.string.channel_name_alert),
                NotificationManager.IMPORTANCE_HIGH
            ).apply {
                description = context.getString(R.string.channel_desc_alert)
                enableVibration(true)
            }

            notificationManager.createNotificationChannel(batteryChannel)
            notificationManager.createNotificationChannel(alertChannel)
        }
    }

    fun buildForegroundNotification(state: ControllerUiState): Notification {
        val intent = Intent(context, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_SINGLE_TOP
        }
        val pendingIntent = PendingIntent.getActivity(
            context,
            0,
            intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )

        val title = if (state.isConnected) {
            "DualSense: ${state.batteryPercentage}%"
        } else {
            "DualSense Monitor"
        }

        val contentText = state.statusText

        return NotificationCompat.Builder(context, CHANNEL_ID_BATTERY)
            .setContentTitle(title)
            .setContentText(contentText)
            .setSmallIcon(R.drawable.ic_launcher_foreground)
            .setContentIntent(pendingIntent)
            .setOngoing(true)
            .setOnlyAlertOnce(true)
            .build()
    }

    fun updateForegroundNotification(state: ControllerUiState) {
        val notification = buildForegroundNotification(state)
        notificationManager.notify(NOTIFICATION_ID_FOREGROUND, notification)
    }

    fun showCriticalBatteryAlert(percentage: Int) {
        val intent = Intent(context, MainActivity::class.java)
        val pendingIntent = PendingIntent.getActivity(
            context,
            1,
            intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )

        val notification = NotificationCompat.Builder(context, CHANNEL_ID_ALERT)
            .setContentTitle("⚠️ Bateria do DualSense Crítica!")
            .setContentText("A bateria está em $percentage%. Conecte o cabo para não desligar.")
            .setSmallIcon(R.drawable.ic_launcher_foreground)
            .setContentIntent(pendingIntent)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setAutoCancel(true)
            .build()

        notificationManager.notify(NOTIFICATION_ID_CRITICAL, notification)
    }
}
