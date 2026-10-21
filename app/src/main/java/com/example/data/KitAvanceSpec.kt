package com.example.data

import org.json.JSONObject

/** El sitio de la placa lisa de una medalla donde va el número (Compose): su centro y su tamaño en dp, desde la esquina de la imagen. */
data class KitPlate(val centerX: Float, val centerY: Float, val width: Float, val height: Float)

/**
 * Una pieza del kit para Avance y Juegos (Tarea 125): la imagen mide [widthPx] × [heightPx] a 3 px por dp, con [margins] (dp) entre su borde y el cuerpo de [bodyWidth] × [bodyHeight] dp (ahí cae la sombra).
 * El canal y el churro son nine-slice horizontal: [insetLeftPx] e [insetRightPx] fijan las tapas y dejan la franja central para estirar. Las medallas llevan su [plate].
 */
data class KitAvancePiece(
  val widthPx: Int, val heightPx: Int, val margins: KitMargins, val bodyWidth: Float, val bodyHeight: Float,
  val insetLeftPx: Int = 0, val insetRightPx: Int = 0, val plate: KitPlate? = null,
  /** Los edificios: dónde toca el suelo (dp desde la esquina de la imagen), para ponerlos sobre el pedestal. */
  val footX: Float = 0f, val footY: Float = 0f
) {
  val isStrip: Boolean get() = insetLeftPx > 0 || insetRightPx > 0
  /** Como pieza suelta (se dibuja entera con [com.example.ui.components.drawKitPiece]). */
  fun asPiece(): KitPiece = KitPiece(widthPx, heightPx, margins, bodyWidth, bodyHeight)
  /** Como tira de nine-slice horizontal (se dibuja con [com.example.ui.components.drawNineSlice]): sin insets arriba ni abajo, así que solo se estira a lo ancho. */
  fun asStrip(): KitSlab = KitSlab(widthPx, heightPx, margins, bodyWidth, bodyHeight, insetLeftPx, 0, insetRightPx, 0, radius = bodyHeight / 2f, pressTextDy = 0f, colors = emptyList())
}

/**
 * `res/raw/kit_avance.json` (Tarea 125): lo que Compose necesita para dibujar las piezas nuevas del kit (canal, churro, emblemas de los 17 juegos, farolas, pedestal, colas del globo, medallas y escudos), renderizadas en Blender
 * (`tools/isla/isla.py --kit --kit-solo avance`) y llevadas a la app por `tools/isla/kit_avance_app.py`. `AvanceKitTest` falla si el JSON y los WebP quedan desparejados.
 */
data class KitAvanceSpec(val pxPerDp: Float, val pieces: Map<String, KitAvancePiece>) {
  fun piece(key: String): KitAvancePiece = pieces[key] ?: error("el kit no tiene la pieza «$key»")

  companion object {
    fun parse(text: String): KitAvanceSpec {
      val root = JSONObject(text); val all = root.getJSONObject("piezas"); val out = LinkedHashMap<String, KitAvancePiece>()
      for (k in all.keys()) {
        val o = all.getJSONObject(k)
        val size = o.getJSONArray("imagen_px"); val m = o.getJSONObject("margenes_dp"); val b = o.getJSONObject("cuerpo_dp"); val ins = o.optJSONObject("insets_px"); val pl = o.optJSONObject("placa_dp"); val foot = o.optJSONArray("pie_dp")
        out[k] = KitAvancePiece(
          size.getInt(0), size.getInt(1), KitMargins(m.getDouble("izq").toFloat(), m.getDouble("arr").toFloat(), m.getDouble("der").toFloat(), m.getDouble("abajo").toFloat()),
          b.getDouble("ancho").toFloat(), b.getDouble("alto").toFloat(), ins?.getInt("izq") ?: 0, ins?.getInt("der") ?: 0,
          pl?.let { KitPlate(it.getDouble("cx").toFloat(), it.getDouble("cy").toFloat(), it.getDouble("ancho").toFloat(), it.getDouble("alto").toFloat()) },
          foot?.getDouble(0)?.toFloat() ?: 0f, foot?.getDouble(1)?.toFloat() ?: 0f
        )
      }
      return KitAvanceSpec(root.getDouble("px_por_dp").toFloat(), out)
    }
  }
}
