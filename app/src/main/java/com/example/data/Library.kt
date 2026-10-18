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

/**
 * El orden de los juegos dentro de un área (Tarea 124; Fable, `spec-avance-juegos.md` §2.4): tres grupos con rótulo, los vacíos no se muestran. Lógica pura (`LibraryOrderTest`).
 * 1. **En tu ruta de hoy:** los juegos de la ruta del día que son de esta área, en el orden de la ruta; los ya hechos (los primeros [completedCount] de la ruta) quedan en el grupo, marcados.
 * 2. **Hace días que no juegas:** los jugados alguna vez, de MÁS días sin jugar a menos (decisión: la spec decía «lastPlayedDay desc» y «de más días a menos»; se toma el segundo, que es el que tiene sentido para quien vuelve); empate por orden del catálogo.
 * 3. **Nuevo para ti:** los nunca jugados, en orden del catálogo.
 * `focusGameId` (la última ficha abierta) ya NO reordena: solo pinta la losa en sol.
 */
object LibraryOrder {
  enum class Group(val title: String) {
    RUTA("En tu ruta de hoy"),
    HACE_DIAS("Hace días que no juegas"),
    NUEVO("Nuevo para ti")
  }

  /** Un juego de la lista: [routeStop] = su número de parada (1 a 3) si es de la ruta; [doneToday] = ya hecho en la ruta; [daysSince] = días desde su última partida (null si nunca se jugó). */
  data class Entry(val gameId: String, val routeStop: Int? = null, val doneToday: Boolean = false, val daysSince: Int? = null)

  data class Section(val group: Group, val entries: List<Entry>)

  fun sort(
    games: List<String>,
    routeIds: List<String>,
    completedCount: Int,
    lastPlayedDay: Map<String, Long>,
    today: Long,
    catalogOrder: List<String>
  ): List<Section> {
    val rank = catalogOrder.withIndex().associate { (i, id) -> id to i }
    val inArea = games.toSet()
    val route = routeIds.take(3).withIndex().filter { it.value in inArea }
    val routeSet = route.map { it.value }.toSet()
    fun daysSince(id: String) = lastPlayedDay[id]?.let { (today - it).toInt().coerceAtLeast(0) }
    val ruta = route.map { (i, id) -> Entry(id, routeStop = i + 1, doneToday = i < completedCount, daysSince = daysSince(id)) }
    val rest = games.filter { it !in routeSet }
    val hace = rest.filter { it in lastPlayedDay }
      .sortedWith(compareBy<String> { lastPlayedDay.getValue(it) }.thenBy { rank[it] ?: Int.MAX_VALUE })
      .map { Entry(it, daysSince = daysSince(it)) }
    val nuevo = rest.filter { it !in lastPlayedDay }.sortedBy { rank[it] ?: Int.MAX_VALUE }.map { Entry(it) }
    return listOf(Section(Group.RUTA, ruta), Section(Group.HACE_DIAS, hace), Section(Group.NUEVO, nuevo)).filter { it.entries.isNotEmpty() }
  }
}
