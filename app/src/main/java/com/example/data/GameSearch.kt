package com.example.data

import java.text.Normalizer

/**
 * La búsqueda de «Todos los juegos» (Tarea 158): por el nombre del juego, sin importar las tildes ni las mayúsculas («rescate relampago» encuentra «Rescate relámpago»; «TINTA» encuentra «Tinta o Palabra»). Pura: se prueba sin pantalla (`GameSearchTest`).
 */
object GameSearch {
  /** El texto sin tildes ni mayúsculas y sin espacios de más: la forma en que se comparan el nombre y lo escrito (la «ñ» se conserva distinta de la «n»: «año» no es «ano»). */
  fun fold(text: String): String =
    Normalizer.normalize(text.replace('ñ', '\u0001').replace('Ñ', '\u0001'), Normalizer.Form.NFD)
      .replace(Regex("\\p{Mn}+"), "")
      .replace('\u0001', 'ñ')
      .lowercase()
      .trim()
      .replace(Regex("\\s+"), " ")

  /** ¿El nombre [title] tiene lo escrito en [query]? Una búsqueda vacía encuentra todo; si tiene varias palabras, TODAS deben estar en el nombre (en cualquier orden). */
  fun matches(title: String, query: String): Boolean {
    val q = fold(query)
    if (q.isEmpty()) return true
    val t = fold(title)
    return q.split(' ').all { it in t }
  }
}
