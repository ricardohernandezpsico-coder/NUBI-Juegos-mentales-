package com.example.ui.theme

import android.os.Build
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.dynamicDarkColorScheme
import androidx.compose.material3.dynamicLightColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext

// Esquema unico "cosmos": fondo transparente (lo dibuja el fondo «Espacio profundo»), superficies azul noche y, de acento, los colores del KIT (Clay: cielo, tinta, coral, sol), no los azul eléctrico y naranja con que se armó la app en AI Studio.
// Todo sale de NubiTokens y Clay (Tarea 123). Cambios de color: `primary`, `secondary`, `tertiary` y sus contenedores (ningún componente visible los lee: se buscó en el código) y `onSurfaceVariant`,
// que pasa de #B4BFEA a #C7D0FF (el texto secundario sobre el cielo es uno solo). Lo demás, igual que antes.
private val CosmosColorScheme =
  darkColorScheme(
    primary = Clay.Sky,
    onPrimary = Clay.Ink,
    primaryContainer = Clay.Ink,
    onPrimaryContainer = Clay.Cream,
    secondary = Clay.Coral,
    onSecondary = Clay.Ink,
    tertiary = Clay.Sun,
    background = Color.Transparent,
    onBackground = NubiTokens.OnNight,
    surface = NubiTokens.Surface,
    onSurface = NubiTokens.OnNight,
    surfaceVariant = NubiTokens.SurfaceVariant,
    onSurfaceVariant = NubiTokens.OnNightDim,
    outline = NubiTokens.Outline,
    outlineVariant = NubiTokens.OutlineVariant
  )

@Composable
fun NeuroVidaTheme(
  darkTheme: Boolean = isSystemInDarkTheme(),
  dynamicColor: Boolean = false, // Use intentional brand theme by default
  content: @Composable () -> Unit,
) {
  // La identidad visual es unica (cosmos oscuro): darkTheme y dynamicColor se ignoran.
  val colorScheme = CosmosColorScheme

  MaterialTheme(colorScheme = colorScheme, typography = Typography, content = content)
}

