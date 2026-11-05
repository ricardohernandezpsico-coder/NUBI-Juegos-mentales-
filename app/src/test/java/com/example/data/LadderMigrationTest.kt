package com.example.data

import android.app.Application
import android.content.Context
import androidx.test.core.app.ApplicationProvider
import com.example.TestSupport
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.async
import kotlinx.coroutines.awaitAll
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * Lo que Room NO puede traducir al alargar la escalera de un juego (Tarea 233; `docs/alargar-dificultad.md` §B.1): las medidas de `star_measures` guardan el rating de cada partida con la escalera VIEJA y la app las lleva a la de ahora UNA sola vez, al arrancar,
 * con la marca de versión de la preferencia `skill` ([SkillState.ladderMarks]). Aquí: se traduce solo lo de Carga exacta, una vez aunque la app se abra dos veces o dos llamadas corran a la vez, una instalación nueva solo recibe sus marcas, con las marcas puestas no se toca nada
 * y «Borrar datos» deja las marcas de ahora (si no, lo que se juegue después se traduciría de más al abrir la app). La base (Room) la prueba `MigrationTest`; el respaldo, `BackupRulesTest`: la marca vive en `skill`, que ya va en el respaldo.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class LadderMigrationTest {
  private lateinit var app: Application
  private val repos = mutableListOf<NeuroVidaRepository>()

  @Before fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    for (name in listOf("star_measures", "skill")) app.getSharedPreferences(name, Context.MODE_PRIVATE).edit().clear().commit()
  }

  @After fun tearDown() {
    repos.forEach { TestSupport.release(it) }
    repos.clear()
    TestSupport.resetDatabase()
  }

  private fun repo(): NeuroVidaRepository = NeuroVidaRepository(app).also { repos.add(it) }
  private fun stars() = app.getSharedPreferences("star_measures", Context.MODE_PRIVATE)
  private fun skill() = app.getSharedPreferences("skill", Context.MODE_PRIVATE)

  /** Medidas guardadas con la escalera VIEJA: Carga a 0,92 (el 4,6 de 5), una sin dato (−1), y otras de juegos que no se alargaron. */
  private val oldPoints = listOf(
    MeasurePoint(1_000L, "carga", 60f, 0.92f, false),
    MeasurePoint(2_000L, "carga", 70f, -1f, null),
    MeasurePoint(3_000L, "vocab", 55f, 0.8f, false, PlayMode.A_TU_MEDIDA),
    MeasurePoint(4_000L, "carga", 80f, 0.4f, true, PlayMode.DESAFIO),
    MeasurePoint(5_000L, "brake", 210f, 0.6f, false)
  )

  private fun seedOld(marks: Map<String, Int> = emptyMap()) {
    stars().edit().putString("points", StarMeasures.encode(oldPoints)).commit()
    skill().edit().putString("state", SkillState(measured = setOf("calculo"), ladderMarks = marks).encode()).commit()
  }

  private fun ratings(points: List<MeasurePoint>) = points.map { it.timestamp to it.rating }

  @Test
  fun `la traduccion de star_measures lleva a cinco octavos solo el rating de Carga y se aplica una sola vez aunque la app se abra dos veces`() {
    seedOld()
    val first = repo()
    TestSupport.awaitUntil(30_000, "El arranque no tradujo la escalera") { first.skill.value.ladderMark("calculo") == 2 }
    TestSupport.awaitUntil(30_000, "Las medidas no se tradujeron") { first.starMeasures.value.first { it.timestamp == 1_000L }.rating < 0.9f }
    val after = StarMeasures.decode(stars().getString("points", null))
    assertEquals("0,92 de 5 niveles = 0,575 de 8", 0.575f, after.first { it.timestamp == 1_000L }.rating, 1e-5f)
    assertEquals("sin dato (−1) no se toca", -1f, after.first { it.timestamp == 2_000L }.rating, 0f)
    assertEquals("0,4 de 5 = 0,25 de 8", 0.25f, after.first { it.timestamp == 4_000L }.rating, 1e-5f)
    assertEquals("otros juegos, igual", 0.8f, after.first { it.timestamp == 3_000L }.rating, 0f)
    assertEquals("otros juegos, igual", 0.6f, after.first { it.timestamp == 5_000L }.rating, 0f)
    // lo demás de cada medida queda como estaba: valor, reloj y modo
    assertEquals(listOf(60f, 70f, 55f, 80f, 210f), after.sortedBy { it.timestamp }.map { it.value })
    assertEquals(listOf<Boolean?>(false, null, false, true, false), after.sortedBy { it.timestamp }.map { it.timed })
    assertEquals(listOf<PlayMode?>(null, null, PlayMode.A_TU_MEDIDA, PlayMode.DESAFIO, null), after.sortedBy { it.timestamp }.map { it.mode })
    assertEquals("la marca quedó en la preferencia skill", 2, SkillState.decode(skill().getString("state", null)).ladderMark("calculo"))
    assertTrue("y lo que ya estaba en skill se conserva", "calculo" in SkillState.decode(skill().getString("state", null)).measured)
    val written = stars().getString("points", null)
    // la app se abre otra vez (otro repositorio, mismas preferencias): NO se vuelve a traducir
    repos.forEach { TestSupport.release(it) }; repos.clear()
    val second = repo()
    TestSupport.awaitUntil(30_000, "La segunda apertura no cargó") { second.isFirstLoadDone() }
    runBlocking { second.migrateLadderVersionsIfNeeded() }
    assertEquals("la segunda apertura no toca nada", written, stars().getString("points", null))
    assertEquals(ratings(after), ratings(second.starMeasures.value.sortedBy { it.timestamp }))
    // dos llamadas a la vez (el arranque y alguien más) tampoco traducen dos veces
    runBlocking { second.migrateLadderVersionsIfNeeded(); second.migrateLadderVersionsIfNeeded() }
    assertEquals(0.575f, StarMeasures.decode(stars().getString("points", null)).first { it.timestamp == 1_000L }.rating, 1e-5f)
  }

  @Test
  fun `dos arranques que corren a la vez traducen una sola vez`() {
    seedOld()
    val r = repo()                                                  // su init ya lanza la traducción
    runBlocking { (1..4).map { async(Dispatchers.Default) { r.migrateLadderVersionsIfNeeded() } }.awaitAll() }
    TestSupport.awaitUntil(30_000, "El arranque no tradujo la escalera") { r.skill.value.ladderMark("calculo") == 2 }
    assertEquals(0.575f, StarMeasures.decode(stars().getString("points", null)).first { it.timestamp == 1_000L }.rating, 1e-5f)
    assertEquals(0.575f, r.starMeasures.value.first { it.timestamp == 1_000L }.rating, 1e-5f)
  }

  @Test
  fun `con la marca puesta no se toca nada`() {
    seedOld(marks = mapOf("calculo" to 2))                       // ya traducido: sus medidas están en la escalera de 8
    val r = repo()
    TestSupport.awaitUntil(30_000, "El arranque no cargó") { r.isFirstLoadDone() }
    runBlocking { r.migrateLadderVersionsIfNeeded() }
    assertEquals(ratings(oldPoints), ratings(StarMeasures.decode(stars().getString("points", null))))
    assertEquals(2, r.skill.value.ladderMark("calculo"))
  }

  @Test
  fun `una instalacion nueva sin medidas solo recibe sus marcas`() {
    val r = repo()
    TestSupport.awaitUntil(30_000, "El arranque no puso las marcas") { r.skill.value.ladderMark("calculo") == 2 }
    assertTrue("sin medidas no se escribe nada", stars().getString("points", null).isNullOrEmpty())
    assertEquals("solo los juegos alargados llevan marca", setOf("calculo"), r.skill.value.ladderMarks.keys)
    // lo que se juegue desde ahora ya va con la escalera de 8: una apertura nueva no lo toca
    val point = MeasurePoint(9_000L, "carga", 75f, 0.6f, false)
    stars().edit().putString("points", StarMeasures.encode(listOf(point))).commit()
    repos.forEach { TestSupport.release(it) }; repos.clear()
    val again = repo()
    TestSupport.awaitUntil(30_000, "La apertura no cargó") { again.isFirstLoadDone() }
    runBlocking { again.migrateLadderVersionsIfNeeded() }
    assertEquals(0.6f, StarMeasures.decode(stars().getString("points", null)).single().rating, 0f)
  }

  @Test
  fun `Borrar datos deja las marcas de ahora y lo que se juegue despues no se traduce de mas`() {
    seedOld()
    val r = repo()
    TestSupport.awaitUntil(30_000, "El arranque no tradujo la escalera") { r.skill.value.ladderMark("calculo") == 2 }
    runBlocking { r.resetData() }
    assertEquals(2, r.skill.value.ladderMark("calculo"))
    assertEquals(2, SkillState.decode(skill().getString("state", null)).ladderMark("calculo"))
    assertTrue("sin medidas", r.starMeasures.value.isEmpty())
    // se juega después de borrar: la medida nace con la escalera de 8 y la apertura siguiente no la toca
    stars().edit().putString("points", StarMeasures.encode(listOf(MeasurePoint(9_000L, "carga", 75f, 0.6f, false)))).commit()
    repos.forEach { TestSupport.release(it) }; repos.clear()
    val again = repo()
    TestSupport.awaitUntil(30_000, "La apertura no cargó") { again.isFirstLoadDone() }
    runBlocking { again.migrateLadderVersionsIfNeeded() }
    assertEquals(0.6f, StarMeasures.decode(stars().getString("points", null)).single().rating, 0f)
  }

  @Test
  fun `la regla de traduccion es pura, lleva el rating de un juego alargado y deja el resto`() {
    val out = StarMeasures.rescaleRatings(oldPoints, mapOf("calculo" to 5f / 8f))
    assertEquals(listOf(0.575f, -1f, 0.8f, 0.25f, 0.6f), out.map { it.rating })
    assertEquals("sin juegos que traducir, nada cambia", oldPoints, StarMeasures.rescaleRatings(oldPoints, emptyMap()))
    assertEquals("una razón de 1 tampoco", oldPoints, StarMeasures.rescaleRatings(oldPoints, mapOf("calculo" to 1f)))
  }
}
