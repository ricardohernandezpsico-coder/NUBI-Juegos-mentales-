package com.example.data

import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Date
import java.util.Locale

/**
 * Lo que dicen los cuatro cuadros de Tú (Tarea 185, 28-oct; `auditoria-fable/tu-pestana.md` §2.2 B «Lo tuyo en cuadros»): racha, partidas, medallas y juegos probados. Cada uno es una cifra grande, su unidad en una frase, una línea de apoyo y la frase completa que lee el
 * lector de pantalla («Racha: 7 días seguidos. Tu mejor racha: 12 días.»). Nada se inventa: salen de lo que la app ya sabe (`Streak`, el historial, `Achievements`). Singular y plural van bien dichos («1 día seguido», «1 partida») y ningún cuadro repite lo de Hoy (la semana) ni
 * lo de Avance (las áreas). Sin culpa: una racha de 0 no dice «perdiste», dice cuándo empieza.
 */
data class TuTile(
  /** «racha», «partidas», «medallas» o «juegos» (el nombre interno y el rótulo del lector de pantalla). */
  val key: String,
  /** La cifra grande: «7». */
  val figure: String,
  /** Su unidad en una frase: «días seguidos». */
  val unit: String,
  /** La línea de apoyo (16 sp, más suave): «Tu mejor racha: 12 días». null si no hay. */
  val support: String?,
  /** Lo que lee el lector de pantalla: el cuadro entero como una frase. */
  val spoken: String
)

object TuStats {
  /** Cuántos días: «1 día», «12 días». */
  private fun days(n: Int) = if (n == 1) "1 día" else "$n días"

  /**
   * Los cuatro cuadros, en su orden: [currentStreak] y [bestStreak] (la racha que perdona; la mejor nunca baja de la de hoy), [games] = las partidas guardadas y [firstPlayed] = cuándo fue la primera (null sin partidas), [medals] de [medalsTotal] y
   * [distinctGames] de [totalGames] juegos probados al menos una vez.
   */
  fun tiles(
    currentStreak: Int, bestStreak: Int, games: Int, firstPlayed: Long?, now: Long,
    medals: Int, medalsTotal: Int, distinctGames: Int, totalGames: Int
  ): List<TuTile> {
    val best = maxOf(bestStreak, currentStreak)
    val streakUnit = if (currentStreak == 1) "día seguido" else "días seguidos"
    val streakSupport = if (best <= 0) "Tu racha empieza con tu próxima partida" else "Tu mejor racha: ${days(best)}"
    val gamesUnit = if (games == 1) "partida" else "partidas"
    val gamesSupport = if (firstPlayed == null || games == 0) "Tu primera partida te espera" else "desde el ${since(firstPlayed, now)}"
    // lo que SE VE lleva el número pegado a su unidad (`glueNumbers`: «21 días», «8 de octubre», «de 15 medallas»); lo que lee el lector de pantalla, con espacios normales
    return listOf(
      TuTile("racha", "$currentStreak", streakUnit.glueNumbers(), streakSupport.glueNumbers(), "Racha: $currentStreak $streakUnit. $streakSupport."),
      TuTile("partidas", "$games", gamesUnit.glueNumbers(), gamesSupport.glueNumbers(), "Partidas: $games $gamesUnit, $gamesSupport."),
      TuTile("medallas", "$medals", "de $medalsTotal medallas".glueNumbers(), null, "Medallas: $medals de $medalsTotal medallas."),
      TuTile("juegos", "$distinctGames", "de $totalGames juegos".glueNumbers(), "probados al menos una vez", "Juegos: $distinctGames de $totalGames juegos, probados al menos una vez.")
    )
  }

  /** «3 de octubre», con el año si no es el de [now] («3 de octubre de 2025»). */
  fun since(timestamp: Long, now: Long): String {
    val year = Calendar.getInstance().apply { timeInMillis = timestamp }.get(Calendar.YEAR)
    val thisYear = Calendar.getInstance().apply { timeInMillis = now }.get(Calendar.YEAR)
    val pattern = if (year == thisYear) "d 'de' MMMM" else "d 'de' MMMM 'de' yyyy"
    return SimpleDateFormat(pattern, Locale("es")).format(Date(timestamp))
  }
}
