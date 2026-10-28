package com.example.data

/**
 * El contacto de Nubi (Tarea 185, 28-oct; `auditoria-fable/tu-pestana.md` §4): la sección «Contacto» de Tú. Google Play pide un correo del desarrollador, que sale público en la ficha de la tienda, y Ricardo decidió que sea un correo NUEVO solo para Nubi
 * que todavía no crea: por eso [EMAIL] es null. Mientras sea null, la losa «Escribir a Nubi» se ve pero dice «Correo por definir» y no hace nada; cuando Ricardo lo cree, se escribe aquí UNA vez y la losa abre la app de correo con ese destinatario (y el correo aparece
 * en texto plano debajo, para copiarlo). Pendiente en `docs/hoja-de-ruta.md`, Etapa 4.
 */
object NubiContact {
  /** El correo de Nubi, por ejemplo `"hola@…"`. null = todavía por definir. */
  val EMAIL: String? = null

  const val WRITE_TITLE = "Escribir a Nubi"
  const val TO_DEFINE = "Correo por definir"
  const val SUBJECT = "Nubi"

  /** La segunda línea de la losa «Escribir a Nubi»: el correo, o «Correo por definir» mientras no haya. */
  fun secondLine(email: String? = EMAIL): String = email ?: TO_DEFINE

  /** ¿La losa hace algo? Solo con un correo. */
  fun canWrite(email: String? = EMAIL): Boolean = !email.isNullOrBlank()

}
