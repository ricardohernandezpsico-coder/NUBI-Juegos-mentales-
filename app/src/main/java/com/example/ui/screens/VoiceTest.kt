package com.example.ui.screens

import android.Manifest
import android.content.pm.PackageManager
import android.os.Build
import android.os.SystemClock
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalClipboardManager
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.text.font.FontStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.core.content.ContextCompat
import com.example.bridge.SpeechBridge
import kotlinx.coroutines.delay
import org.json.JSONArray

/** Palabras que no cuentan como respuesta ("perro y gato", "eh…"). */
private val FillerWords = setOf("y", "e", "o", "u", "el", "la", "los", "las", "un", "una", "unos", "unas", "de", "del",
  "que", "eh", "em", "mm", "este", "esto", "bueno", "ya", "otro", "otra", "también", "tambien", "con", "a", "al")

/**
 * [Debug] Prueba de voz para Constelación de Palabras: antes de construir el juego, ver si el teléfono entiende bien
 * a la persona diciendo animales durante 60 s (qué palabras entiende, cuántas veces se reinicia el micrófono, si hay
 * errores, si funciona sin internet). Al final arma un resumen para copiar y pegarlo en la conversación.
 * Es un diálogo (herramienta de prueba), no una pantalla del juego.
 */
@Composable
fun VoiceTestDialog(onDismiss: () -> Unit) {
  val context = LocalContext.current
  val clipboard = LocalClipboardManager.current
  val available = remember { SpeechBridge.isAvailable(context) }
  val onDevice = remember { SpeechBridge.isOnDeviceAvailable(context) }
  val language = remember { SpeechBridge.defaultLanguage() }

  var offline by remember { mutableStateOf(false) }
  var running by remember { mutableStateOf(false) }
  var stopRequested by remember { mutableStateOf(false) }
  var finished by remember { mutableStateOf(false) }
  var secondsLeft by remember { mutableIntStateOf(60) }
  var partial by remember { mutableStateOf("") }
  var restarts by remember { mutableIntStateOf(0) }
  var firstWordMs by remember { mutableStateOf<Long?>(null) }
  val words = remember { mutableStateListOf<String>() }
  val phrases = remember { mutableStateListOf<String>() }
  val errors = remember { mutableStateListOf<String>() }
  var hasPermission by remember {
    mutableStateOf(ContextCompat.checkSelfPermission(context, Manifest.permission.RECORD_AUDIO) == PackageManager.PERMISSION_GRANTED)
  }

  fun begin() {
    words.clear(); phrases.clear(); errors.clear()
    partial = ""; restarts = 0; firstWordMs = null; secondsLeft = 60
    stopRequested = false; finished = false
    SpeechBridge.start(context, language, offline)
    running = true
  }

  val permission = rememberLauncherForActivityResult(ActivityResultContracts.RequestPermission()) { granted ->
    hasPermission = granted
    if (granted) begin()
  }

  LaunchedEffect(running) {
    if (!running) return@LaunchedEffect
    val start = SystemClock.elapsedRealtime()
    var stoppedAt = 0L
    while (true) {
      delay(100)
      val arr = JSONArray(SpeechBridge.poll())
      for (i in 0 until arr.length()) {
        val e = arr.getJSONObject(i)
        val text = e.optString("text")
        val t = e.optLong("t")
        when (e.optString("type")) {
          "partial" -> partial = text
          "final" -> {
            partial = ""
            val alts = e.optJSONArray("alts")?.let { a -> (0 until a.length()).joinToString(" / ") { a.getString(it) } }
            phrases.add("${t / 1000} s: $text" + (alts?.takeIf { it.isNotBlank() }?.let { "   (otras: $it)" } ?: ""))
            text.lowercase().split(Regex("[^\\p{L}]+")).filter { it.isNotBlank() && it !in FillerWords }.forEach { w ->
              if (w !in words) {
                words.add(w)
                if (firstWordMs == null) firstWordMs = t
              }
            }
          }
          "restart" -> restarts = e.optInt("code")
          "error" -> errors.add("${t / 1000} s: $text")
        }
      }
      val now = SystemClock.elapsedRealtime()
      secondsLeft = (60 - (now - start) / 1000).toInt().coerceAtLeast(0)
      if (stoppedAt == 0L && (stopRequested || now - start >= 60_000)) {
        SpeechBridge.stop()
        stoppedAt = now
      }
      // Después de detener, se espera un poco: la última palabra dicha todavía puede llegar.
      if (stoppedAt != 0L && now - stoppedAt > 1500) break
    }
    running = false
    finished = true
  }

  DisposableEffect(Unit) { onDispose { SpeechBridge.stop() } }

  val summary = buildString {
    appendLine("Prueba de voz · Constelación de Palabras")
    appendLine("Teléfono: ${Build.MANUFACTURER} ${Build.MODEL} · Android ${Build.VERSION.RELEASE}")
    appendLine("Idioma: $language · Sin internet: ${if (offline) "sí" else "no"} (el teléfono ${if (onDevice) "sí" else "no"} lo permite)")
    appendLine("Palabras distintas: ${words.size}" + (firstWordMs?.let { " (la primera a los ${it / 1000} s)" } ?: ""))
    appendLine("Reinicios del micrófono: $restarts")
    appendLine("Errores: " + (errors.joinToString("; ").ifBlank { "ninguno" }))
    appendLine("Palabras: " + words.joinToString(", "))
    appendLine("Frases tal cual:")
    phrases.forEach { appendLine("  $it") }
  }

  AlertDialog(
    onDismissRequest = { if (!running) onDismiss() },
    title = { Text("Prueba de voz") },
    text = {
      Column(
        Modifier.heightIn(max = 460.dp).verticalScroll(rememberScrollState()),
        verticalArrangement = Arrangement.spacedBy(6.dp)
      ) {
        Text("Reconocimiento de voz: " + if (available) "disponible" else "NO disponible en este teléfono")
        Text("Sin internet (en el teléfono): " + if (onDevice) "se puede" else "no se puede")
        Text("Idioma: $language")
        if (onDevice) {
          Row(verticalAlignment = Alignment.CenterVertically) {
            Switch(checked = offline, onCheckedChange = { if (!running) offline = it }, modifier = Modifier.testTag("voice_offline"))
            Text("  Probar sin internet")
          }
        }
        if (!running && !finished) {
          Text("Toca Empezar y di en voz alta todos los ANIMALES que se te ocurran durante 60 segundos, a tu ritmo.")
        }
        if (running) {
          Text("Quedan $secondsLeft s · di animales", fontWeight = FontWeight.Bold)
          if (partial.isNotBlank()) Text("…$partial", fontStyle = FontStyle.Italic)
        }
        if (words.isNotEmpty()) {
          Text("Entendió ${words.size}:", fontWeight = FontWeight.Bold)
          Text(words.joinToString(" · "))
        }
        if (running || finished) Text("Reinicios del micrófono: $restarts", fontSize = 13.sp)
        errors.takeLast(3).forEach { Text("Error: $it", fontSize = 13.sp) }
        if (finished) {
          Text("Listo. Copia el resumen y pégamelo en la conversación.", fontWeight = FontWeight.Bold)
          Text(summary, fontSize = 12.sp)
        }
      }
    },
    confirmButton = {
      when {
        running -> TextButton(onClick = { stopRequested = true }, modifier = Modifier.testTag("voice_stop")) { Text("Detener") }
        finished -> TextButton(onClick = { clipboard.setText(AnnotatedString(summary)) }, modifier = Modifier.testTag("voice_copy")) { Text("Copiar resumen") }
        else -> TextButton(
          enabled = available,
          onClick = { if (hasPermission) begin() else permission.launch(Manifest.permission.RECORD_AUDIO) },
          modifier = Modifier.testTag("voice_start")
        ) { Text("Empezar") }
      }
    },
    dismissButton = {
      Row {
        if (finished) TextButton(onClick = { begin() }) { Text("Otra vez") }
        TextButton(onClick = { if (!running) onDismiss() }, enabled = !running) { Text("Cerrar") }
      }
    }
  )
}
