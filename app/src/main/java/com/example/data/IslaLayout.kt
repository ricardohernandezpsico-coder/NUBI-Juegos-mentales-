package com.example.data

import org.json.JSONArray
import org.json.JSONObject

/** Un punto normalizado (0-1) sobre el recorte común de la isla (capas `isla_base` e `isla_edificios`, que miden lo mismo). */
data class IslaPoint(val x: Float, val y: Float)

/** Un lugar de la isla: [key] (`observatorio`, `antena`, `biblioteca`, `hangar`), el área a la que pertenece (`MEMORIA`…) y sus tres puntos. */
data class IslaPlace(val key: String, val area: String, val foot: IslaPoint, val center: IslaPoint, val top: IslaPoint)

/** La caja de una imagen de Nubi (`hola` o `mira_<lugar>`) sobre el recorte común: esquina superior izquierda, esquina inferior derecha y su tamaño en píxeles a 2×. */
data class IslaNubiBox(val topLeft: IslaPoint, val bottomRight: IslaPoint, val widthPx: Int, val heightPx: Int) {
  val width: Float get() = bottomRight.x - topLeft.x
  val height: Float get() = bottomRight.y - topLeft.y
}

/** La sombra de Nubi sobre la losa: elipse de tinta, ancho y alto normalizados a ese ancho y alto del recorte. */
data class IslaShadow(val center: IslaPoint, val width: Float, val height: Float, val opacity: Float, val blur: Float)

/** Uno o varios halos (degradado radial de [colorHex], de [opacity] al centro): [radius] es una fracción del ANCHO del recorte. */
data class IslaHalo(val centers: List<IslaPoint>, val radius: Float, val opacity: Float, val colorHex: String)

/**
 * Las coordenadas que Compose necesita para dibujar encima de la isla renderizada en Blender (`res/raw/isla.json`, que sale de `tools/isla/componer.py` junto con las imágenes: si se regenera la isla con otra cámara,
 * cambian las dos cosas a la vez; `IslaJsonTest` falla antes de llegar al teléfono si quedan desparejadas). Ver `docs/diseno-isla.md` §13.3.
 */
data class IslaLayout(
  /** Tamaño del recorte común a 2× en píxeles (ancho, alto): fija la proporción de la isla en pantalla. */
  val widthPx: Int,
  val heightPx: Int,
  /** Los cuatro lugares en el orden de foco: Observatorio, Antena, Biblioteca, Hangar. */
  val places: List<IslaPlace>,
  /** El tope de cada una de las cuatro farolas de la ruta. */
  val lamps: List<IslaPoint>,
  /** Caja de Nubi `hola` (esquina superior izquierda y esquina inferior derecha) y su tamaño en píxeles a 2×. */
  val nubiTopLeft: IslaPoint,
  val nubiBottomRight: IslaPoint,
  val nubiWidthPx: Int,
  val nubiHeightPx: Int,
  val shadow: IslaShadow,
  val antennaHalo: IslaHalo,
  val lampHalos: IslaHalo,
  /** Las cajas de Nubi mirando hacia cada lugar (`observatorio`, `antena`, `biblioteca`, `hangar`): cada imagen tiene la suya porque se recortó a su borde. */
  val miraBoxes: Map<String, IslaNubiBox> = emptyMap()
) {
  /** Ancho / alto del recorte (0,89): el `Box` de la isla en pantalla. */
  val aspect: Float get() = widthPx.toFloat() / heightPx

  fun place(key: String): IslaPlace = places.first { it.key == key }

  companion object {
    /** El orden en que el lector de pantalla recorre los lugares (el del rombo de la ruta). */
    val PLACE_ORDER = listOf("observatorio", "antena", "biblioteca", "hangar")

    fun parse(text: String): IslaLayout {
      val root = JSONObject(text)
      fun pt(a: JSONArray) = IslaPoint(a.getDouble(0).toFloat(), a.getDouble(1).toFloat())
      fun pts(a: JSONArray) = (0 until a.length()).map { pt(a.getJSONArray(it)) }
      val places = root.getJSONObject("lugares").let { l ->
        PLACE_ORDER.map { key ->
          val o = l.getJSONObject(key)
          IslaPlace(key, o.getString("area").uppercase(), pt(o.getJSONArray("pie")), pt(o.getJSONArray("centro")), pt(o.getJSONArray("techo")))
        }
      }
      val img = root.getJSONObject("imagen").getJSONArray("recorte_px_2x")
      val nubi = root.getJSONObject("capa_nubi").getJSONObject("hola")
      val box = nubi.getJSONArray("caja")
      val px = nubi.getJSONArray("px_2x")
      val sh = root.getJSONObject("nubi").getJSONObject("sombra_elipse")
      val capa = root.getJSONObject("capa_nubi")
      val mira = PLACE_ORDER.associateWith { key ->
        val o = capa.getJSONObject("mira_$key")
        val b = o.getJSONArray("caja"); val q = o.getJSONArray("px_2x")
        IslaNubiBox(pt(b.getJSONArray(0)), pt(b.getJSONArray(1)), q.getInt(0), q.getInt(1))
      }
      val ha = root.getJSONObject("halo_antena")
      val hf = root.getJSONObject("halos_farolas")
      return IslaLayout(
        widthPx = img.getInt(0), heightPx = img.getInt(1),
        places = places,
        lamps = pts(root.getJSONArray("lamparas")),
        nubiTopLeft = pt(box.getJSONArray(0)), nubiBottomRight = pt(box.getJSONArray(1)),
        nubiWidthPx = px.getInt(0), nubiHeightPx = px.getInt(1),
        shadow = IslaShadow(pt(sh.getJSONArray("centro")), sh.getDouble("ancho").toFloat(), sh.getDouble("alto").toFloat(), sh.getDouble("opacidad").toFloat(), sh.getDouble("desenfoque").toFloat()),
        antennaHalo = IslaHalo(listOf(pt(ha.getJSONArray("centro"))), ha.getDouble("radio").toFloat(), ha.getDouble("opacidad").toFloat(), ha.getString("color")),
        lampHalos = IslaHalo(pts(hf.getJSONArray("centros")), hf.getDouble("radio").toFloat(), hf.getDouble("opacidad").toFloat(), hf.getString("color")),
        miraBoxes = mira
      )
    }
  }
}

/**
 * Los cuatro lugares de la isla (`docs/diseno-isla.md` §2): cada área de la app tiene el suyo. «Correo» y «Taller» se descartaron como nombres porque chocaban con juegos (Correo Estelar, Engranajes: Taller de reparación).
 */
object IslaLugar {
  private val NAME_BY_AREA = mapOf("MEMORIA" to "Observatorio", "ATENCION" to "Antena", "RAZONAMIENTO" to "Hangar", "LENGUAJE" to "Biblioteca")
  private val AREA_NAME = mapOf("MEMORIA" to "Memoria", "ATENCION" to "Atención", "RAZONAMIENTO" to "Razonamiento", "LENGUAJE" to "Lenguaje")

  /** «Observatorio» para `MEMORIA`, etc.; null si el área no existe. */
  fun nameOfArea(area: String): String? = NAME_BY_AREA[area]

  /** «Memoria», «Atención», «Razonamiento», «Lenguaje» para `MEMORIA`…; null si el área no existe. */
  fun areaName(area: String): String? = AREA_NAME[area]

  /** «Hangar, Razonamiento»: lo que lee el lector de pantalla al llegar a un edificio. */
  fun spoken(area: String): String? = NAME_BY_AREA[area]?.let { "$it, ${AREA_NAME.getValue(area)}" }

  /** «el Observatorio», «la Antena», «el Hangar», «la Biblioteca»: para frases como «la ruta pasa por…». */
  fun withArticle(area: String): String? = NAME_BY_AREA[area]?.let { (if (it == "Antena" || it == "Biblioteca") "la " else "el ") + it }
}
