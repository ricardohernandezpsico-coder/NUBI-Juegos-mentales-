package com.example.data

import org.json.JSONObject

/** Los márgenes (dp) entre el borde de la imagen y su cuerpo: ahí cae la sombra de contacto. */
data class KitMargins(val left: Float, val top: Float, val right: Float, val bottom: Float)

/** La losa de nine-slice: la imagen mide [widthPx] × [heightPx] a [ClayKitSpec.pxPerDp] px por dp; sus insets (px) fijan las esquinas (con su sombra) y dejan el centro, parejo, para estirarlo. */
data class KitSlab(
  val widthPx: Int, val heightPx: Int, val margins: KitMargins, val bodyWidth: Float, val bodyHeight: Float,
  val insetLeftPx: Int, val insetTopPx: Int, val insetRightPx: Int, val insetBottomPx: Int,
  /** Cuánto baja el texto (dp) cuando la losa se presiona. */
  val pressTextDy: Float
)

/** Una pieza suelta (el botón redondo con X o el triángulo de «Jugar»): la imagen se dibuja entera con [margins] alrededor de su cuerpo de [bodyWidth] × [bodyHeight] dp. */
data class KitPiece(val widthPx: Int, val heightPx: Int, val margins: KitMargins, val bodyWidth: Float, val bodyHeight: Float)

/**
 * `res/raw/kit.json` (Tarea 89): lo que Compose necesita para dibujar el kit de plasticina renderizado en Blender (`tools/isla/isla.py --kit` y `tools/isla/kit.py`). Si se vuelve a renderizar con otro tamaño, cambian las imágenes y este JSON juntos;
 * `ClayKitTest` falla si quedan desparejados (tamaño de cada WebP, insets dentro de la imagen).
 */
data class ClayKitSpec(val pxPerDp: Float, val slab: KitSlab, val close: KitPiece, val play: KitPiece) {
  companion object {
    fun parse(text: String): ClayKitSpec {
      val root = JSONObject(text)
      fun margins(o: JSONObject) = o.getJSONObject("margenes_dp").let { KitMargins(it.getDouble("izq").toFloat(), it.getDouble("arr").toFloat(), it.getDouble("der").toFloat(), it.getDouble("abajo").toFloat()) }
      val l = root.getJSONObject("losa")
      val size = l.getJSONArray("imagen_px"); val ins = l.getJSONObject("insets_px"); val body = l.getJSONObject("cuerpo_dp")
      val slab = KitSlab(
        size.getInt(0), size.getInt(1), margins(l), body.getDouble("ancho").toFloat(), body.getDouble("alto").toFloat(),
        ins.getInt("izq"), ins.getInt("arr"), ins.getInt("der"), ins.getInt("abajo"),
        l.getJSONObject("presionada").getDouble("texto_baja_dp").toFloat()
      )
      fun piece(o: JSONObject): KitPiece {
        val s = o.getJSONArray("imagen_px"); val b = o.get("cuerpo_dp")
        val (bw, bh) = if (b is JSONObject) b.getDouble("ancho").toFloat() to b.getDouble("alto").toFloat() else (b as Number).toFloat() to (b as Number).toFloat()
        return KitPiece(s.getInt(0), s.getInt(1), margins(o), bw, bh)
      }
      return ClayKitSpec(root.getDouble("px_por_dp").toFloat(), slab, piece(root.getJSONObject("cerrar")), piece(root.getJSONObject("jugar")))
    }
  }
}
