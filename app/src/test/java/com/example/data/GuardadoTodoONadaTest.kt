package com.example.data

import android.app.Application
import androidx.test.core.app.ApplicationProvider
import com.example.TestSupport
import com.example.bridge.UnityGameLauncher
import com.example.bridge.UnityResultInbox
import com.example.data.local.NeuroVidaDatabase
import com.example.model.AgeBand
import com.example.model.DomainType
import com.example.model.GamePlayResult
import com.example.viewmodel.NeuroVidaViewModel
import java.util.UUID
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.async
import kotlinx.coroutines.awaitAll
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotEquals
import org.junit.Assert.assertTrue
import org.junit.Assert.fail
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * Guardar una partida es TODO O NADA y no se duplica (Tarea 192; `docs/guardado-de-una-partida.md`): lo de Room va en una transacción (un fallo adentro no deja nada, ni en Room ni en las preferencias ni en memoria), el id de la fila
 * es el del lanzamiento de Unity (la doble entrega, también simultánea, guarda una sola vez) y la marca de la bandeja se escribe DESPUÉS (un reinicio entre la transacción y la marca no duplica). Incluye el Contrarreloj.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class GuardadoTodoONadaTest {
  private lateinit var app: Application
  private val repos = mutableListOf<NeuroVidaRepository>()
  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  @Before fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    app.getSharedPreferences("unity_results", android.content.Context.MODE_PRIVATE).edit().clear().commit()
    UnityResultInbox.forgetMemory()
  }

  @After fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())
    viewModels.clear()
    repos.forEach { TestSupport.release(it) }
    repos.clear()
    TestSupport.resetDatabase()
  }

  private fun repo(): NeuroVidaRepository = NeuroVidaRepository(app).also { repos.add(it) }
  private fun db() = NeuroVidaDatabase.getDatabase(app)

  /** Una partida que mueve de todo: puntos, avance del juego (endRating), una medida estrella (Rescate) y la ruta. */
  private fun played(id: String = UUID.randomUUID().toString(), game: String = "radar", score: Int = 80) =
    GamePlayResult(id = id, gameId = game, score = score, correctAnswers = 8, totalTrials = 10, timed = false, level = 2, endRating = 0.6f, glanceMs = 300)

  private fun trial(id: String, game: String = "stroop") = TimeTrialResult(id = id, gameId = game, day = "2026-10-30", trackVersion = 2, hits = 30, errors = 2, level = 3)

  private fun rows() = runBlocking { db().gameResultDao().getAllResultsSync() }
  private fun trialRows() = runBlocking { db().timeTrialResultDao().getAllSync() }

  // ---------------------------------------------------------------- todo o nada

  @Test
  fun `un fallo forzado dentro de la transaccion no deja nada guardado ni en Room ni en las preferencias ni en memoria`() {
    val r = repo()
    runBlocking { r.recordGameResult(played(game = "radar")) }                 // una partida de antes, para comparar contra algo que no está vacío
    val progressBefore = runBlocking { db().gameProgressDao().getAllProgressSync() }
    val skillBefore = r.skill.value
    val logBefore = r.progressLog.value
    val measuresBefore = r.starMeasures.value
    val xpBefore = runBlocking { DomainType.entries.map { db().domainMasteryDao().getForDomain(it.name)?.xp } }
    val sessionBefore = r.dailySession.value
    assertEquals(1, rows().size)

    r.beforeCommitForTest = { throw IllegalStateException("fallo forzado") }
    try {
      runBlocking { r.recordGameResult(played(game = "radar")) }
      fail("el fallo forzado debía llegar hasta quien guarda")
    } catch (e: IllegalStateException) {
      assertEquals("fallo forzado", e.message)
    }
    r.beforeCommitForTest = null

    assertEquals("la partida no quedó", 1, rows().size)
    assertEquals("el avance del juego, intacto", progressBefore, runBlocking { db().gameProgressDao().getAllProgressSync() })
    assertEquals("la maestría, intacta", xpBefore, runBlocking { DomainType.entries.map { db().domainMasteryDao().getForDomain(it.name)?.xp } })
    assertEquals("la ruta de hoy, intacta", sessionBefore, r.dailySession.value)
    assertEquals("el avance (Skill) no cuenta una partida que no existe", skillBefore, r.skill.value)
    assertEquals("la historia del avance, intacta", logBefore, r.progressLog.value)
    assertEquals("las medidas, intactas", measuresBefore, r.starMeasures.value)

    runBlocking { r.recordGameResult(played(game = "radar")) }                 // y el siguiente guardado sí funciona
    assertEquals(2, rows().size)
  }

  // ---------------------------------------------------------------- idempotencia

  @Test
  fun `la doble entrega guarda una sola vez y la segunda dice que ya estaba guardada`() {
    val r = repo()
    val id = UUID.randomUUID().toString()
    val first = runBlocking { r.recordGameResult(played(id)) }
    val second = runBlocking { r.recordGameResult(played(id)) }
    assertFalse(first.alreadySaved)
    assertTrue(second.alreadySaved)
    assertEquals(1, rows().size)
    assertEquals("el avance del juego cuenta UNA partida", 1, runBlocking { db().gameProgressDao().getProgressForGameSync("radar") }!!.totalGamesPlayed)
    assertEquals("la historia del avance, un punto", 1, r.progressLog.value.size)
    assertEquals("las medidas, un punto", 1, r.starMeasures.value.size)
  }

  @Test
  fun `la doble entrega SIMULTANEA (la vuelta con el Intent y el broadcast a la vez) guarda una sola vez`() {
    val r = repo()
    val id = UUID.randomUUID().toString()
    val outcomes = runBlocking { (1..4).map { async(Dispatchers.Default) { r.recordGameResult(played(id)) } }.awaitAll() }
    assertEquals(1, rows().size)
    assertEquals("exactamente UNA entrega guarda", 1, outcomes.count { !it.alreadySaved })
    assertEquals(1, runBlocking { db().gameProgressDao().getProgressForGameSync("radar") }!!.totalGamesPlayed)
  }

  @Test
  fun `un reinicio del proceso despues de la transaccion y antes de la marca no duplica`() {
    val id = UUID.randomUUID().toString()
    val before = repo()
    runBlocking { before.recordGameResult(played(id)) }                         // la transacción se confirmó...
    assertTrue("...y la marca de la bandeja todavía NO se escribió (el proceso murió antes)", UnityResultInbox.claim(id))
    UnityResultInbox.forgetMemory()                                             // el reinicio: se pierde la reserva en memoria
    val after = repo()                                                          // un repositorio nuevo, como en un proceso nuevo, sobre la MISMA base
    assertTrue("la entrega se acepta otra vez (no hay marca)", UnityResultInbox.claim(id))
    val outcome = runBlocking { after.recordGameResult(played(id)) }
    assertTrue("pero la base la reconoce y no guarda nada más", outcome.alreadySaved)
    assertEquals(1, rows().size)
    UnityResultInbox.markHandled(id)
    UnityResultInbox.forgetMemory()
    assertFalse("con la marca escrita, el filtro rápido la corta", UnityResultInbox.claim(id))
  }

  @Test
  fun `si el guardado falla se suelta la reserva y otra entrega puede reintentar`() {
    val id = UUID.randomUUID().toString()
    assertTrue(UnityResultInbox.claim(id))
    assertFalse("mientras se guarda, el otro camino no entra", UnityResultInbox.claim(id))
    UnityResultInbox.release(id)
    assertTrue("falló: la reserva se soltó SIN anotar la marca", UnityResultInbox.claim(id))
    UnityResultInbox.markHandled(id)
    assertFalse(UnityResultInbox.claim(id))
  }

  // ---------------------------------------------------------------- Contrarreloj

  @Test
  fun `el Contrarreloj tambien es idempotente (doble entrega, simultanea) y todo o nada`() {
    val r = repo()
    val id = UUID.randomUUID().toString()
    runBlocking { r.recordTimeTrial(trial(id)) }
    runBlocking { r.recordTimeTrial(trial(id)) }
    assertEquals(1, trialRows().size)
    val other = UUID.randomUUID().toString()
    runBlocking { (1..4).map { async(Dispatchers.Default) { r.recordTimeTrial(trial(other)) } }.awaitAll() }
    assertEquals("la doble entrega simultánea guarda una sola vez", 2, trialRows().size)
    r.beforeCommitForTest = { throw IllegalStateException("fallo forzado") }
    try { runBlocking { r.recordTimeTrial(trial(UUID.randomUUID().toString())) }; fail("debía fallar") } catch (e: IllegalStateException) { }
    r.beforeCommitForTest = null
    assertEquals("un fallo dentro de la transacción no deja la fila", 2, trialRows().size)
  }

  // ---------------------------------------------------------------- el id del lanzamiento nunca se repite entre dos partidas

  @Test
  fun `el id de lanzamiento es un UUID de verdad (no un contador) y cada lanzamiento trae uno nuevo`() {
    val uuid = Regex("^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$")
    val ids = (1..200).map {
      UnityGameLauncher.buildGameIntent(app, "radar", "u1", level = 1, baseIntensity = 0, timed = false, ageBand = AgeBand.ADULT).getStringExtra(UnityGameLauncher.EXTRA_LAUNCH_ID)!!
    }
    assertEquals("ningún id se repite", ids.size, ids.toSet().size)
    ids.forEach { assertTrue("«$it» es un UUID aleatorio (versión 4)", uuid.matches(it)) }
    // la sesión (`key(sessionToken)` en MainActivity y FirstFlightHost: cada sesión es su propio grupo de composición y genera su propio id)
    val sessions = (1..50).map { com.example.viewmodel.ActiveGameSession(com.example.model.GameRegistry.getById("radar")!!, 1, false, false, 0) }
    assertEquals(50, sessions.map { it.sessionToken }.toSet().size)
  }

  @Test
  fun `Jugar de nuevo y Otra vez son lanzamientos nuevos con su propio id y las dos partidas se guardan`() {
    val r = repo()
    val first = UUID.randomUUID().toString()
    val again = UUID.randomUUID().toString()                                    // «Jugar de nuevo»: launchGame sin pausa → sesión nueva → id nuevo
    runBlocking { r.recordGameResult(played(first)); r.recordGameResult(played(again)) }
    assertEquals(2, rows().size)
    runBlocking { r.recordTimeTrial(trial(first)); r.recordTimeTrial(trial(again)) }      // «Otra vez» del Contrarreloj: lo mismo
    assertEquals(2, trialRows().size)
  }

  @Test
  fun `Reiniciar desde la pausa conserva el id pero sale un solo resultado porque la partida abandonada nunca informa`() {
    // Unity reinicia dentro de su proceso con el MISMO Intent (mismo id): la escena se recarga y la partida abandonada desaparece sin llamar a onGameFinished (solo la que TERMINA informa);
    // y tras terminar, Atrás vuelve a la app (no abre la pausa), así que no hay un «Reiniciar» posterior a un resultado. Resultado: un solo informe por id.
    val r = repo()
    val id = UUID.randomUUID().toString()
    runBlocking { r.recordGameResult(played(id, score = 60)) }                  // la partida reiniciada, al terminar
    runBlocking { r.recordGameResult(played(id, score = 60)) }                  // y su reentrega (broadcast + Intent)
    assertEquals(1, rows().size)
  }

  @Test
  fun `retomar una partida en pausa usa el mismo id y su resultado se guarda una sola vez`() {
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    vm.launchGame("radar")
    val launchId = UUID.randomUUID().toString()
    vm.onUnityLaunched(launchId)
    vm.onReturnedFromGame(launchId, null, paused = true)                        // «Salir» desde la pausa: queda en pausa dentro de Unity
    vm.launchGame("radar")                                                      // se vuelve a abrir el mismo juego, mismo modo
    assertEquals("se retoma con el MISMO id", launchId, vm.activeGame.value?.resumeLaunchId)
    vm.onUnityResult(played(game = "radar"), launchId)
    vm.onUnityResult(played(game = "radar"), launchId)                         // y llega otra vez
    TestSupport.awaitUntil(message = "La partida no se guardó") { rows().size == 1 }
    Thread.sleep(300)
    assertEquals(1, rows().size)
    assertEquals(launchId, rows().single().id)
  }

  // ---------------------------------------------------------------- respaldo restaurado

  @Test
  fun `restaurar un respaldo con la base vieja no pierde partidas nuevas ni choca con las filas restauradas`() {
    val r = repo()
    // la base restaurada trae partidas de antes, con ids que NO son de lanzamiento (UUID aleatorios del guardado viejo, o «r1», «r2» de las pruebas)
    runBlocking {
      db().gameResultDao().insertAll(
        listOf("r1", "r2", UUID.randomUUID().toString(), UUID.randomUUID().toString()).mapIndexed { i, id ->
          com.example.data.local.GameResultEntity(id, "radar", 70, 7, 10, false, 1, 1_000L + i)
        }
      )
    }
    val newOnes = (1..5).map { UUID.randomUUID().toString() }
    runBlocking { newOnes.forEach { r.recordGameResult(played(it)) } }
    assertEquals("las 4 restauradas y las 5 nuevas", 9, rows().size)
    // la misma partida que ya vino en el respaldo (mismo id de lanzamiento) NO se duplica: es la misma partida
    val restoredId = newOnes.first()
    val again = runBlocking { r.recordGameResult(played(restoredId)) }
    assertTrue(again.alreadySaved)
    assertEquals(9, rows().size)
  }

  @Test
  fun `la bandeja del ViewModel anota el lanzamiento DESPUES de guardar y una segunda entrega no hace nada`() {
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    val id = UUID.randomUUID().toString()
    assertTrue(UnityResultInbox.claim(id))                                      // el camino que llega primero la reserva
    vm.onUnityResult(played(game = "radar"), id)
    TestSupport.awaitUntil(message = "La partida no se guardó") { rows().size == 1 }
    TestSupport.awaitUntil(message = "La marca no se escribió") { app.getSharedPreferences("unity_results", android.content.Context.MODE_PRIVATE).getString("handled_launch_ids", "").orEmpty().contains(id) }
    UnityResultInbox.forgetMemory()
    assertFalse("el otro camino ya no entra", UnityResultInbox.claim(id))
    assertEquals(id, rows().single().id)
    assertNotEquals(0, rows().size)
  }
}
