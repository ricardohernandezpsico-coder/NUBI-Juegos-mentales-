package com.example.ui.components

import android.content.Context
import android.provider.Settings
import androidx.compose.runtime.Composable
import androidx.compose.runtime.compositionLocalOf
import androidx.compose.runtime.remember
import androidx.compose.ui.platform.LocalContext

/**
 * "Quitar animaciones" (docs/movimiento-reducido.md): la escala de animacion del sistema en 0. Es el mismo dato que la
 * app le manda a Unity (`UnityGameLauncher`: `reduce_motion`). Un solo lugar para leerlo.
 *
 * [LocalReduceMotion] en `null` = leerlo del sistema; las pruebas lo fijan en true/false con
 * `CompositionLocalProvider`. Regla: se queda el movimiento esencial (el que ES la informacion), se quita lo
 * decorativo (titileos, respiracion, pulsos, paralaje, giros de fondo).
 */
val LocalReduceMotion = compositionLocalOf<Boolean?> { null }

fun systemReduceMotion(context: Context): Boolean =
  Settings.Global.getFloat(context.contentResolver, Settings.Global.ANIMATOR_DURATION_SCALE, 1f) == 0f

/** Lee la escala UNA vez por composicion (no se vuelve a leer mientras la pantalla esta abierta). */
@Composable
fun rememberReduceMotion(): Boolean {
  LocalReduceMotion.current?.let { return it }
  val context = LocalContext.current
  return remember { systemReduceMotion(context) }
}
