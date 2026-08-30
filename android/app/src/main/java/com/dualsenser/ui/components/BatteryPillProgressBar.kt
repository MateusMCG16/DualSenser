package com.dualsenser.ui.components

import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.dualsenser.ui.theme.BatteryBackground
import com.dualsenser.ui.theme.BatteryGreen
import com.dualsenser.ui.theme.BatteryRed
import com.dualsenser.ui.theme.BatteryYellow
import com.dualsenser.ui.theme.TextPrimary
import com.dualsenser.ui.theme.TextSecondary

@Composable
fun BatteryPillProgressBar(
    percentage: Int,
    isCharging: Boolean,
    isConnected: Boolean,
    modifier: Modifier = Modifier
) {
    val targetProgress = if (isConnected) (percentage.coerceIn(0, 100) / 100f) else 0f
    val animatedProgress by animateFloatAsState(
        targetValue = targetProgress,
        animationSpec = tween(durationMillis = 600, easing = FastOutSlowInEasing),
        label = "batteryProgress"
    )

    val targetColor = when {
        !isConnected -> TextSecondary
        percentage > 40 -> BatteryGreen
        percentage > 20 -> BatteryYellow
        else -> BatteryRed
    }

    val animatedColor by animateColorAsState(
        targetValue = targetColor,
        animationSpec = tween(durationMillis = 400),
        label = "batteryColor"
    )

    val pillShape = RoundedCornerShape(percent = 50)

    Row(
        modifier = modifier,
        verticalAlignment = Alignment.CenterVertically
    ) {
        // Barra Horizontal Estilo Pílula
        Box(
            modifier = Modifier
                .width(220.dp)
                .height(20.dp)
                .clip(pillShape)
                .background(BatteryBackground)
                .border(
                    width = 1.dp,
                    color = Color.White.copy(alpha = 0.08f),
                    shape = pillShape
                )
        ) {
            Box(
                modifier = Modifier
                    .fillMaxHeight()
                    .fillMaxWidth(animatedProgress)
                    .clip(pillShape)
                    .background(animatedColor)
            )
        }

        Spacer(modifier = Modifier.width(16.dp))

        // Percentual Numérico ao lado da Barra
        val percentText = if (isConnected) {
            if (isCharging) "$percentage% ⚡" else "$percentage%"
        } else {
            "--"
        }

        Text(
            text = percentText,
            color = if (isConnected) animatedColor else TextSecondary,
            fontSize = 18.sp,
            fontWeight = FontWeight.Medium
        )
    }
}
