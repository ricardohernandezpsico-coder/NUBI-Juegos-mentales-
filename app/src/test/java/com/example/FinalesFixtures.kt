package com.example

import com.example.data.MeasurePoint
import com.example.data.PuntaEntry
import com.example.data.PuntaTier
import com.example.model.GamePlayResult

/**
 * Una partida de ejemplo de cada uno de los 17 juegos de la app (con su medida) y 5 partidas anteriores comparables de cada uno: lo que usan `FinalFitTest` y las capturas del final «B» (Tarea 146).
 * «Primera partida» = `playsBefore` 0 y sin historial; «con historial» = 5 partidas comparables (`history`).
 */
internal object FinalesFixtures {
  private fun base(id: String, correct: Int = 7, total: Int = 10, level: Int = 4, block: GamePlayResult.() -> GamePlayResult = { this }) =
    GamePlayResult(gameId = id, score = 74, correctAnswers = correct, totalTrials = total, timed = false, level = level, endRating = 0.5f, timestamp = 1_000_000L).block()

  val byGame: Map<String, GamePlayResult> = listOf(
    base("parejas", 7, 9, 8) { copy(conGroup = 4, conGroups = 6, conMemGroups = 4, conBestStreak = 5, conBest = 6, conNewRecord = false) },
    base("secuencia", 14, 19, 9) { copy(rasBestLen = listOf(5, 4, 3, 2), rasRounds = listOf(7, 4, 5, 3), rasHits = listOf(7, 3, 2, 2), rasModesSeen = 15, rasNewModes = 0) },
    base("bodega", 9, 12, 6) { copy(bodGroup = 3, bodBestStreak = 4, bodBiggest = 5, bodBest = 6, bodNewRecord = false, bodMs = 4500) },
    base("correo", 8, 11, 5) { copy(mailGroup = 2, mailEvHits = 4, mailEvTotal = 4, mailTimeHits = 1, mailTimeTotal = 2, mailCommissions = 0, mailPeeks = 3, mailPeeksGood = 1, mailRight = 6, mailBest = 9, mailNewRecord = false) },
    base("stroop", 20, 24) { copy(interferenceMs = 420, switchCostMs = 180) },
    base("piloto") { copy(pilSignalPct = 58, pilHits = 14, pilTargets = 18, pilFalse = 2, pilLanePct = 82, pilSignalLevel = 6, pilBestStreak = 7, pilPoints = 1250) },
    base("freno", 7, 10, 2) { copy(brakeMs = 245, stopsOk = 6, stopsTotal = 8, brakeBestSsdMs = 180) },
    base("satelites", 7, 8) { copy(trackingCapacity = 2.6f, trackingTargets = 3.5f, satLights = 12, satPerfect = 5, satBestStreak = 3, satBest = 22, satNewRecord = false) },
    base("radar", 9, 12, 6) { copy(glanceMs = 170, glanceLoad = 3f, rescRescued = 23, rescBest = 31, rescNewRecord = false, captureK = 3.5f, rescPerfect = 5, rescRounds = 12, rescBestStreak = 3, rescShortestMs = 150, rescTrips = 2) },
    base("calculo", 5, 8) { copy(cargaAlone = 5, cargaHinted = 2, cargaShort = 3, cargaMs = 14_000) },
    base("engranajes", 7, 10, 3) { copy(engrEtapa = 3, engrLaunches = 1, engrOrbit = 3, engrLights = 0, engrMs = 7500) },
    base("acoplamiento") { copy(rotationSpeedDps = 212, rotationCurveMs = listOf(900, 1100, 1400, 1750, 2100), dockDocked = 24, dockRings = 3, dockBest = 31, dockNewRecord = false) },
    base("aterrizaje", 13, 15) {
      copy(
        numlineErrorPct = 7.4f, numlineTrue = listOf(0.1f, 0.2f, 0.45f, 0.55f, 0.8f, 0.9f), numlineGiven = listOf(0.11f, 0.19f, 0.46f, 0.54f, 0.81f, 0.89f),
        numlineBullseyes = 3, landDomes = 2, landStreak = 6
      )
    },
    base("anagramas", 6, 8, 3) {
      copy(
        puntaSolo = 5, puntaPista = 2, puntaLetras = 0, puntaVista = 1, puntaMs = 6500,
        puntaWords = listOf(PuntaEntry("luna", PuntaTier.SOLO), PuntaEntry("barco", PuntaTier.PISTA), PuntaEntry("nube", PuntaTier.VISTA))
      )
    },
    base("meteoros") {
      copy(
        lexBandSeen = listOf(8, 8, 8, 8, 8, 8), lexBandHits = listOf(8, 8, 7, 6, 5, 3), lexFaSeen = listOf(7, 7, 7), lexFaHits = listOf(0, 1, 2),
        lexRtCommonMs = 700, lexRtRareMs = 1100, lexRareWords = listOf("alcayata", "badil")
      )
    },
    base("disparate") { copy(svSeenType = listOf(8, 8, 8, 8, 8, 8), svRtType = listOf(900, 950, 1300, 1500, 1100, 1000), svWpm = 142, svSubtleHits = 6, svSubtleSeen = 8, svBestStreak = 14) },
    base("intrusa", 11, 14) { copy(intrSeenType = listOf(3, 3, 2, 2, 2, 2), intrHitsType = listOf(3, 2, 2, 2, 1, 1), intrBonusSeen = 5, intrBonusHits = 3, intrRtMs = 1800) }
  ).associateBy { it.gameId }

  /** Los 17, en el orden del catálogo de arriba. */
  val all: List<GamePlayResult> get() = byGame.values.toList()

  /** La medida guardada de cada juego (`StarMeasures`: su clave y unos valores parecidos a los de hoy para que la comparación sea «parejo»). */
  private val series: Map<String, Pair<String, List<Float>>> = mapOf(
    "parejas" to ("place" to listOf(60f, 70f, 80f, 75f, 72f)),
    "secuencia" to ("trail" to listOf(4f, 5f, 4f, 5f, 4f)),
    "bodega" to ("bodega" to listOf(58f, 66f, 83f, 70f, 75f)),
    "correo" to ("estacion" to listOf(55f, 64f, 70f, 66f, 72f)),
    "stroop" to ("stroop" to listOf(300f, 500f, 400f, 450f, 380f)),
    "piloto" to ("mandos" to listOf(50f, 55f, 60f, 52f, 58f)),
    "freno" to ("brake" to listOf(260f, 250f, 240f, 270f, 255f)),
    "satelites" to ("tracking" to listOf(2.2f, 2.4f, 2.9f, 2.5f, 2.6f)),
    "radar" to ("glance" to listOf(200f, 190f, 180f, 185f, 175f)),
    "calculo" to ("carga" to listOf(50f, 75f, 62.5f, 60f, 65f)),
    "engranajes" to ("taller" to listOf(60f, 70f, 80f, 65f, 72f)),
    "acoplamiento" to ("rotation" to listOf(180f, 200f, 260f, 210f, 215f)),
    "aterrizaje" to ("numline" to listOf(12f, 10f, 9.5f, 8f, 7.9f)),
    "anagramas" to ("punta" to listOf(50f, 60f, 70f, 62f, 65f)),
    "meteoros" to ("vocab" to listOf(40f, 50f, 60f, 48f, 52f)),
    "disparate" to ("wpm" to listOf(130f, 140f, 150f, 138f, 144f)),
    "intrusa" to ("atlas" to listOf(70f, 75f, 80f, 74f, 78f))
  )

  /** Cinco partidas anteriores comparables del juego (mismo reloj, mismo nivel). */
  fun history(gameId: String): List<MeasurePoint> {
    val (key, values) = series.getValue(gameId)
    return values.mapIndexed { i, v -> MeasurePoint(1_000L * (i + 1), key, v, 0.5f, false) }
  }
}
