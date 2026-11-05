package com.example

import com.example.data.Carga
import com.example.data.GamePoints
import com.example.data.LevelLines
import com.example.data.Skill
import com.example.model.AgeBand
import org.junit.Assume
import org.junit.Test
import java.io.File

/**
 * La tabla de la lámina de la Tarea 233 (`tools/previews/tarea_233.py`): tres personas de ejemplo de Carga exacta (nivel 2 de 5, 4,6 de 5 y en el tope) con su nivel y sus puntos ANTES (la fórmula de la escalera de 5) y AHORA (el código), y dos que ya jugaron en los niveles nuevos.
 * Sale del código real, no de una cuenta aparte. No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea233TablaCapturaTest*"`); el texto va a `src/test/screenshots/t233/tabla.txt` (fuera de git).
 */
class Tarea233TablaCapturaTest {
  @Test fun tabla_de_personas() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    val age = AgeBand.ADULT
    val lines = mutableListOf<String>()
    // {quién, rating de la escalera de 5 de antes}
    val people = listOf("Nivel 2 de 5" to 0.30f, "4,6 de 5" to 0.92f, "En el tope (5 de 5)" to 0.998f)
    for ((who, old) in people) {
      val newRating = Carga.translateOldRating(old)
      // ANTES: la fórmula de siempre sobre 5 niveles (adulto: sin corrimiento); AHORA: el código
      val oldProgress = old.coerceIn(0f, 1f)
      val oldLevel = (1 + Math.floor(oldProgress * 5 + 1e-4).toInt()).coerceIn(1, 5)
      val oldPoints = GamePoints.of("calculo", oldProgress).points
      val progress = Skill.progress("calculo", newRating, age)
      val ladder = Skill.ladderProgress("calculo", newRating, age)
      lines += listOf(who, old, newRating, oldLevel, 5, LevelLines.levelOf("calculo", ladder), 8, oldPoints, GamePoints.of("calculo", progress).points, LevelLines.line("calculo", ladder)).joinToString("|")
    }
    // DESPUÉS DE JUGAR: quien llega a los niveles nuevos (rating de la escalera de 8): los puntos se quedan en el tope y el nivel sigue subiendo
    for ((who, newRating) in listOf("Juega y llega al nivel 7 de 8" to 6.6f / 8f, "Juega y llega al nivel 8 de 8" to 0.95f)) {
      val progress = Skill.progress("calculo", newRating, age)
      val ladder = Skill.ladderProgress("calculo", newRating, age)
      lines += listOf(who, "", newRating, "", "", LevelLines.levelOf("calculo", ladder), 8, "", GamePoints.of("calculo", progress).points, LevelLines.line("calculo", ladder)).joinToString("|")
    }
    val out = File("src/test/screenshots/t233/tabla.txt")
    out.parentFile.mkdirs()
    out.writeText(lines.joinToString(System.lineSeparator()), Charsets.UTF_8)
  }
}
