package com.example.flow

import android.app.Application
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onAllNodesWithTag
import androidx.compose.ui.test.onAllNodesWithText
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.TestSupport
import com.example.data.MeasurePoint
import com.example.data.PlayMode
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
import com.example.ui.screens.GamesLibraryScreen
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
 * «Bitácora de Misión» (`bitacora`, retirada el 5-oct-2026 por pedido de Ricardo): no aparece en Juegos, en el camino diario, en Hoy (ya no hay línea de «Misión del día»), en Avance ni en las medidas,
 * pero NADA se borra: sus partidas y su avance siguen en la base, su misión (`mission_log`) y su medida (`recall`) siguen guardadas aunque ya no se lean. Datos viejos de verdad en Room y en las preferencias.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w412dp-h915dp", sdk = [36])
class RetiredBitacoraTest {
  @get:Rule val composeTestRule = createComposeRule()
  @get:Rule val retryOnDbFlake = TestSupport.retryOnDbFlake()

  private lateinit var app: Application
  private val old = "bitacora"
  private val day = 24L * 60 * 60 * 1000

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    runCatching {
      androidx.work.WorkManager.initialize(app, androidx.work.Configuration.Builder().setExecutor(java.util.concurrent.Executor { it.run() }).build())
    }
  }

  @After
  fun tearDown() {
    TestSupport.resetDatabase()
  }

  private fun db() = NeuroVidaDatabase.getDatabase(app)

  private fun today() = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date())

  /** Una base que quedó atada a la prueba anterior (su ViewModel sigue vivo un momento y la reabre) da «unable to open database file»: se suelta y se reintenta (igual que en [RetiredGameTest]). */
  private fun <T> withFreshDb(block: () -> T): T {
    repeat(3) {
      try { return block() } catch (e: android.database.sqlite.SQLiteCantOpenDatabaseException) { TestSupport.resetDatabase(); Thread.sleep(250) }
    }
    return block()
  }

  /** Un teléfono con lo que dejó Bitácora: 3 partidas, su avance, su misión de hoy a medias, una medida `recall` y un camino de HOY que la nombra. */
  private fun seedOldData() = withFreshDb { seedOldDataOnce() }

  private fun seedOldDataOnce() = runBlocking {
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
    app.getSharedPreferences("mission_log", android.content.Context.MODE_PRIVATE).edit()
      .putString("date", today()).putInt("seed", 4321).putLong("encodedAt", now - 60_000L).putInt("archivedTotal", 17).commit()
    val points = (0..3).map { MeasurePoint(now - (4 - it) * day, "recall", 40f + it * 10f) }
    app.getSharedPreferences("star_measures", android.content.Context.MODE_PRIVATE).edit().putString("points", StarMeasures.encode(points)).commit()
  }

  @Test
  fun `el registro queda con 18 juegos, Memoria con 4, y el id retirado reservado`() {
    assertNull(GameRegistry.getById(old))
    assertFalse(GameRegistry.allGames.any { it.id == old })
    assertTrue(GameRegistry.isRetired(old))
    assertEquals("MEMORIA", GameRegistry.retiredDomains[old]!!.name)
    assertEquals(18, GameRegistry.allGames.size)
    assertEquals(setOf("parejas", "secuencia", "rumbo", "correo"), GameRegistry.allGames.filter { it.domain.name == "MEMORIA" }.map { it.id }.toSet())
    assertNull("ya no hay medida de Bitácora", StarMeasures.defForGame(old))
    assertNull(StarMeasures.def("recall"))
  }

  @Test
  fun `las partidas de Bitacora cuentan para la racha y el total pero no para los logros de juegos`() {
    val now = System.currentTimeMillis()
    fun played(id: String, daysAgo: Int) = GamePlayResult(gameId = id, score = 80, correctAnswers = 10, totalTrials = 12, timed = true, level = 4, timestamp = now - daysAgo * day)
    val history = listOf(played(old, 1), played(old, 2), played(old, 3), played("calculo", 5))
    val stats = computeAchievementStats(history, mapOf(old to 5000, "calculo" to 120))
    assertEquals(4, stats.totalGames)
    assertEquals(3, stats.bestStreak)
    assertEquals(1, stats.distinctGames)
    assertEquals(120, stats.bestGameRating)
  }

  @Test
  fun `el historial y la mision se conservan, no se muestran y la medida vieja no se lee`() {
    seedOldData()
    val vm = NeuroVidaViewModel(app)
    TestSupport.awaitUntil(message = "Las partidas viejas no se cargaron") { vm.gameHistory.value.isNotEmpty() }
    // los datos siguen ahí: nada se borra
    assertEquals(3, runBlocking { db().gameResultDao().getAllResultsSync().count { it.gameId == old } })
    assertNotNull(runBlocking { db().gameProgressDao().getProgressForGameSync(old) })
    val mission = app.getSharedPreferences("mission_log", android.content.Context.MODE_PRIVATE)
    assertEquals("la misión guardada sigue ahí (va en el respaldo)", 17, mission.getInt("archivedTotal", -1))
    assertTrue(app.getSharedPreferences("star_measures", android.content.Context.MODE_PRIVATE).getString("points", "")!!.contains("recall"))
    // pero no aparece en lo que muestra la app
    assertEquals(18, vm.gameRanks.value.size)
    assertTrue(vm.gameRanks.value.none { it.gameId == old })
    assertEquals(18, vm.gameLevelsForProgress.value.size)
    assertFalse(vm.gameLevelsForProgress.value.containsKey(old))
    assertTrue("sin medidas de Bitácora en Avance", StarMeasures.discover(vm.starMeasures.value, System.currentTimeMillis()) == null)
    // no se puede abrir, ni desde Juegos ni desde las herramientas
    vm.launchGame(old)
    assertNull(vm.activeGame.value)
    vm.playFromLibrary(old, PlayMode.A_TU_MEDIDA)
    assertNull(vm.activeGame.value)
  }

  @Test
  fun `un camino de hoy guardado con Bitacora se cambia por otro juego de Memoria y conserva el avance`() {
    seedOldData()
    val vm = NeuroVidaViewModel(app)
    TestSupport.awaitUntil(message = "El camino de hoy no se corrigió") { vm.dailySession.value.gameIds.none { it == old } && vm.dailySession.value.gameIds.size == 3 }
    val s = vm.dailySession.value
    assertEquals(1, s.completedCount)
    assertEquals("calculo", s.gameIds[1])
    assertEquals("stroop", s.gameIds[2])
    assertEquals("MEMORIA", GameRegistry.getById(s.gameIds[0])!!.domain.name)
    assertEquals(3, s.gameIds.toSet().size)
    val saved = runBlocking { db().dailySessionDao().getDailySessionSync(today()) }!!
    assertFalse(saved.gameIdsRaw.contains(old))
  }

  @Test
  fun `los caminos nuevos nunca traen Bitacora y la sesion de hoy se puede empezar`() {
    seedOldData()
    val vm = NeuroVidaViewModel(app)
    TestSupport.awaitUntil { vm.dailySession.value.gameIds.none { it == old } && vm.dailySession.value.gameIds.size == 3 }
    vm.startDailySession()
    assertNotNull(vm.activeGame.value)
    assertTrue(vm.activeGame.value!!.gameDef.id != old)
  }

  @Test
  fun `Hoy, Juegos y Avance se abren sin errores, no nombran Bitacora y Hoy ya no tiene la linea de la mision`() {
    seedOldData()
    val vm = NeuroVidaViewModel(app)
    TestSupport.awaitUntil { vm.gameHistory.value.isNotEmpty() && vm.dailySession.value.gameIds.none { it == old } }
    composeTestRule.mainClock.autoAdvance = false
    val screen = androidx.compose.runtime.mutableIntStateOf(0)
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp)) {
          androidx.compose.runtime.CompositionLocalProvider(com.example.ui.components.LocalTabSwipeLock provides androidx.compose.runtime.remember { androidx.compose.runtime.mutableIntStateOf(0) }) {
            when (screen.intValue) {
              0 -> HomeScreen(viewModel = vm, onNavigateToGames = {})
              1 -> GamesLibraryScreen(viewModel = vm)
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
      assertEquals("«Bitácora» no debe verse en la pantalla $i", 0, composeTestRule.onAllNodesWithText("Bitácora", substring = true).fetchSemanticsNodes().size)
      assertEquals("la línea de la misión ya no existe (pantalla $i)", 0, composeTestRule.onAllNodesWithTag("mission_line").fetchSemanticsNodes().size)
    }
  }
}
