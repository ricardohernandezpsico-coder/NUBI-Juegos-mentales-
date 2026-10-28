package com.example.data

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
}
