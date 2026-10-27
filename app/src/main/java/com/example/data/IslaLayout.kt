package com.example.data

import org.json.JSONArray
import org.json.JSONObject

/** Un punto normalizado (0-1) sobre el recorte común de la isla (capas `isla_base` e `isla_edificios`, que miden lo mismo). */
data class IslaPoint(val x: Float, val y: Float)

/** Un lugar de la isla: [key] (`observatorio`, `antena`, `biblioteca`, `hangar`), el área a la que pertenece (`MEMORIA`…) y sus tres puntos. */
data class IslaPlace(val key: String, val area: String, val foot: IslaPoint, val center: IslaPoint, val top: IslaPoint)

/** La sombra de Nubi sobre la losa: elipse de tinta, ancho y alto normalizados a ese ancho y alto del recorte. */
data class IslaShadow(val center: IslaPoint, val width: Float, val height: Float, val opacity: Float, val blur: Float)

/** Una pieza de la isla por medallas (`IslaPieza`; Tarea 136): [anchor] = donde toca la isla (0-1 del recorte común; de ahí crece al revelarse) y [capa] = `suelo` o `aire` (`res/raw/isla.json`, `decoraciones`). */
data class IslaDeco(val anchor: IslaPoint, val capa: String)

/** Uno o varios halos (degradado radial de [colorHex], de [opacity] al centro): [radius] es una fracción del ANCHO del recorte. */
data class IslaHalo(val centers: List<IslaPoint>, val radius: Float, val opacity: Float, val colorHex: String)

/** Cómo se ve una farola ENCENDIDA (parada hecha hoy; Tarea 88): disco sol en el tope, halo de 2× el de las apagadas y un leve resplandor en la losa a su pie. Los radios son fracciones del ANCHO del recorte; [footDy] baja del tope al pie (fracción del ALTO). */
data class IslaLitLamp(
  val discRadius: Float, val discColorHex: String,
  val haloRadius: Float, val haloOpacity: Float, val haloColorHex: String,
  val footDy: Float, val footGlowRadius: Float, val footGlowOpacity: Float, val footGlowColorHex: String
) {
  companion object {
    val DEFAULT = IslaLitLamp(0.0115f, "#FFC93C", 0.0904f, 0.8f, "#FFC86A", 0.057f, 0.05f, 0.35f, "#FFC86A")
  }
}

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
  /** Qué farola (índice en [lamps]) es de cada lugar (`observatorio`…): la de menor distancia total entre el pie de cada edificio y el tope de cada farola (Tarea 88; `tools/isla/componer.py`). */
  val lampOf: Map<String, Int> = emptyMap(),
  val litLamp: IslaLitLamp = IslaLitLamp.DEFAULT,
  /** Las diez piezas por medallas (por id: `c1`…`vo3`): su ancla y su capa. Vacío si el JSON no las trae. */
  val deco: Map<String, IslaDeco> = emptyMap()
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
      val lampOf = root.optJSONObject("lampara_de")?.let { o -> PLACE_ORDER.mapNotNull { k -> if (o.has(k)) k to o.getInt(k) else null }.toMap() } ?: emptyMap()
      val litLamp = root.optJSONObject("farolas_encendidas")?.let { f ->
        val g = f.getJSONObject("resplandor_losa")
        IslaLitLamp(
          f.getDouble("disco_radio").toFloat(), f.getString("disco_color"), f.getDouble("halo_radio").toFloat(), f.getDouble("halo_opacidad").toFloat(), f.getString("halo_color"),
          f.getDouble("pie_dy").toFloat(), g.getDouble("radio").toFloat(), g.getDouble("opacidad").toFloat(), g.getString("color")
        )
      } ?: IslaLitLamp.DEFAULT
      val deco = root.optJSONObject("decoraciones")?.let { d -> d.keys().asSequence().associateWith { k -> d.getJSONObject(k).let { o -> IslaDeco(pt(o.getJSONArray("ancla")), o.getString("capa")) } } } ?: emptyMap()
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
        lampOf = lampOf, litLamp = litLamp, deco = deco
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
