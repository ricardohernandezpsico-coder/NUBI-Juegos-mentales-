package com.example.data

import android.content.Context
import com.example.model.AgeRange
import com.squareup.moshi.JsonClass
import com.squareup.moshi.Moshi

/**
 * La REFERENCIA ESTIMADA del Contrarreloj (Tarea 205; decisión de Ricardo del 1-nov; diseño de Fable en `auditoria-fable/referencia-estimada.md`): mientras una franja tenga menos de 50 personas reales, el Contrarreloj compara con una tabla de marcas que producen ROBOTS POR EDAD jugando la pista real en Unity
 * (`AgeReference`, Tarea 204). La tabla va DENTRO del APK (`assets/referencia/<juego>_v<versión de la pista>.json`, unos 1,4 KB cada una): no se pide nada a ningún servidor ni sale ningún dato del teléfono. Es una [Comparison.Source] cuyas [Comparison.Distribution] llevan `estimated = true`: la página
 * siempre dice «referencia estimada» y NUNCA las llama «personas». Por franja hay una sola tabla (la misma todas las semanas: «no cambia de semana en semana»); las clases que no son una franja (todas las edades, las franjas juntas) no existen: devuelve una distribución vacía.
 *
 * Es por (juego, versión de la pista): al subir la versión de una pista (`TimeTrialDay.trackVersion`) se genera el archivo nuevo y, mientras no esté, ese juego queda SIN comparación ([covers] = false: la página se ve como la de quien no tiene comparación).
 */
class EstimatedReference private constructor(private val tables: Map<Pair<String, Int>, Table>) : Comparison.Source {

  /** Lo que lee de un archivo: por franja su histograma (la mitad y los cuartiles los calcula la app desde los tramos); el resto (modelo, fuente, escala, semillas, p25 a p90) es informe del generador. */
  class Table(
    val game: String, val trackVersion: Int, val binWidth: Int, val model: Int, val source: String, val scale: Double, val seed: Long, val playedPerWeek: Int,
    val ranges: Map<AgeRange, Comparison.Distribution>
  )

  /** Los archivos cargados (para las pruebas y el informe). */
  val loaded: Collection<Table> get() = tables.values

  override fun covers(game: String): Boolean = tables.containsKey(game to TimeTrialDay.trackVersion(game))

  override fun distribution(game: String, ranges: List<AgeRange>?, weeksAgo: Int, weeks: Int): Comparison.Distribution {
    val table = tables[game to TimeTrialDay.trackVersion(game)]
    val range = ranges?.singleOrNull()
    if (table == null || range == null) return Comparison.Distribution.empty(table?.binWidth ?: Comparison.BIN_WIDTH, table?.ranges?.values?.firstOrNull()?.bins?.size ?: Comparison.BINS)
    return table.ranges[range] ?: Comparison.Distribution.empty(table.binWidth, table.ranges.values.firstOrNull()?.bins?.size ?: Comparison.BINS)
  }

  // ------------------------------------------------------------------ el JSON de los robots

  @JsonClass(generateAdapter = true)
  data class RangeDto(val robots: Int, val tramos: List<Int>, val media: Double = 0.0, val p25: Int, val p50: Int, val p75: Int, val p90: Int = 0)

  @JsonClass(generateAdapter = true)
  data class FileDto(
    val estimada: Boolean = false,
    val modelo: Int = 0,
    val fuente_modelo: String = "",
    val juego: String,
    val version_pista: Int,
    val ancho_tramo: Int,
    val tramos: Int,
    val robots_por_franja: Int = 0,
    val partidas_por_semana: Int = 0,
    val dias_de_pista: Int = 0,
    val primer_dia: Int = 0,
    val semilla: Long = 0,
    val escala: Double = 1.0,
    val generado: String = "",
    val franjas: Map<String, RangeDto>
  )

  companion object {
    /** La carpeta de `assets` con las tablas. */
    const val ASSET_DIR = "referencia"

    private val adapter by lazy { Moshi.Builder().build().adapter(FileDto::class.java) }

    /**
     * Lee UN archivo; null si no es una tabla válida (no estimada, un histograma que no suma sus robots, otro número de tramos…): una tabla rota no entra, mejor ninguna comparación que una mentira. Las franjas con un nombre que no es de `AgeRange` (o la de 13 a 17 años, que no se compara) se ignoran.
     */
    fun parse(json: String): Table? {
      val dto = try { adapter.fromJson(json) } catch (e: Exception) { null } ?: return null
      if (!dto.estimada || dto.ancho_tramo < 1 || dto.tramos < 2) return null
      val ranges = LinkedHashMap<AgeRange, Comparison.Distribution>()
      for ((key, r) in dto.franjas) {
        val range = AgeRange.entries.firstOrNull { it.name == key } ?: continue
        if (r.tramos.size != dto.tramos || r.tramos.any { it < 0 } || r.tramos.sum() != r.robots || r.robots < Comparison.MIN_PEOPLE) return null
        // la mitad y los cuartiles salen de los TRAMOS (la regla del percentil y de la escalera); los p25/p50/p75/p90 del JSON son solo informe del generador y no se leen
        val bare = Comparison.Distribution(r.robots, r.tramos, 0, 0, 0, binWidth = dto.ancho_tramo, estimated = true)
        ranges[range] = bare.copy(p25 = Comparison.markAtPercentile(25, bare), p50 = Comparison.markAtPercentile(50, bare), p75 = Comparison.markAtPercentile(75, bare))
      }
      if (ranges.isEmpty()) return null
      return Table(dto.juego, dto.version_pista, dto.ancho_tramo, dto.modelo, dto.fuente_modelo, dto.escala, dto.semilla, dto.partidas_por_semana, ranges)
    }

    /** Arma la fuente con los textos de varios archivos (los inválidos se saltan). */
    fun of(files: Collection<String>): EstimatedReference {
      val tables = LinkedHashMap<Pair<String, Int>, Table>()
      for (json in files) parse(json)?.let { tables[it.game to it.trackVersion] = it }
      return EstimatedReference(tables)
    }

    @Volatile private var cached: EstimatedReference? = null

    /** La referencia ya cargada, o null si todavía no se leyó (no lee nada: se puede llamar desde el hilo principal). */
    fun peek(): EstimatedReference? = cached

    /** La referencia que viene en el APK, leída una sola vez (son 4 archivos de ~1,4 KB). Lee de disco: NO llamarla desde el hilo principal (el ViewModel la carga en `Dispatchers.IO`). Sin la carpeta de assets queda vacía: ningún juego tiene comparación. */
    fun get(context: Context): EstimatedReference = cached ?: synchronized(this) {
      cached ?: load(context).also { cached = it }
    }

    private fun load(context: Context): EstimatedReference {
      val assets = context.applicationContext.assets
      val names = try { assets.list(ASSET_DIR)?.filter { it.endsWith(".json") }.orEmpty() } catch (e: Exception) { emptyList() }
      val texts = names.mapNotNull { name -> try { assets.open("$ASSET_DIR/$name").bufferedReader(Charsets.UTF_8).use { it.readText() } } catch (e: Exception) { null } }
      return of(texts)
    }
  }
}
