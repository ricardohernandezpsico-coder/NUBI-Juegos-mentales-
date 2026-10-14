package com.example.data

import org.json.JSONObject

/** Los márgenes (dp) entre el borde de la imagen y su cuerpo: ahí cae la sombra de contacto. */
data class KitMargins(val left: Float, val top: Float, val right: Float, val bottom: Float)

/** La losa de nine-slice: la imagen mide [widthPx] × [heightPx] a [ClayKitSpec.pxPerDp] px por dp; sus insets (px) fijan las esquinas (con su sombra) y dejan el centro, parejo, para estirarlo. */
data class KitSlab(
  val widthPx: Int, val heightPx: Int, val margins: KitMargins, val bodyWidth: Float, val bodyHeight: Float,
  val insetLeftPx: Int, val insetTopPx: Int, val insetRightPx: Int, val insetBottomPx: Int,
  /** Radio de las esquinas (dp). */
  val radius: Float,
  /** Cuánto baja el texto (dp) cuando la losa se presiona. */
  val pressTextDy: Float,
  /** Los colores que se renderizaron de este radio (claves del JSON: sol, crema, lima, cielo, coral, lila). */
  val colors: List<String>
)

/** Una pieza suelta (un disco, un ícono de churro tinta o el triángulo de «Jugar»): la imagen se dibuja entera con [margins] alrededor de su cuerpo de [bodyWidth] × [bodyHeight] dp. [pressTextDy] solo vale para los discos. */
data class KitPiece(val widthPx: Int, val heightPx: Int, val margins: KitMargins, val bodyWidth: Float, val bodyHeight: Float, val pressTextDy: Float = 0f)

/**
 * `res/raw/kit.json` (Tareas 89 y 90): lo que Compose necesita para dibujar el kit de plasticina renderizado en Blender (`tools/isla/isla.py --kit` y `tools/isla/kit.py`). Si se vuelve a renderizar con otro tamaño, cambian las imágenes y este JSON juntos;
 * `ClayKitTest` falla si quedan desparejados (tamaño de cada WebP, insets dentro de la imagen).
 * [slabs] por radio (`ch` = 14 dp, `md` = 22, `gr` = 30), [discs] (`cerrar`, `disco_crema`, `disco_cielo`) y [pieces] (`jugar`, `volver`, `visto`, `izq`, `der`, `compartir`, `candado`, `engranaje`).
 */
data class ClayKitSpec(val pxPerDp: Float, val slabs: Map<String, KitSlab>, val discs: Map<String, KitPiece>, val pieces: Map<String, KitPiece>) {
  fun slab(key: String): KitSlab = slabs[key] ?: error("el kit no tiene la losa «$key»")
  fun disc(key: String): KitPiece = discs[key] ?: error("el kit no tiene el disco «$key»")
  fun piece(key: String): KitPiece = pieces[key] ?: error("el kit no tiene la pieza «$key»")

  companion object {
    fun parse(text: String): ClayKitSpec {
      val root = JSONObject(text)
      fun margins(o: JSONObject) = o.getJSONObject("margenes_dp").let { KitMargins(it.getDouble("izq").toFloat(), it.getDouble("arr").toFloat(), it.getDouble("der").toFloat(), it.getDouble("abajo").toFloat()) }
      fun piece(o: JSONObject): KitPiece {
        val s = o.getJSONArray("imagen_px"); val b = o.getJSONObject("cuerpo_dp")
        val dy = o.optJSONObject("presionada")?.getDouble("texto_baja_dp")?.toFloat() ?: 0f
        return KitPiece(s.getInt(0), s.getInt(1), margins(o), b.getDouble("ancho").toFloat(), b.getDouble("alto").toFloat(), dy)
      }
      fun slab(o: JSONObject): KitSlab {
        val size = o.getJSONArray("imagen_px"); val ins = o.getJSONObject("insets_px"); val body = o.getJSONObject("cuerpo_dp"); val colors = o.getJSONArray("colores")
        return KitSlab(
          size.getInt(0), size.getInt(1), margins(o), body.getDouble("ancho").toFloat(), body.getDouble("alto").toFloat(),
          ins.getInt("izq"), ins.getInt("arr"), ins.getInt("der"), ins.getInt("abajo"),
          o.getDouble("radio_dp").toFloat(), o.getJSONObject("presionada").getDouble("texto_baja_dp").toFloat(), List(colors.length()) { colors.getString(it) }
        )
      }
      fun <T> objects(key: String, make: (JSONObject) -> T): Map<String, T> {
        val o = root.getJSONObject(key); val out = LinkedHashMap<String, T>()
        for (k in o.keys()) o.optJSONObject(k)?.let { out[k] = make(it) }          // salta la «nota» de texto
        return out
      }
      return ClayKitSpec(root.getDouble("px_por_dp").toFloat(), objects("losas", ::slab), objects("discos", ::piece), objects("piezas", ::piece))
    }
  }
}
