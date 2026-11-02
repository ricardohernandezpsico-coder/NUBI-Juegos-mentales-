package com.example.data

import com.example.Features

/**
 * «Preguntas frecuentes» (Tarea 209; `auditoria-fable/claridad-plus-y-ayuda.md` §1.3): respuestas cortas a lo que más se preguntó en las simulaciones de MiroFish (cambio de teléfono, la racha, con qué compara el Contrarreloj, qué ve alguien de 13 a 17 años, quién hace Nubi y si hace falta internet o una cuenta),
 * en un panel de Tú con losas plegables (la pregunta a la vista; la respuesta al tocar; nada se abre solo). Cada texto lo cuida la guardia de textos como todos: sin «cerebro», «cognitivo», «entrenamiento», «capacidad», «ranking», «velocidad» ni «percentil», y sin el número de robots.
 *
 * Las dos del cobro ([billing]) están redactadas y APAGADAS detrás de [Features.BILLING]: cuando se enciendan van ARRIBA de la lista, porque son las que más se preguntan. Ajuste de Opus (1-nov) a la primera: la copia de seguridad la hace Android, no Nubi, y «Nubi no envía tus datos a nadie» va justo después, para que no parezca una contradicción.
 */
object Preguntas {
  const val TITLE = "Preguntas frecuentes"
  const val LEAD = "Respuestas cortas. Si falta una, escríbenos desde Contacto."

  /**
   * Una pregunta con su respuesta. [sourceLink] pone, bajo la respuesta, la fila [Comparison.SOURCE_LINK] (abre «De dónde sale la referencia»); [subscriptionsButton] pone el botón «Abrir Suscripciones en Google Play» ([SUBSCRIPTIONS_BUTTON]).
   */
  data class Pregunta(val key: String, val question: String, val answer: String, val sourceLink: Boolean = false, val subscriptionsButton: Boolean = false)

  /**
   * «Quién hace Nubi», la sección corta de Tú entre los botones y «Contacto» (Tarea 209, decisión de Ricardo, 1-nov): SOLO el psicólogo, sin nombre («un psicólogo chileno») y sin la IA, que se nombra únicamente en la pregunta «¿Quién hace Nubi?» de abajo, como herramienta.
   * En las simulaciones de MiroFish la IA dicha como herramienta «no molesta, pero tampoco suma» y a quienes tienen 65 o más les baja algo la confianza: en Tú va lo que suma.
   */
  const val WHO_TITLE = "Quién hace Nubi"
  const val WHO_LINE = "Un psicólogo chileno decide cada juego y cada texto."

  /** Las seis que van hoy, en este orden (lo que más preguntaron primero, sin las del cobro). */
  val general: List<Pregunta> = listOf(
    Pregunta(
      "telefono", "¿Qué pasa si cambio de teléfono?",
      "Tu avance se guarda en tu teléfono y en la copia de seguridad de Android de tu cuenta de Google, si la tienes activada. En el teléfono nuevo, entra con la misma cuenta de Google y restaura la copia: vuelven tus partidas, tu avance por juego, tus medallas y tu semana. La copia la hace Android, no Nubi: Nubi no envía tus datos a nadie."
    ),
    Pregunta(
      "racha", "¿Cómo funciona la racha?",
      "La racha cuenta los días seguidos con al menos una partida. Un día sin jugar se perdona cada 7 días: la racha sigue, pero ese día no suma. Los perdones no se acumulan. Es gratis."
    ),
    Pregunta(
      "referencia", "¿Con qué me compara el Contrarreloj?",
      "Con una referencia estimada para tu edad, no con otras personas: Nubi no junta datos de nadie. La referencia sale de estudios por edad y de un programa que jugó este mismo Contrarreloj en un computador. Gratis ves tu división de la semana; con Nubi Plus, además, dónde queda tu marca frente a tu edad y a otras edades.",
      sourceLink = true
    ),
    Pregunta(
      "menores", "¿Qué ve alguien de 13 a 17 años?",
      "Todos los juegos y el Contrarreloj, igual que cualquiera. Sus marcas del Contrarreloj se comparan solo con las suyas: sin divisiones ni referencia por edad."
    ),
    Pregunta(
      "quien", "¿Quién hace Nubi?",
      "Un psicólogo chileno, que decide cada juego y cada texto. Se programó con ayuda de herramientas de inteligencia artificial, que también revisaron errores. Nubi no promete mejorar nada: es un juego."
    ),
    Pregunta("internet", "¿Nubi necesita internet o una cuenta?", "No. Nubi funciona sin cuenta, sin internet y sin anuncios.")
  )

  /** Las dos del cobro, redactadas y APAGADAS ([Features.BILLING]); cuando existan, van arriba. */
  val billing: List<Pregunta> = listOf(
    Pregunta(
      "renueva", "¿Nubi Plus se renueva solo?",
      "El plan mensual y el anual se renuevan solos hasta que los canceles en Google Play. El de pago único se paga una vez y no se renueva. Google Play te manda un correo antes de renovar el plan anual."
    ),
    Pregunta(
      "cancelar", "¿Cómo cancelo Nubi Plus?",
      "En Google Play: abre Play Store, toca tu foto, luego Pagos y suscripciones, Suscripciones, Nubi y Cancelar. Sigues con Plus hasta el final del período que ya pagaste. Desinstalar la app no cancela la suscripción.",
      subscriptionsButton = true
    )
  )

  /** Las que se ven: las del cobro arriba solo con [Features.BILLING] encendida. */
  fun visible(billingOn: Boolean = Features.BILLING): List<Pregunta> = if (billingOn) billing + general else general

  /** El botón de la pregunta de cancelar (48 dp o más): abre la página de Suscripciones de Google Play. */
  const val SUBSCRIPTIONS_BUTTON = "Abrir Suscripciones en Google Play"

  /** El paquete de la app, para el enlace a UNA suscripción (el mismo `applicationId` de siempre). */
  const val PACKAGE = "com.nubibraingames.app"

  /** La página general de Suscripciones de Google Play (la de `https://play.google.com/store/account/subscriptions`; la documenta «Acerca de las suscripciones» de Android Developers). */
  const val SUBSCRIPTIONS_URL = "https://play.google.com/store/account/subscriptions"

  /**
   * El enlace a la suscripción de ESTA app: el general con el `package` de Nubi y, cuando exista el producto, su `sku` (todavía por definir: sin él, Play abre la lista de suscripciones). Se abre con un `Intent` de `ACTION_VIEW`, como los DOI de «En qué se basa Nubi»: la app no necesita permiso de internet.
   */
  fun subscriptionsUrl(sku: String? = null): String = "$SUBSCRIPTIONS_URL?" + (if (sku != null) "sku=$sku&" else "") + "package=$PACKAGE"

  /**
   * El mismo texto, corrido y sin los botones, para la descripción larga de la ficha de Play (Fable §1.3: cabe en 2.000 caracteres con las ocho; con el cobro, las del cobro primero). Cada pregunta y su respuesta, separadas por una línea en blanco.
   */
  fun playListing(billingOn: Boolean = Features.BILLING): String = visible(billingOn).joinToString("\n\n") { "${it.question} ${it.answer}" }
}
