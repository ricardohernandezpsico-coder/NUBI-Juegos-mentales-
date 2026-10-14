package com.example.data

/**
 * Los avatares que se pueden elegir en Opciones (Tarea 91): ocho objetos de plasticina, sin emojis, sin caras de personas y sin símbolos prohibidos (`docs/simbolos-neutros.md`). Lo que se guarda es el ID de texto de [ALL], nunca un dibujo.
 * Antes el avatar era un emoji (🧠 por defecto): [resolve] traduce lo que haya guardado en la base —un emoji viejo, un id nuevo, nada o algo desconocido— a un id válido, así nadie queda sin avatar ni con uno que ya no existe, y no hace falta migrar Room.
 * Qué dibujo corresponde a cada id lo dice `ui/components/KitAvatar.kt`.
 */
object AvatarIds {
  const val NUBI = "nubi"
  const val COHETE = "cohete"
  const val FAROL = "farol"
  const val ISLA = "isla"
  const val ENGRANAJE = "engranaje"
  const val PLANETA = "planeta"
  const val LUCERO = "lucero"
  const val GOTA = "gota"

  /** El avatar por defecto y el de quien no tenía uno válido. */
  const val DEFAULT = NUBI

  /** Los ocho, en el orden en que se muestran. */
  val ALL = listOf(NUBI, COHETE, FAROL, ISLA, ENGRANAJE, PLANETA, LUCERO, GOTA)

  /** Cómo se llama cada uno para el lector de pantalla y para decirlo en voz alta. */
  val LABELS = mapOf(
    NUBI to "Nubi", COHETE to "Cohete", FAROL to "Farol", ISLA to "Isla", ENGRANAJE to "Engranaje", PLANETA to "Planeta", LUCERO to "Lucero", GOTA to "Gota"
  )

  /**
   * Los emojis que se ofrecían hasta la Tarea 90 y a qué avatar nuevo pasan: el cerebro (el de por defecto) y las dos caras de personas → Nubi; las estrellas → el lucero redondo; la diana → el planeta; el búho → la isla; la hoja → la gota; la bombilla → el farol; el rayo → el engranaje.
   * Los emojis se escriben con escapes Unicode para que el barrido de emojis (`EmojiSweepTest`) no encuentre ninguno en el código.
   */
  private val LEGACY = mapOf(
    "\uD83E\uDDE0" to NUBI,            // cerebro
    "\uD83D\uDC75" to NUBI,            // abuela
    "\uD83D\uDC74" to NUBI,            // abuelo
    "\uD83C\uDF1F" to LUCERO,          // estrella brillante
    "\u2B50" to LUCERO,                // estrella
    "\uD83C\uDFAF" to PLANETA,         // diana
    "\uD83E\uDD89" to ISLA,            // búho
    "\uD83C\uDF3F" to GOTA,            // hoja
    "\uD83D\uDCA1" to FAROL,           // bombilla
    "\u26A1" to ENGRANAJE              // rayo
  )

  /** El id válido para lo que haya guardado ([stored]): un id nuevo se queda; un emoji viejo pasa a su avatar; lo demás (nada, vacío o desconocido) es el de por defecto. */
  fun resolve(stored: String?): String {
    val s = stored?.trim().orEmpty().replace("\uFE0F", "")
    if (s in ALL) return s
    return LEGACY[s] ?: DEFAULT
  }
}
