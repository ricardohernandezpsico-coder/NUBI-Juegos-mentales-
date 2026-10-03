package com.example.data

import android.content.Context
import com.squareup.moshi.Moshi

/** Un punto de una figura, en 0..1 (x a la derecha, y hacia abajo). */
data class FigPt(val x: Float, val y: Float)

/** El contorno del grabado: puntos suavizados (curva cerrada) o una elipse (centro y radios). */
sealed class FigContour {
  data class Smooth(val points: List<FigPt>) : FigContour()
  data class Ellipse(val cx: Float, val cy: Float, val rx: Float, val ry: Float) : FigContour()
}

/** Un detalle del grabado: ojo, aro pequeño o línea. */
sealed class FigDetail {
  data class Eye(val at: FigPt) : FigDetail()
  data class Dot(val at: FigPt) : FigDetail()
  data class Line(val a: FigPt, val b: FigPt) : FigDetail()
}

/**
 * La figura de una regla de "La estrella intrusa" (assets/intrusa_figuras.json: lo escribe tools/intrusa/figuras.py, con las
 * listas anidadas; la copia de Unity va en listas planas). Sirve para dibujar las miniaturas del atlas en la pantalla final.
 */
data class Figure(
  val rule: String,
  val name: String,
  val points: List<FigPt>,
  val edges: List<Pair<Int, Int>>,
  val anchors: List<Int>,
  val contour: List<FigContour>,
  val details: List<FigDetail>
)

object FigureBank {
  const val ASSET = "intrusa_figuras.json"

  @Volatile private var cache: Map<String, Figure>? = null

  /** Las figuras del asset (una sola vez); vacío si falta o no se puede leer, para que la pantalla final no se caiga. */
  fun load(context: Context): Map<String, Figure> {
    cache?.let { return it }
    val loaded = try {
      context.assets.open(ASSET).bufferedReader().use { parse(it.readText()) }
    } catch (e: Exception) {
      emptyMap()
    }
    cache = loaded
    return loaded
  }

  fun parse(json: String): Map<String, Figure> {
    val root = Moshi.Builder().build().adapter(Any::class.java).fromJson(json) as? Map<*, *> ?: return emptyMap()
    val figures = root["figuras"] as? List<*> ?: return emptyMap()
    val out = LinkedHashMap<String, Figure>()
    for (f in figures) {
      val m = f as? Map<*, *> ?: continue
      val rule = m["regla"] as? String ?: continue
      val name = m["nombre"] as? String ?: continue
      val points = pts(m["puntos"]) ?: continue
      val edges = (m["aristas"] as? List<*>)?.mapNotNull { e ->
        val l = e as? List<*> ?: return@mapNotNull null
        val a = (l.getOrNull(0) as? Number)?.toInt() ?: return@mapNotNull null
        val b = (l.getOrNull(1) as? Number)?.toInt() ?: return@mapNotNull null
        if (a in points.indices && b in points.indices) a to b else null
      }.orEmpty()
      val anchors = (m["anclas"] as? List<*>)?.mapNotNull { (it as? Number)?.toInt() }.orEmpty()
      val contour = (m["contorno"] as? List<*>)?.mapNotNull { c ->
        val cm = c as? Map<*, *> ?: return@mapNotNull null
        val smooth = cm["puntos"]?.let { pts(it) }
        val ell = (cm["elipse"] as? List<*>)?.mapNotNull { (it as? Number)?.toFloat() }
        when {
          smooth != null && smooth.size >= 3 -> FigContour.Smooth(smooth)
          ell != null && ell.size == 4 -> FigContour.Ellipse(ell[0], ell[1], ell[2], ell[3])
          else -> null
        }
      }.orEmpty()
      val details = (m["detalles"] as? List<*>)?.mapNotNull { d ->
        val dm = d as? Map<*, *> ?: return@mapNotNull null
        when {
          dm["ojo"] != null -> pt(dm["ojo"])?.let { FigDetail.Eye(it) }
          dm["punto"] != null -> pt(dm["punto"])?.let { FigDetail.Dot(it) }
          dm["linea"] != null -> pts(dm["linea"])?.takeIf { it.size == 2 }?.let { FigDetail.Line(it[0], it[1]) }
          else -> null
        }
      }.orEmpty()
      out[rule] = Figure(rule, name, points, edges, anchors, contour, details)
    }
    return out
  }

  private fun pt(v: Any?): FigPt? {
    val l = v as? List<*> ?: return null
    val x = (l.getOrNull(0) as? Number)?.toFloat() ?: return null
    val y = (l.getOrNull(1) as? Number)?.toFloat() ?: return null
    return FigPt(x, y)
  }

  private fun pts(v: Any?): List<FigPt>? {
    val l = v as? List<*> ?: return null
    val out = l.map { pt(it) ?: return null }
    return out.takeIf { it.isNotEmpty() }
  }
}
