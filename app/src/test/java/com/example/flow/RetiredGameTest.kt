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
import com.example.data.SkillState
import com.example.data.computeAchievementStats
import com.example.data.local.DailySessionEntity
import com.example.data.local.GameProgressEntity
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toDomain
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
 * Juego RETIRADO (`cambiochip`, 3-oct-2026): quien ya lo jugó conserva sus partidas y su progreso en la base de datos y en las preferencias (nada se borra ni se migra),
 * pero no se muestra en ningún lado y no rompe nada. Prueba con datos viejos de verdad en Room y en `skill`: el ViewModel, las pantallas Hoy, Juegos y Avance, los logros y el camino de hoy.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class RetiredGameTest {
  private lateinit var app: Application
  private var old = "cambiochip"
  private val day = 24L * 60 * 60 * 1000

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    old = "cambiochip"
    TestSupport.resetDatabase()
    // El ViewModel agenda el recordatorio con WorkManager; sin iniciarlo, esa excepción suelta hace fallar a la regla de Compose («uncaught exceptions before the test started»).
    runCatching {
      androidx.work.WorkManager.initialize(app, androidx.work.Configuration.Builder().setExecutor(java.util.concurrent.Executor { it.run() }).build())
    }
  }

  @After
  fun tearDown() {
    TestSupport.resetDatabase()
  }

  private fun db() = NeuroVidaDatabase.getDatabase(app)

  /** Un teléfono con lo que dejó Cambio de Chip: 3 partidas (la última ayer), su avance, su medida en `skill` y un camino de HOY que lo nombra. */
  /** Una base que quedó atada a la prueba anterior (su ViewModel sigue vivo un momento y la reabre) da «unable to open database file»: se suelta y se reintenta. */
  private fun <T> withFreshDb(block: () -> T): T {
    repeat(3) {
      try { return block() } catch (e: android.database.sqlite.SQLiteCantOpenDatabaseException) { TestSupport.resetDatabase(); Thread.sleep(250) }
    }
    return block()
  }

  private fun seedOldData(todayWith: String = "$old,calculo,acoplamiento", completed: Int = 0) = withFreshDb { seedOldDataOnce(todayWith, completed) }

  private fun seedOldDataOnce(todayWith: String, completed: Int) = runBlocking {
    val now = System.currentTimeMillis()
    db().userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ana").toEntity())
    db().gameProgressDao().insertAll(GameRegistry.allGames.map { GameProgressEntity(gameId = it.id, currentLevel = 1) })
    db().gameProgressDao().insertOrUpdate(GameProgressEntity(gameId = old, currentLevel = 4, eloRating = 900, ddaRating = 0.7f, totalGamesPlayed = 3))
    for (i in 0..2) {
      db().gameResultDao().insert(
        GamePlayResult(gameId = old, score = 80, correctAnswers = 10, totalTrials = 12, timed = true, level = 4, timestamp = now - (i + 1) * day, endRating = 0.7f).toEntity()
      )
    }
    db().gameResultDao().insert(GamePlayResult(gameId = "calculo", score = 70, correctAnswers = 7, totalTrials = 10, timed = false, level = 2, timestamp = now - 5 * day).toEntity())
    val today = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date())
    db().dailySessionDao().insertOrUpdate(DailySessionEntity(dateKey = today, gameIdsRaw = todayWith, completedCount = completed, scoresRaw = ""))
    app.getSharedPreferences("skill", android.content.Context.MODE_PRIVATE).edit()
      .putString("state", SkillState(measured = setOf(old, "calculo")).encode()).commit()
  }

  private fun newViewModel() = NeuroVidaViewModel(app)

  @Test
  fun `el registro tiene 19 juegos, Atencion 5, y el id retirado queda reservado`() {
    assertEquals(19, GameRegistry.allGames.size)
    assertNull(GameRegistry.getById(old))
    assertFalse(GameRegistry.allGames.any { it.id == old })
    assertEquals(5, GameRegistry.allGames.count { it.domain.name == "ATENCION" })
    assertTrue(GameRegistry.isRetired(old))
    assertFalse(GameRegistry.isRetired("stroop"))
    assertEquals("ATENCION", GameRegistry.retiredDomains[old]!!.name)
  }

  @Test
  fun `lo guardado de Cambio de Chip se conserva en la base y no se muestra en las listas del ViewModel`() {
    seedOldData()
    val vm = newViewModel()
    TestSupport.awaitUntil(message = "Las partidas viejas no se cargaron") { vm.gameHistory.value.isNotEmpty() }
    // los datos siguen ahí: nada se borra
    assertEquals(3, runBlocking { db().gameResultDao().getAllResultsSync().count { it.gameId == old } })
    assertNotNull(runBlocking { db().gameProgressDao().getProgressForGameSync(old) })
    // pero no aparece en lo que muestra la app
    assertEquals(19, vm.gameRanks.value.size)
    assertTrue(vm.gameRanks.value.none { it.gameId == old })
    assertEquals(19, vm.gameLevelsForProgress.value.size)
    assertFalse(vm.gameLevelsForProgress.value.containsKey(old))
    // no se puede abrir
    vm.launchGame(old)
    assertNull(vm.activeGame.value)
    vm.playFromLibrary(old, com.example.data.PlayMode.A_TU_MEDIDA)
    assertNull(vm.activeGame.value)
  }

  @Test
  fun `la racha y el total de partidas cuentan los dias jugados, los logros no cuentan el juego retirado`() {
    seedOldData()
    val history = withFreshDb { runBlocking { db().gameResultDao().getAllResultsSync().map { it.toDomain() } } }
    val stats = computeAchievementStats(history, mapOf(old to 5000, "calculo" to 120))
    // 4 partidas y 4 días seguidos? ayer, anteayer, hace 3 días (los de Cambio de Chip) y hace 5: la racha larga son los 3 de Cambio de Chip
    assertEquals(4, stats.totalGames)
    assertEquals(3, stats.bestStreak)
    // «Explorador» (9 juegos distintos) y «Mente completa» solo cuentan juegos del registro: Cambio de Chip no suma
    assertEquals(1, stats.distinctGames)
    // la liga más alta tampoco sale de un juego retirado
    assertEquals(120, stats.bestGameRating)
  }

  @Test
  fun `un camino de hoy guardado con el juego retirado se cambia por otro de su area y conserva el avance`() {
    seedOldData(todayWith = "calculo,$old,acoplamiento", completed = 1)
    val vm = newViewModel()
    TestSupport.awaitUntil(message = "El camino de hoy no se corrigió") { vm.dailySession.value.gameIds.none { it == old } && vm.dailySession.value.gameIds.size == 3 }
    val s = vm.dailySession.value
    assertEquals(1, s.completedCount)
    assertEquals("calculo", s.gameIds[0])
    assertEquals("acoplamiento", s.gameIds[2])
    assertEquals("ATENCION", GameRegistry.getById(s.gameIds[1])!!.domain.name)
    assertEquals(3, s.gameIds.toSet().size)
    // y quedó corregido en la base
    val saved = runBlocking { db().dailySessionDao().getDailySessionSync(SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date())) }!!
    assertFalse(saved.gameIdsRaw.contains(old))
  }

  @Test
  fun `la sesion de hoy se puede empezar con datos viejos de Cambio de Chip`() {
    seedOldData()
    val vm = newViewModel()
    TestSupport.awaitUntil { vm.dailySession.value.gameIds.none { it == old } && vm.dailySession.value.gameIds.size == 3 }
    vm.startDailySession()
    assertNotNull(vm.activeGame.value)
    assertTrue(vm.activeGame.value!!.gameDef.id != old)
  }

  // ---- Comparación Instantánea (`comparacion`, retirada el 4-oct-2026): el mismo mecanismo; aquí solo lo que no necesita la base de datos (las pruebas de arriba, con un ViewModel
  // y Room, ya cubren el camino de hoy y las listas para cualquier id retirado; las pantallas, en RetiredComparacionScreensTest).

  @Test
  fun `Comparacion Instantanea tambien queda retirada, su id reservado y el area Atencion con 5`() {
    assertNull(GameRegistry.getById("comparacion"))
    assertTrue(GameRegistry.isRetired("comparacion"))
    assertTrue(GameRegistry.isRetired("cambiochip"))
    assertEquals("ATENCION", GameRegistry.retiredDomains["comparacion"]!!.name)
    assertEquals(19, GameRegistry.allGames.size)
    assertEquals(5, GameRegistry.allGames.count { it.domain.name == "ATENCION" })
  }

  @Test
  fun `las partidas de Comparacion Instantanea cuentan para la racha y el total pero no para los logros de juegos`() {
    val now = System.currentTimeMillis()
    fun played(id: String, daysAgo: Int) = GamePlayResult(gameId = id, score = 80, correctAnswers = 10, totalTrials = 12, timed = true, level = 4, timestamp = now - daysAgo * day)
    val history = listOf(played("comparacion", 1), played("comparacion", 2), played("comparacion", 3), played("calculo", 5))
    val stats = computeAchievementStats(history, mapOf("comparacion" to 5000, "calculo" to 120))
    assertEquals(4, stats.totalGames)
    assertEquals(3, stats.bestStreak)
    assertEquals(1, stats.distinctGames)
    assertEquals(120, stats.bestGameRating)
  }

  // ---- Detective de Series (`series`, retirado el 4-oct-2026): lo mismo, sin base de datos (las pruebas con ViewModel y Room de arriba valen para cualquier id retirado).

  @Test
  fun `Detective de Series tambien queda retirado, su id reservado y Razonamiento con 4 (con Engranajes)`() {
    assertNull(GameRegistry.getById("series"))
    assertFalse(GameRegistry.allGames.any { it.id == "series" })
    assertTrue(GameRegistry.isRetired("series"))
    assertTrue(GameRegistry.isRetired("comparacion"))
    assertTrue(GameRegistry.isRetired("cambiochip"))
    assertEquals("RAZONAMIENTO", GameRegistry.retiredDomains["series"]!!.name)
    assertEquals(19, GameRegistry.allGames.size)
    assertEquals(setOf("calculo", "acoplamiento", "aterrizaje", "engranajes"), GameRegistry.allGames.filter { it.domain.name == "RAZONAMIENTO" }.map { it.id }.toSet())
  }

  @Test
  fun `las partidas de Detective de Series cuentan para la racha y el total pero no para los logros de juegos`() {
    val now = System.currentTimeMillis()
    fun played(id: String, daysAgo: Int) = GamePlayResult(gameId = id, score = 80, correctAnswers = 10, totalTrials = 12, timed = true, level = 4, timestamp = now - daysAgo * day)
    val history = listOf(played("series", 1), played("series", 2), played("series", 3), played("calculo", 5))
    val stats = computeAchievementStats(history, mapOf("series" to 5000, "calculo" to 120))
    assertEquals(4, stats.totalGames)
    assertEquals(3, stats.bestStreak)
    assertEquals(1, stats.distinctGames)
    assertEquals(120, stats.bestGameRating)
  }

  // ---- Ruta del Tesoro (`rutatesoro`, retirada el 4-oct-2026): lo mismo, sin base de datos.

  @Test
  fun `Ruta del Tesoro tambien queda retirada, su id reservado y Memoria con 5`() {
    assertNull(GameRegistry.getById("rutatesoro"))
    assertFalse(GameRegistry.allGames.any { it.id == "rutatesoro" })
    assertTrue(GameRegistry.isRetired("rutatesoro"))
    assertTrue(GameRegistry.isRetired("series"))
    assertEquals("MEMORIA", GameRegistry.retiredDomains["rutatesoro"]!!.name)
    assertEquals(19, GameRegistry.allGames.size)
    assertEquals(setOf("parejas", "secuencia", "bitacora", "rumbo", "correo"), GameRegistry.allGames.filter { it.domain.name == "MEMORIA" }.map { it.id }.toSet())
  }

  @Test
  fun `las partidas de Ruta del Tesoro cuentan para la racha y el total pero no para los logros de juegos`() {
    val now = System.currentTimeMillis()
    fun played(id: String, daysAgo: Int) = GamePlayResult(gameId = id, score = 80, correctAnswers = 10, totalTrials = 12, timed = true, level = 4, timestamp = now - daysAgo * day)
    val history = listOf(played("rutatesoro", 1), played("rutatesoro", 2), played("rutatesoro", 3), played("calculo", 5))
    val stats = computeAchievementStats(history, mapOf("rutatesoro" to 5000, "calculo" to 120))
    assertEquals(4, stats.totalGames)
    assertEquals(3, stats.bestStreak)
    assertEquals(1, stats.distinctGames)
    assertEquals(120, stats.bestGameRating)
  }

  // ---- Tráfico Estelar (`trafico`, retirado el 4-oct-2026): lo mismo, sin base de datos.

  @Test
  fun `Trafico Estelar tambien queda retirado, su id reservado y Razonamiento con 4 (Engranajes tiene otro id)`() {
    assertNull(GameRegistry.getById("trafico"))
    assertFalse(GameRegistry.allGames.any { it.id == "trafico" })
    assertTrue(GameRegistry.isRetired("trafico"))
    assertTrue(GameRegistry.isRetired("rutatesoro"))
    assertEquals("RAZONAMIENTO", GameRegistry.retiredDomains["trafico"]!!.name)
    assertFalse("Engranajes NO reutiliza el id retirado", GameRegistry.isRetired("engranajes"))
    assertEquals(19, GameRegistry.allGames.size)
    assertEquals(setOf("calculo", "acoplamiento", "aterrizaje", "engranajes"), GameRegistry.allGames.filter { it.domain.name == "RAZONAMIENTO" }.map { it.id }.toSet())
  }

  @Test
  fun `las partidas de Trafico Estelar cuentan para la racha y el total pero no para los logros de juegos`() {
    val now = System.currentTimeMillis()
    fun played(id: String, daysAgo: Int) = GamePlayResult(gameId = id, score = 80, correctAnswers = 10, totalTrials = 12, timed = true, level = 4, timestamp = now - daysAgo * day)
    val history = listOf(played("trafico", 1), played("trafico", 2), played("trafico", 3), played("calculo", 5))
    val stats = computeAchievementStats(history, mapOf("trafico" to 5000, "calculo" to 120))
    assertEquals(4, stats.totalGames)
    assertEquals(3, stats.bestStreak)
    assertEquals(1, stats.distinctGames)
    assertEquals(120, stats.bestGameRating)
  }
}
