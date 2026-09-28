package com.example.data

/** Un juego para la biblioteca: su id, su dominio ([com.example.model.DomainType.name]) y si es juego estrella. */
data class LibraryGame(val id: String, val domain: String, val star: Boolean)

/** Una sugerencia de "Para ti hoy": el juego y por qué. */
data class LibraryPick(val gameId: String, val reason: String)

/**
 * "Para ti hoy" de la pestaña Juegos (28-sep, Ricardo eligió "Para ti + áreas"): elige qué jugar a partir del planeta
 * de Hoy, en este orden: la zona quieta o sin explorar (su juego menos jugado), después los juegos estrella que nunca
 * se probaron, después los que llevan más días sin jugarse. Devuelve varias, sin repetir, para "otro". Lógica pura.
 */
object Library {
  fun picks(
    planet: PlanetState,
    games: List<LibraryGame>,
    lastPlayed: Map<String, Long>,
    now: Long,
    dayOf: (Long) -> Long,
    domainName: (String) -> String
  ): List<LibraryPick> {
    val out = LinkedHashMap<String, String>()
    fun daysAgo(id: String) = lastPlayed[id]?.let { (dayOf(now) - dayOf(it)).toInt() }
    fun leastRecent(list: List<LibraryGame>) = list.minByOrNull { lastPlayed[it.id] ?: Long.MIN_VALUE }

    planet.quiet?.let { d ->
      leastRecent(games.filter { it.domain == d })?.let { g ->
        out[g.id] = if (planet.quietDays < 0) "Aún no exploras tu zona de ${domainName(d)}."
        else "Tu zona de ${domainName(d)} está quieta hace ${planet.quietDays} días."
      }
    }
    games.filter { it.star && it.id !in lastPlayed }.forEach { g ->
      out.putIfAbsent(g.id, "Nuevo para ti: al final te muestra una medida tuya.")
    }
    games.filter { it.id !in lastPlayed }.forEach { g -> out.putIfAbsent(g.id, "Todavía no lo pruebas.") }
    games.filter { it.id in lastPlayed }.sortedBy { lastPlayed[it.id] }.forEach { g ->
      val d = daysAgo(g.id) ?: 0
      if (d >= 2) out.putIfAbsent(g.id, "No lo juegas hace $d días.")
    }
    if (out.isEmpty()) leastRecent(games)?.let { out[it.id] = "Un buen juego para hoy." }
    return out.map { (id, why) -> LibraryPick(id, why) }
  }
}
