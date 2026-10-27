package com.example.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

private val OnNight = Color(0xFFEAF0FF)
private val OnNightDim = com.example.ui.theme.NubiTokens.OnNightDim

/** Titulo de seccion sin recuadro: texto claro con una linea fina encima que separa del bloque anterior. [fontFamily] = la del título: Rubik Bold ([TitleFamily]) desde la Tarea 175 (Avance la usó primero, Tarea 174); `null` = la de siempre. */
@Composable
fun SpaceSectionTitle(text: String, modifier: Modifier = Modifier, hint: String? = null, fontFamily: androidx.compose.ui.text.font.FontFamily? = com.example.ui.theme.TitleFamily) {
  Column(modifier = modifier.fillMaxWidth().padding(top = 8.dp)) {
    Box(
      modifier = Modifier
        .fillMaxWidth()
        .height(1.dp)
        .background(Color.White.copy(alpha = 0.10f))
    )
    OnBoardText(text, color = OnNight, fontSize = 20.sp, fontWeight = FontWeight.Bold, fontFamily = fontFamily, modifier = Modifier.padding(top = 14.dp))
    if (hint != null) OnBoardText(hint, color = OnNightDim, fontSize = 16.sp, modifier = Modifier.padding(top = 2.dp))
  }
}
