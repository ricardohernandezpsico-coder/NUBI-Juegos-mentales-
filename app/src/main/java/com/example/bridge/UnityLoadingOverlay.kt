package com.example.bridge

import android.app.Activity
import android.app.Application
import android.os.Bundle
import android.util.Log
import android.view.Choreographer
import android.view.View
import android.view.ViewGroup
import android.widget.FrameLayout
import androidx.activity.ComponentActivity
import androidx.compose.ui.platform.ComposeView
import com.example.data.TimeTrialDay
import com.example.model.GameRegistry
import com.example.ui.components.GameLoadingScreen
import com.unity3d.player.UnityPlayerGameActivity
import org.json.JSONObject
import java.lang.ref.WeakReference

/**
 * Pantalla de carga ENCIMA de la Activity de Unity mientras el juego se arma: tanto en el arranque en frío (primera
 * partida tras abrir la app, o si Android cerró el proceso `:unity`) como con Unity ya vivo (Tarea 190: antes en
 * caliente no había capa y se veía el azul vacío de Unity y, después, la cuenta regresiva). Se ve [GameLoadingScreen]
 * (planeta, nombre y etapa), la misma pantalla que la app muestra un instante antes (`UnityGameHost`), así el paso
 * app -> Unity no se nota.
 *
 * Se instala desde `NeuroVidaApplication` (corre en cada proceso; solo actúa sobre la Activity de Unity). En frío la
 * capa se agrega en el primer onStart de la Activity (después de que Unity armó su vista; en onCreate Unity la
 * reemplazaría); en caliente, en el onResume con un Intent de partida NUEVA (otro id de lanzamiento: volver a una partida en pausa
 * con el mismo id no la tapa). Se quita con un fundido cuando Unity avisa que su TABLERO ESTÁ COMPLETO
 * (`NativeBridge.NotifyGameShown` -> [NativeReceiver.onGameShown], desde `GameReadiness.Report`: la escena armada, el
 * arte horneado y el sonido calculado; ya no tres cuadros después de la configuración). Si ese aviso no llega
 * (Unity viejo en el APK, error), se quita sola a los [TIMEOUT_MS].
 */
object UnityLoadingOverlay : Application.ActivityLifecycleCallbacks {
  private const val TAG = "UnityLoadingOverlay"
  private const val TIMEOUT_MS = 45_000L // más que el tope de espera de Unity (30 s para el sonido) y su arte: en un teléfono lento el juego más pesado puede tardar bastante
  private const val FADE_MS = 250L
  private const val FADE_CALM_MS = 150L // con «quitar animaciones»
  private const val WARM_CONTENT_DELAY_MS = 250L // en caliente el contenido espera un poco para no destellar si el juego llega rápido
  private const val NIGHT = 0xFF100F3A.toInt() // = NeuroStyle.NightMid: el tono del medio del cielo único de la app (CosmosBackground, Tarea 126)

  /** Activity de Unity recién creada que todavía no tiene la capa (se agrega en su onStart). */
  private var pending: WeakReference<Activity>? = null

  /** Capa visible; se lee desde el hilo de Unity al avisar. */
  @Volatile
  private var overlay: WeakReference<View>? = null

  /** El id del último lanzamiento que esta capa ya tapó: un Intent con otro id es una partida nueva (y se tapa); el mismo, volver a una partida en pausa (no). */
  private var coveredLaunchId: String? = null

  /** Cuánto dura el fundido al irse: 250 ms, 150 ms con «quitar animaciones» (se decide al mostrar la capa, con la config de la partida). */
  @Volatile
  private var fadeMs = FADE_MS

  /** La sonda de la animación de la carga (Tarea 190b, SOLO en depuración): mide cada cuadro mientras la capa está visible. */
  private var jank: LoadingFrameProbe? = null

  fun install(app: Application) = app.registerActivityLifecycleCallbacks(this)

  override fun onActivityCreated(activity: Activity, savedInstanceState: Bundle?) {
    if (activity is UnityPlayerGameActivity) pending = WeakReference(activity)
  }

  override fun onActivityStarted(activity: Activity) {
    if (pending?.get() !== activity) return
    pending = null
    try {
      show(activity, warm = false)
    } catch (e: Exception) {
      // Nunca debe impedir que el juego arranque.
      Log.e(TAG, "No se pudo mostrar la pantalla de carga", e)
      com.example.diag.ErrorLog.record("PANTALLA", "No se pudo mostrar la pantalla de carga sobre Unity.", e)
    }
  }

  private fun show(activity: Activity, warm: Boolean) {
    val json = activity.intent?.getStringExtra(UnityGameLauncher.EXTRA_CONFIG_JSON) ?: return
    val root = JSONObject(json)
    coveredLaunchId = activity.intent?.getStringExtra(UnityGameLauncher.EXTRA_LAUNCH_ID)
    val game = GameRegistry.allGames.find { it.id == root.optString("game_id") } ?: return
    val config = root.optJSONObject("config")
    fadeMs = if (config?.optBoolean("reduce_motion") == true) FADE_CALM_MS else FADE_MS
    val level = config?.optInt("level", 0)?.takeIf { it > 0 }
    // Los cuatro juegos con Contrarreloj ya no eligen el reloj (Tarea 181): la carga no dice «Sin reloj»; un Contrarreloj dice su nombre («de hoy» solo si la pista es la del día de ese juego)
    val timeTrial = config?.optBoolean("time_trial") == true && TimeTrialDay.supports(game.id)
    val timed = if (config?.has("timed") == true && !TimeTrialDay.supports(game.id)) config.optBoolean("timed") else null
    val timeTrialName = if (timeTrial) TimeTrialDay.nameFor(game.id, config?.optInt("track_seed", 0)?.let { TimeTrialDay.dateKeyOfSeed(it) }.orEmpty()) else null
    val assessmentStep = if (config?.optBoolean("assessment") == true) config.optInt("assessment_step", 0) else 0
    val assessmentTotal = config?.optInt("assessment_total", 0) ?: 0

    val layer = FrameLayout(activity).apply {
      setBackgroundColor(NIGHT)
      isClickable = true // mientras carga, los toques no llegan al juego
    }
    if (activity is ComponentActivity) {
      // ComponentActivity.addContentView deja listos los "dueños" de ciclo de vida que ComposeView necesita.
      layer.addView(
        ComposeView(activity).apply {
          setContent {
            GameLoadingScreen(
              game = game, level = level, timed = timed, withSky = true, fadeInDelayMs = if (warm) WARM_CONTENT_DELAY_MS else 0L,
              assessmentStep = assessmentStep, assessmentTotal = assessmentTotal, timeTrialName = timeTrialName
            )
          }
        },
        FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT)
      )
    }
    activity.addContentView(
      layer,
      ViewGroup.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT)
    )
    overlay = WeakReference(layer)
    layer.postDelayed({ hide(layer) }, TIMEOUT_MS)
    if (com.example.BuildConfig.DEBUG) {
      jank = LoadingFrameProbe(game.id, warm, activity).also { it.start() }
    }
  }

  /** Unity ya pintó el juego (cualquier hilo). */
  fun hideFromAnyThread() {
    val layer = overlay?.get() ?: return
    layer.post { hide(layer) }
  }

  private fun hide(layer: View) {
    if (overlay?.get() !== layer) return
    overlay = null
    layer.animate().alpha(0f).setDuration(fadeMs).withEndAction {
      (layer.parent as? ViewGroup)?.removeView(layer)
      jank?.stopAndWrite()
      jank = null
    }.start()
  }

  /** Unity ya vivo y una partida NUEVA (otro id de lanzamiento): se tapa mientras el juego se arma. */
  override fun onActivityResumed(activity: Activity) {
    if (activity !is UnityPlayerGameActivity || overlay?.get() != null) return
    val id = activity.intent?.getStringExtra(UnityGameLauncher.EXTRA_LAUNCH_ID) ?: return
    if (id == coveredLaunchId) return
    try {
      show(activity, warm = true)
    } catch (e: Exception) {
      Log.e(TAG, "No se pudo mostrar la pantalla de carga sobre Unity en caliente", e)
      com.example.diag.ErrorLog.record("PANTALLA", "No se pudo mostrar la pantalla de carga sobre Unity en caliente.", e)
    }
  }
  override fun onActivityPaused(activity: Activity) = Unit
  override fun onActivityStopped(activity: Activity) = Unit
  override fun onActivitySaveInstanceState(activity: Activity, outState: Bundle) = Unit
  override fun onActivityDestroyed(activity: Activity) {
    if (pending?.get() === activity) pending = null
  }
}
