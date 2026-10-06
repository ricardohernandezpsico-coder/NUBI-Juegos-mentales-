package com.example.data

import java.util.Locale

/**
 * «Bodega de carga» (juego de Memoria, id `bodega`; ver docs/diseno-bodega-de-carga.md): la lectura de lo que manda Unity para la pantalla final y el récord que se guarda. Lógica pura con pruebas.
 *
 * La carga entra por la esclusa y el robot la guarda en las escotillas; a veces cambia una caja de lugar y a veces la bodega gira; después piden los objetos uno por uno. La medida es «cuántos
 * objetos encontraste al primer intento» (la misma de la tarea clásica de asociación objeto-lugar); la racha más larga, tu bodega más grande de hoy (el pedido más grande sin errores), tu
 * récord (la bodega más grande de siempre; se guarda en las preferencias `bodega_record`, que SÍ van en el respaldo, y se manda a Unity en cada partida) y la etapa más alta (de 5 grupos)
 * la acompañan. No se habla de enfermedades ni se promete nada de salud: va la nota común «No es un diagnóstico».
 */
object Bodega {
  /** Objetos encontrados para mostrar el ritmo y la racha («no sacar conclusiones de pocos casos»). */
  const val MIN_FOR_DETAILS = 3
  /** Tiempo máximo creíble por objeto, en ms (fuera de eso no se muestra). */
  private const val MAX_MS = 120_000

  /** Los 5 grupos de etapa de la pantalla final: pocos objetos, más escotillas, cajas que se mueven, la bodega gira y todo junto. */
  private val GROUPS = listOf("pocos objetos", "más escotillas", "cajas que se mueven", "la bodega gira", "todo junto")

  /** «Encontraste 7 de 10 objetos al primer intento»; null sin objetos. */
  fun headline(firstTry: Int?, total: Int?): String? {
    if (firstTry == null || total == null || total <= 0) return null
    return "Encontraste ${firstTry.coerceIn(0, total)} de $total objetos al primer intento"
  }

  /** El porcentaje al primer intento (0..100); null sin objetos. */
  fun percent(firstTry: Int?, total: Int?): Int? {
    if (firstTry == null || total == null || total <= 0) return null
    return Math.round(100f * firstTry.coerceIn(0, total) / total)
  }

  /** La marca para ver su evolución: % de objetos al primer intento (más alto = mejor). null sin medida. */
  fun mark(firstTry: Int?, total: Int?): Float? {
    if (firstTry == null || total == null || total <= 0) return null
    return 100f * firstTry.coerceIn(0, total) / total
  }

  /** «Etapa más alta: 4 de 5 (la bodega gira)»; null si no es un grupo válido (1..5). */
  fun groupLine(group: Int?): String? {
    if (group == null || group !in 1..GROUPS.size) return null
    return "Etapa más alta: $group de ${GROUPS.size} (${GROUPS[group - 1]})"
  }

  /** «Tu racha más larga: 5 seguidos al primer intento»; solo con [MIN_FOR_DETAILS] o más objetos y una racha de 2 o más. */
  fun streakLine(streak: Int?, total: Int?): String? {
    if (streak == null || streak < 2 || total == null || total < MIN_FOR_DETAILS) return null
    return "Tu racha más larga: $streak seguidos al primer intento"
  }

  /** «Tu bodega más grande hoy: 5 objetos sin errores»; null si ningún pedido salió perfecto. */
  fun biggestLine(biggest: Int?): String? {
    if (biggest == null || biggest <= 0) return null
    return "Tu bodega más grande hoy: " + objects(biggest) + " sin errores"
  }

  /** «Tu récord: 6 objetos»; con «¡Nuevo récord!» solo cuando en esta partida se superó el guardado (lo dice Unity: igualarlo no cuenta). null sin récord. */
  fun recordLine(best: Int?, isNew: Boolean?): String? {
    if (best == null || best <= 0) return null
    return if (isNew == true) "¡Nuevo récord! " + objects(best) else "Tu récord: " + objects(best)
  }

  /** «Tu ritmo: 4,5 s por objeto»; solo con [MIN_FOR_DETAILS] o más objetos. */
  fun paceLine(ms: Int?, total: Int?): String? {
    if (ms == null || ms <= 0 || ms > MAX_MS || total == null || total < MIN_FOR_DETAILS) return null
    return "Tu ritmo: " + String.format(Locale("es"), "%.1f", ms / 1000f) + " s por objeto"
  }

  /** El consejo (con la marca «Truco: » que lee [ResultAdvice]): cuando algún objeto no se encontró al primer intento. null si todos salieron a la primera. */
  fun tip(firstTry: Int?, total: Int?): String? {
    if (firstTry == null || total == null || total <= 0 || firstTry >= total) return null
    return "Truco: imagina cada objeto dentro de su escotilla y dile su nombre en voz baja."
  }

  /** El récord que se guarda: el mayor de lo guardado y lo que trae la partida; nunca baja y nunca es negativo. */
  fun mergeRecord(saved: Int, fromGame: Int?): Int = maxOf(saved.coerceAtLeast(0), (fromGame ?: 0).coerceAtLeast(0))

  /** Para lectores de pantalla. */
  fun spoken(firstTry: Int?, total: Int?): String = headline(firstTry, total) ?: "Sin medida"

  private fun objects(n: Int) = if (n == 1) "1 objeto" else "$n objetos"
}
