package com.example.data

/**
 * El aviso de salud que pide Google Play (Tarea 121, 17-oct): el texto lo aprobó Ricardo y se muestra IGUAL en Ajustes (sección «Aviso de salud») y en «Licencias y créditos».
 * `VisibleTextsGuardTest` trae UNA excepción justificada, solo para este texto: la regla de vocabulario prohíbe «previene» (promesas de salud), pero aquí es justo lo contrario: dice que Nubi NO previene nada.
 * Razones y reglas de publicidad: `docs/nombre-marca-y-riesgos.md` §6.
 */
object HealthNotice {
  /**
   * La edad mínima de Nubi (Tarea 216; decisión de Ricardo, 2-nov: «solo por temas legales, así nos resguardamos», `auditoria-fable/referencia-14-17.md` §2): va en «Acerca de Nubilus», de Ajustes, justo bajo el título, en una línea de lectura. Una sola constante, para que la guardia, las pruebas y el cambio de nombre la encuentren en un solo lugar.
   * Es una afirmación, no una barrera: no hay pantalla de edad ni fecha de nacimiento (una barrera solo invita a mentir).
   */
  const val AGE_LINE = "Nubilus es para personas de 14 años o más."

  const val TITLE = "Aviso de salud"
  const val TEXT = "Nubilus es un conjunto de juegos para mantener la mente activa. No es un dispositivo médico: no diagnostica, trata, cura ni previene ninguna condición. Ante cualquier duda sobre tu salud, consulta a un profesional."
}
