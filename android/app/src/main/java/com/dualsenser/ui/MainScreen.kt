package com.dualsenser.ui

import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.dualsenser.R
import com.dualsenser.service.DualSenserForegroundService
import com.dualsenser.ui.components.BatteryPillProgressBar
import com.dualsenser.ui.theme.AccentBlue
import com.dualsenser.ui.theme.BackgroundDark
import com.dualsenser.ui.theme.BatteryGreen
import com.dualsenser.ui.theme.SurfaceDark
import com.dualsenser.ui.theme.TextPrimary
import com.dualsenser.ui.theme.TextSecondary

@Composable
fun MainScreen(
    viewModel: MainViewModel,
    modifier: Modifier = Modifier
) {
    val state by viewModel.uiState.collectAsState()
    val context = LocalContext.current
    var showIpDialog by remember { mutableStateOf(false) }
    var inputIp by remember { 
        mutableStateOf(DualSenserForegroundService.getLastKnownIp(context).ifBlank { "192.168.15.4" }) 
    }

    // Container com respeito rigoroso à Safe Area (notch, status bar, navigation bar)
    Box(
        modifier = modifier
            .fillMaxSize()
            .background(BackgroundDark)
            .safeDrawingPadding()
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(horizontal = 24.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.SpaceBetween
        ) {
            // 1. Topo: Logo oficial, Título "Status" e Subtítulo dinâmico
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(top = 36.dp)
                    .clickable { showIpDialog = true },
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Image(
                    painter = painterResource(id = R.drawable.app_logo),
                    contentDescription = "Logo DualSenser",
                    modifier = Modifier
                        .size(72.dp)
                        .padding(bottom = 8.dp)
                )

                Text(
                    text = "Status",
                    color = TextPrimary,
                    fontSize = 32.sp,
                    fontWeight = FontWeight.Medium,
                    letterSpacing = 0.5.sp,
                    textAlign = TextAlign.Center
                )

                Spacer(modifier = Modifier.height(6.dp))

                Text(
                    text = state.statusText,
                    color = TextSecondary,
                    fontSize = 20.sp,
                    fontWeight = FontWeight.Normal,
                    letterSpacing = 0.25.sp,
                    textAlign = TextAlign.Center
                )
            }

            // 2. Centro: Barra de Bateria Estilo Pílula e Percentual
            Box(
                modifier = Modifier.weight(1f),
                contentAlignment = Alignment.Center
            ) {
                BatteryPillProgressBar(
                    percentage = state.batteryPercentage,
                    isCharging = state.isCharging,
                    isConnected = state.isConnected
                )
            }

            // 3. Rodapé discreto com status do PC (clicável para configurar IP se necessário)
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(bottom = 16.dp)
                    .clickable { showIpDialog = true },
                contentAlignment = Alignment.Center
            ) {
                if (state.serverHost.isNotEmpty()) {
                    Text(
                        text = "Servidor: ${state.serverHost}",
                        color = TextSecondary.copy(alpha = 0.6f),
                        fontSize = 12.sp,
                        textAlign = TextAlign.Center
                    )
                } else {
                    Text(
                        text = "Toque aqui para inserir IP manual do PC",
                        color = AccentBlue.copy(alpha = 0.7f),
                        fontSize = 12.sp,
                        textAlign = TextAlign.Center
                    )
                }
            }
        }
    }

    // Modal para inserção manual de IP
    if (showIpDialog) {
        AlertDialog(
            onDismissRequest = { showIpDialog = false },
            containerColor = SurfaceDark,
            title = {
                Text("Conectar ao PC", color = TextPrimary, fontWeight = FontWeight.Bold)
            },
            text = {
                Column {
                    Text(
                        "Digite o IP do computador na rede local (ex: 192.168.15.4):",
                        color = TextSecondary,
                        fontSize = 14.sp
                    )
                    Spacer(modifier = Modifier.height(12.dp))
                    OutlinedTextField(
                        value = inputIp,
                        onValueChange = { inputIp = it },
                        singleLine = true,
                        placeholder = { Text("192.168.15.4", color = TextSecondary.copy(alpha = 0.5f)) },
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedTextColor = TextPrimary,
                            unfocusedTextColor = TextPrimary,
                            focusedBorderColor = BatteryGreen,
                            unfocusedBorderColor = TextSecondary.copy(alpha = 0.3f),
                            cursorColor = BatteryGreen
                        ),
                        shape = RoundedCornerShape(8.dp)
                    )
                }
            },
            confirmButton = {
                Button(
                    onClick = {
                        if (inputIp.isNotBlank()) {
                            DualSenserForegroundService.connectManual(context, inputIp.trim())
                        }
                        showIpDialog = false
                    },
                    colors = ButtonDefaults.buttonColors(containerColor = BatteryGreen)
                ) {
                    Text("Conectar", color = BackgroundDark, fontWeight = FontWeight.Bold)
                }
            },
            dismissButton = {
                TextButton(onClick = { showIpDialog = false }) {
                    Text("Cancelar", color = TextSecondary)
                }
            }
        )
    }
}
