package com.dualsenser

import android.app.Application
import com.dualsenser.service.NotificationHelper

class DualSenserApp : Application() {
    override fun onCreate() {
        super.onCreate()
        val notificationHelper = NotificationHelper(this)
        notificationHelper.createNotificationChannels()
    }
}
