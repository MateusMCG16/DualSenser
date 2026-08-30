package com.dualsenser.ui

import androidx.lifecycle.ViewModel
import com.dualsenser.domain.model.ControllerUiState
import com.dualsenser.service.DualSenserForegroundService
import kotlinx.coroutines.flow.StateFlow

class MainViewModel : ViewModel() {
    val uiState: StateFlow<ControllerUiState> = DualSenserForegroundService.uiStateFlow
}
