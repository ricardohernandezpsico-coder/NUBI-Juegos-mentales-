package com.example.data

/**
 * El aviso de salud que pide Google Play (Tarea 121, 17-oct): el texto lo aprobó Ricardo y se muestra IGUAL en Ajustes (sección «Aviso de salud») y en «Licencias y créditos».
 * `VisibleTextsGuardTest` trae UNA excepción justificada, solo para este texto: la regla de vocabulario prohíbe «previene» (promesas de salud), pero aquí es justo lo contrario: dice que Nubi NO previene nada.
 * Razones y reglas de publicidad: `docs/nombre-marca-y-riesgos.md` §6.
 */
object HealthNotice {
  const val TITLE = "Aviso de salud"
  const val TEXT = "Nubi es un conjunto de juegos para mantener la mente activa. No es un dispositivo médico: no diagnostica, trata, cura ni previene ninguna condición. Ante cualquier duda sobre tu salud, consulta a un profesional."
}
