package com.example.ui.screens

import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.example.ui.components.TopActions
import com.example.viewmodel.NeuroVidaViewModel
import com.example.viewmodel.TopPanel

/**
 * Cabecera de las 3 pestañas (29-sep, `docs/previews/navegacion-nubi.png`): a la izquierda lo propio de cada una
 * ([left]); a la derecha tu perfil (tu inicial) y las opciones (engranaje).
 */
@Composable
fun TabTopBar(viewModel: NeuroVidaViewModel, modifier: Modifier = Modifier, left: @Composable RowScope.() -> Unit) {
  val settings by viewModel.userSettings.collectAsState()
  Row(
    modifier.fillMaxWidth().padding(start = 20.dp, end = 16.dp, top = 8.dp, bottom = 4.dp),
    verticalAlignment = Alignment.CenterVertically
  ) {
    Row(Modifier.weight(1f), verticalAlignment = Alignment.CenterVertically) { left() }
    Spacer(Modifier.width(8.dp))
    TopActions(
      name = settings.name,
      onProfile = { viewModel.openTopPanel(TopPanel.PERFIL) },
      onSettings = { viewModel.openTopPanel(TopPanel.AJUSTES) }
    )
  }
}
