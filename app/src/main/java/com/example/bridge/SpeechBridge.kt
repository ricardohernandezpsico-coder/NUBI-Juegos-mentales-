package com.example.bridge

import android.content.Context
import android.content.Intent
import android.os.Build
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.os.SystemClock
import android.speech.RecognitionListener
import android.speech.RecognizerIntent
import android.speech.SpeechRecognizer
import android.util.Log
import org.json.JSONArray
import org.json.JSONObject
import java.util.Locale

/**
 * Escucha continua con el reconocimiento de voz de Android, para Constelación de Palabras (fluidez verbal: decir en voz
 * alta todas las palabras de una categoría en 60 s).
 *
 * El reconocedor de Android escucha UNA frase y se detiene al quedar en silencio; para escuchar un minuto entero se
 * vuelve a arrancar solo después de cada resultado o silencio (`restarts` cuenta cuántas veces). Todo lo que pasa queda
 * en una cola de eventos que se lee con [poll] (JSON): así lo usa igual la prueba de voz de la app (Compose) y, más
 * adelante, el juego en Unity (que corre en otro proceso y llama a esta clase con `AndroidJavaClass`, sin callbacks).
 *
 * Eventos: `ready` (escuchando), `partial` (lo que va entendiendo), `final` (una frase terminada; `alts` = otras
 * lecturas posibles, sirven para buscar la palabra en la lista del juego), `restart`, `error` (código de Android),
 * `end`.
 */
object SpeechBridge {
  private const val TAG = "SpeechBridge"

  private val main = Handler(Looper.getMainLooper())
  private val events = mutableListOf<JSONObject>()
  private var recognizer: SpeechRecognizer? = null
  private var appContext: Context? = null
  @Volatile private var active = false
  private var startedAt = 0L
  private var language = "es-419"
  private var preferOffline = false
  private var restarts = 0

  /** ¿Hay un servicio de reconocimiento de voz en el teléfono? (necesita el `<queries>` del manifiesto). */
  @JvmStatic
  fun isAvailable(context: Context): Boolean = SpeechRecognizer.isRecognitionAvailable(context)

  /** ¿Puede reconocer en el propio teléfono, sin internet? (Android 12+). */
  @JvmStatic
  fun isOnDeviceAvailable(context: Context): Boolean =
    Build.VERSION.SDK_INT >= Build.VERSION_CODES.S && SpeechRecognizer.isOnDeviceRecognitionAvailable(context)

  /** Idioma a pedir: el del teléfono si es español (es-CL, es-AR…); si no, español latinoamericano. */
  @JvmStatic
  fun defaultLanguage(): String = Locale.getDefault().let { if (it.language == "es") it.toLanguageTag() else "es-419" }

  @JvmStatic
  fun isActive(): Boolean = active

  @JvmStatic
  fun restartCount(): Int = restarts

  /** Empieza a escuchar sin parar hasta [stop]. [offline] = reconocer en el teléfono (Android 12+, si está). */
  @JvmStatic
  @JvmOverloads
  fun start(context: Context, languageTag: String = defaultLanguage(), offline: Boolean = false) {
    main.post {
      stopInternal(emit = false)
      appContext = context.applicationContext
      language = languageTag
      preferOffline = offline
      restarts = 0
      startedAt = SystemClock.elapsedRealtime()
      synchronized(events) { events.clear() }
      active = true
      val ctx = appContext!!
      recognizer = try {
        if (offline && isOnDeviceAvailable(ctx) && Build.VERSION.SDK_INT >= Build.VERSION_CODES.S)
          SpeechRecognizer.createOnDeviceSpeechRecognizer(ctx)
        else SpeechRecognizer.createSpeechRecognizer(ctx)
      } catch (e: Exception) {
        Log.e(TAG, "No se pudo crear el reconocedor", e)
        emit("error", "no_recognizer", code = -1)
        active = false
        null
      }
      recognizer?.setRecognitionListener(listener)
      listen()
    }
  }

  /** Deja de escuchar (la última frase que se estaba diciendo todavía puede llegar como `final`). */
  @JvmStatic
  fun stop() {
    main.post { stopInternal(emit = true) }
  }

  /** Eventos nuevos desde la última lectura, como arreglo JSON (vacío = "[]"). */
  @JvmStatic
  fun poll(): String {
    val out = JSONArray()
    synchronized(events) {
      events.forEach { out.put(it) }
      events.clear()
    }
    return out.toString()
  }

  // ------------------------------------------------------------------ interno

  private fun listen() {
    val r = recognizer ?: return
    if (!active) return
    val intent = Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH).apply {
      putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL, RecognizerIntent.LANGUAGE_MODEL_FREE_FORM)
      putExtra(RecognizerIntent.EXTRA_LANGUAGE, language)
      putExtra(RecognizerIntent.EXTRA_PARTIAL_RESULTS, true)
      putExtra(RecognizerIntent.EXTRA_MAX_RESULTS, 3)
      putExtra(RecognizerIntent.EXTRA_PREFER_OFFLINE, preferOffline)
      putExtra(RecognizerIntent.EXTRA_CALLING_PACKAGE, appContext?.packageName)
      // Pedir que no corte tan rápido con las pausas entre palabras (varios reconocedores lo ignoran).
      putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_COMPLETE_SILENCE_LENGTH_MILLIS, 2500L)
      putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_POSSIBLY_COMPLETE_SILENCE_LENGTH_MILLIS, 2000L)
    }
    try {
      r.startListening(intent)
    } catch (e: Exception) {
      Log.e(TAG, "startListening falló", e)
      emit("error", "start_failed", code = -2)
    }
  }

  private fun restart(delayMs: Long) {
    if (!active) return
    restarts++
    emit("restart", "", code = restarts)
    main.postDelayed({ if (active) listen() }, delayMs)
  }

  private fun stopInternal(emit: Boolean) {
    val wasActive = active
    active = false
    recognizer?.let {
      try {
        it.stopListening()
        it.destroy()
      } catch (_: Exception) {
      }
    }
    recognizer = null
    if (emit && wasActive) emit("end", "", code = restarts)
  }

  private fun emit(type: String, text: String, code: Int = 0, alts: List<String> = emptyList()) {
    val e = JSONObject()
      .put("type", type)
      .put("text", text)
      .put("code", code)
      .put("t", SystemClock.elapsedRealtime() - startedAt)
    if (alts.isNotEmpty()) e.put("alts", JSONArray(alts))
    synchronized(events) { events.add(e) }
  }

  private val listener = object : RecognitionListener {
    override fun onReadyForSpeech(params: Bundle?) = emit("ready", "")
    override fun onBeginningOfSpeech() {}
    override fun onRmsChanged(rmsdB: Float) {}
    override fun onBufferReceived(buffer: ByteArray?) {}
    override fun onEndOfSpeech() {}
    override fun onEvent(eventType: Int, params: Bundle?) {}

    override fun onPartialResults(partialResults: Bundle?) {
      val list = partialResults?.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION) ?: return
      list.firstOrNull()?.takeIf { it.isNotBlank() }?.let { emit("partial", it) }
    }

    override fun onResults(results: Bundle?) {
      val list = results?.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION).orEmpty()
      list.firstOrNull()?.takeIf { it.isNotBlank() }?.let { emit("final", it, alts = list.drop(1)) }
      restart(60)
    }

    override fun onError(error: Int) {
      when (error) {
        // Silencio o no entendió nada: se sigue escuchando sin avisar.
        SpeechRecognizer.ERROR_NO_MATCH, SpeechRecognizer.ERROR_SPEECH_TIMEOUT -> restart(60)
        SpeechRecognizer.ERROR_RECOGNIZER_BUSY -> {
          recognizer?.cancel()
          restart(300)
        }
        SpeechRecognizer.ERROR_CLIENT -> restart(200)
        SpeechRecognizer.ERROR_INSUFFICIENT_PERMISSIONS -> {
          emit("error", "sin permiso de micrófono", code = error)
          stopInternal(emit = true)
        }
        else -> {
          // Red, servidor, idioma no disponible…: se avisa y se reintenta (si se repite, el juego pasará al teclado).
          emit("error", errorName(error), code = error)
          restart(500)
        }
      }
    }
  }

  @JvmStatic
  fun errorName(code: Int): String = when (code) {
    SpeechRecognizer.ERROR_NETWORK_TIMEOUT -> "red lenta"
    SpeechRecognizer.ERROR_NETWORK -> "sin red"
    SpeechRecognizer.ERROR_AUDIO -> "micrófono"
    SpeechRecognizer.ERROR_SERVER -> "servidor"
    SpeechRecognizer.ERROR_CLIENT -> "cliente"
    SpeechRecognizer.ERROR_SPEECH_TIMEOUT -> "silencio"
    SpeechRecognizer.ERROR_NO_MATCH -> "no entendió"
    SpeechRecognizer.ERROR_RECOGNIZER_BUSY -> "ocupado"
    SpeechRecognizer.ERROR_INSUFFICIENT_PERMISSIONS -> "sin permiso"
    12 -> "idioma no disponible"          // ERROR_LANGUAGE_NOT_SUPPORTED (Android 12+)
    13 -> "idioma sin descargar"          // ERROR_LANGUAGE_UNAVAILABLE (Android 12+)
    11 -> "servidor desconectado"         // ERROR_SERVER_DISCONNECTED
    10 -> "demasiadas solicitudes"        // ERROR_TOO_MANY_REQUESTS
    else -> "error $code"
  }
}
