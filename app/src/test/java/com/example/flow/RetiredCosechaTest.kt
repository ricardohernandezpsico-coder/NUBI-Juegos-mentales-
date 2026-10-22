package com.example.flow

import android.app.Application
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onAllNodesWithText
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.TestSupport
import com.example.data.AreaProgress
import com.example.data.MeasurePoint
import com.example.data.PlayMode
import com.example.data.ProgressPoint
import com.example.data.StarMeasures
import com.example.data.computeAchievementStats
import com.example.data.local.DailySessionEntity
import com.example.data.local.GameProgressEntity
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.model.AgeBand
import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import com.example.model.UserSettings
import com.example.ui.screens.TuScreen
import com.example.ui.screens.HomeScreen
import com.example.ui.screens.ProgressScreen
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.NeuroVidaViewModel
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

/**
 * «Cosecha de palabras» (`cosecha`, retirada el 10-oct-2026 por decisión de Ricardo, Tarea 80): no aparece en Juegos, en el camino diario, en Hoy, en Avance ni en las medidas, pero NADA se borra: sus partidas y su avance siguen en la base, su medida (`harvest`) sigue guardada
 * aunque ya no se lea, y una partida vieja suya sigue contando en la racha y en el total. Lenguaje queda con 4 juegos. Datos viejos de verdad en Room y en las preferencias.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w412dp-h915dp", sdk = [36])
class RetiredCosechaTest {
  @get:Rule val composeTestRule = createComposeRule()

  private lateinit var app: Application
  private val old = "cosecha"
  private val day = 24L * 60 * 60 * 1000

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
  }

  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  private fun newViewModel(): NeuroVidaViewModel = NeuroVidaViewModel(app).also { viewModels.add(it) }

  @After
  fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())      // antes de soltar la base: ver TestSupport.release
    viewModels.clear()
    TestSupport.resetDatabase()
  }

  private fun db() = NeuroVidaDatabase.getDatabase(app)

  private fun today() = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date())

  /** Un teléfono con lo que dejó Cosecha: 3 partidas, su avance, una medida `harvest` y un camino de HOY que la nombra. */
  private fun seedOldData() = runBlocking {
    val now = System.currentTimeMillis()
    db().userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ana").toEntity())
    db().gameProgressDao().insertAll(GameRegistry.allGames.map { GameProgressEntity(gameId = it.id, currentLevel = 1) })
    db().gameProgressDao().insertOrUpdate(GameProgressEntity(gameId = old, currentLevel = 4, eloRating = 900, ddaRating = 0.7f, totalGamesPlayed = 3))
    for (i in 0..2) {
      db().gameResultDao().insert(
        GamePlayResult(gameId = old, score = 80, correctAnswers = 10, totalTrials = 12, timed = false, level = 4, timestamp = now - (i + 1) * day, endRating = 0.7f).toEntity()
      )
    }
    db().dailySessionDao().insertOrUpdate(DailySessionEntity(dateKey = today(), gameIdsRaw = "$old,calculo,stroop", completedCount = 1, scoresRaw = ""))
    val points = (0..3).map { MeasurePoint(now - (4 - it) * day, "harvest", 40f + it * 5f) }
    app.getSharedPreferences("star_measures", android.content.Context.MODE_PRIVATE).edit().putString("points", StarMeasures.encode(points)).commit()
  }

  @Test
  fun `el registro queda con 17 juegos, Lenguaje con 4, y el id retirado reservado`() {
    assertNull(GameRegistry.getById(old))
    assertFalse(GameRegistry.allGames.any { it.id == old })
    assertTrue(GameRegistry.isRetired(old))
    assertEquals("LENGUAJE", GameRegistry.retiredDomains[old]!!.name)
    assertEquals(17, GameRegistry.allGames.size)
    assertEquals(setOf("anagramas", "meteoros", "disparate", "intrusa"), GameRegistry.allGames.filter { it.domain.name == "LENGUAJE" }.map { it.id }.toSet())
    assertEquals("las 4 áreas siguen completas: 4 + 5 + 4 + 4", mapOf("MEMORIA" to 4, "ATENCION" to 5, "RAZONAMIENTO" to 4, "LENGUAJE" to 4),
      GameRegistry.allGames.groupingBy { it.domain.name }.eachCount())
    assertNull("ya no hay medida de Cosecha", StarMeasures.defForGame(old))
    assertNull(StarMeasures.def("harvest"))
    assertTrue("todos los juegos de la app tienen tutorial", GameRegistry.allGames.all { it.id in com.example.bridge.UnityGameLauncher.TUTORIAL_GAMES })
    assertFalse("y la retirada no lo pide", old in com.example.bridge.UnityGameLauncher.TUTORIAL_GAMES)
  }

  @Test
  fun `las partidas de Cosecha cuentan para la racha y el total pero no para los logros de juegos`() {
    val now = System.currentTimeMillis()
    fun played(id: String, daysAgo: Int) = GamePlayResult(gameId = id, score = 80, correctAnswers = 10, totalTrials = 12, timed = true, level = 4, timestamp = now - daysAgo * day)
    val history = listOf(played(old, 1), played(old, 2), played(old, 3), played("calculo", 5))
    val stats = computeAchievementStats(history, mapOf(old to 5000, "calculo" to 120))
    assertEquals(4, stats.totalGames)
    assertEquals("el hueco de hace 4 días se perdona (un día de descanso por semana): cuenta 1, 2, 3 y 5", 4, stats.bestStreak)
    assertEquals(1, stats.distinctGames)
    assertEquals(120, stats.bestGameRating)
  }

  @Test
  fun `el avance de Lenguaje se calcula con sus 4 juegos y no cuenta a Cosecha`() {
    val now = 100 * day
    val ids = GameRegistry.allGames.filter { it.domain.name == "LENGUAJE" }.map { it.id }
    val current = mapOf("anagramas" to 0.40f, "meteoros" to 0.50f, "disparate" to 0.60f, "intrusa" to 0.30f, old to 0.95f)
    val s = AreaProgress.status("LENGUAJE", ids, current, listOf(ProgressPoint(old, now - 2 * day, 0.95f), ProgressPoint("anagramas", now - 20 * day, 0.35f)), now)
    assertEquals("promedio de los 4 que quedan: 0,45", 0.45f, s.value!!, 0.0001f)
    assertEquals(45, s.points)
  }

  @Test
  fun `el historial se conserva, no se muestra y la medida vieja no se lee`() {
    seedOldData()
    val vm = newViewModel()
    TestSupport.awaitUntil(message = "Las partidas viejas no se cargaron") { vm.gameHistory.value.isNotEmpty() }
    // los datos siguen ahí: nada se borra
    assertEquals(3, runBlocking { db().gameResultDao().getAllResultsSync().count { it.gameId == old } })
    assertNotNull(runBlocking { db().gameProgressDao().getProgressForGameSync(old) })
    assertTrue("la medida guardada sigue ahí (va en el respaldo)", app.getSharedPreferences("star_measures", android.content.Context.MODE_PRIVATE).getString("points", "")!!.contains("harvest"))
    // pero no aparece en lo que muestra la app
    assertEquals(17, vm.gameRanks.value.size)
    assertTrue(vm.gameRanks.value.none { it.gameId == old })
    assertEquals(17, vm.gameLevelsForProgress.value.size)
    assertFalse(vm.gameLevelsForProgress.value.containsKey(old))
    assertTrue("sin medidas de Cosecha en Avance", StarMeasures.discover(vm.starMeasures.value, System.currentTimeMillis()) == null)
    // no se puede abrir, ni desde Juegos ni desde las herramientas
    vm.launchGame(old)
    assertNull(vm.activeGame.value)
    vm.playFromHome(old, PlayMode.A_TU_MEDIDA)
    assertNull(vm.activeGame.value)
  }

  @Test
  fun `un camino de hoy guardado con Cosecha se cambia por otro juego de Lenguaje y conserva el avance`() {
    seedOldData()
    val vm = newViewModel()
    // El estado inicial del repositorio no nombra el juego retirado y tiene 0 completados: hay que esperar al camino GUARDADO (1 completado).
    TestSupport.awaitUntil(message = "El camino de hoy no se corrigió") { vm.dailySession.value.completedCount == 1 && vm.dailySession.value.gameIds.none { it == old } && vm.dailySession.value.gameIds.size == 3 }
    val s = vm.dailySession.value
    assertEquals(1, s.completedCount)
    assertEquals("calculo", s.gameIds[1])
    assertEquals("stroop", s.gameIds[2])
    assertEquals("LENGUAJE", GameRegistry.getById(s.gameIds[0])!!.domain.name)
    assertEquals(3, s.gameIds.toSet().size)
    val saved = runBlocking { db().dailySessionDao().getDailySessionSync(today()) }!!
    assertFalse(saved.gameIdsRaw.contains(old))
  }

  @Test
  fun `una partida de Cosecha en pausa no rompe nada, no se ofrece ni se retoma y la sesion de hoy se puede empezar`() {
    // quedó en pausa dentro de Unity antes de actualizar la app
    app.getSharedPreferences("paused_game", android.content.Context.MODE_PRIVATE).edit()
      .putString("gameId", old).putString("launchId", "L-vieja").putInt("level", 3).putBoolean("timed", true).putBoolean("daily", false).commit()
    seedOldData()
    val vm = newViewModel()
    assertNull("Hoy no ofrece «Seguir partida» de un juego que ya no existe", vm.pausedGameId.value)
    TestSupport.awaitUntil { vm.dailySession.value.gameIds.none { it == old } && vm.dailySession.value.gameIds.size == 3 }
    vm.launchGame(old)
    assertNull("no se retoma", vm.activeGame.value)
    vm.startDailySession()
    assertNotNull(vm.activeGame.value)
    assertTrue(vm.activeGame.value!!.gameDef.id != old)
  }

  @Test
  fun `los caminos nuevos nunca traen Cosecha y la sesion de hoy se puede empezar`() {
    seedOldData()
    val vm = newViewModel()
    TestSupport.awaitUntil { vm.dailySession.value.gameIds.none { it == old } && vm.dailySession.value.gameIds.size == 3 }
    vm.startDailySession()
    assertNotNull(vm.activeGame.value)
    assertTrue(vm.activeGame.value!!.gameDef.id != old)
  }

  @Test
  fun `Hoy, Tú y Avance se abren sin errores y no nombran Cosecha`() {
    seedOldData()
    val vm = newViewModel()
    TestSupport.awaitUntil { vm.gameHistory.value.isNotEmpty() && vm.dailySession.value.gameIds.none { it == old } }
    composeTestRule.mainClock.autoAdvance = false
    val screen = androidx.compose.runtime.mutableIntStateOf(0)
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp)) {
          androidx.compose.runtime.CompositionLocalProvider(com.example.ui.components.LocalTabSwipeLock provides androidx.compose.runtime.remember { androidx.compose.runtime.mutableIntStateOf(0) }) {
            when (screen.intValue) {
              0 -> HomeScreen(viewModel = vm)
              1 -> TuScreen(viewModel = vm)
              else -> ProgressScreen(viewModel = vm)
            }
          }
        }
      }
    }
    for (i in 0..2) {
      screen.intValue = i
      composeTestRule.mainClock.advanceTimeBy(500)
      composeTestRule.waitForIdle()
      assertEquals("«Cosecha» no debe verse en la pantalla $i", 0, composeTestRule.onAllNodesWithText("Cosecha", substring = true).fetchSemanticsNodes().size)
    }
  }

  @Test
  fun `un resultado de Cosecha que llegara de Unity ya no se lee ni se guarda`() {
    val r = com.example.bridge.NativeReceiver.parse(
      """{"user_id":"u1","game_id":"cosecha","session_metrics":{"correct_trials":6,"total_trials":8,"calculated_score":70,"average_response_time_ms":0,"level":3,"timed":true,"harv_words":30,"harv_common_found":20,"harv_common_total":40}}"""
    )
    assertNull("el juego está retirado: no tiene lector de resultados", r)
  }
}
