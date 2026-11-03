package com.example.ui.components

import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.ui.theme.Clay

/**
 * UNA fila gris con candado (Tarea 218; `auditoria-fable/idea-pestana-contrarreloj.md` §7, decisión 7 de Ricardo): la misma regla de lo que se ve pero no se tiene que las losas grises de la pestaña (Tarea 215), en versión de una línea. La imagen crema del kit sin saturación y un 12 % más oscura ([LockedSlabFilter]),
 * el candado del kit y [phrase] —«Con Nubilus Plus: partidas libres y los cuatro Contrarrelojes.»— en tinta negrita de 16 sp: nada se dice solo con el color. Toda la fila es un botón que abre la hoja del candado ([onClick]); el lector dice la frase y «botón». 56 dp de alto como mínimo;
 * con letra grande la frase pasa a dos o más renglones por palabras y la fila crece.
 */
@Composable
internal fun LockedRow(phrase: String, onClick: () -> Unit, modifier: Modifier = Modifier, tag: String = "locked_row") {
  ClaySlab(
    SlabColor.CREAM, modifier.fillMaxWidth().semantics(mergeDescendants = true) { contentDescription = phrase }.testTag(tag), onClick = onClick, locked = true
  ) {
    Row(Modifier.fillMaxWidth().heightIn(min = 56.dp).padding(horizontal = 12.dp, vertical = 8.dp), verticalAlignment = Alignment.CenterVertically) {
      KitGlyph(Glyph.LOCK, size = 24.dp, modifier = Modifier.clearAndSetSemantics { this[SemanticsProperties.TestTag] = "${tag}_lock" })
      Spacer(Modifier.width(10.dp))
      Text(phrase, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 22.sp, modifier = Modifier.weight(1f).testTag("${tag}_phrase"))
    }
  }
}
