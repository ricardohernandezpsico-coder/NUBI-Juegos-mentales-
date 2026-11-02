package com.example.ui.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.example.data.PlusAccess
import com.example.data.glueNames
import com.example.ui.components.Glyph
import com.example.ui.components.KitGlyph
import com.example.ui.components.SlabSize
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.ClayCard
import com.example.ui.theme.TitleFamily

/**
 * [glueNames] con la letra de ESTE punto de la composición (Tarea 205c): con más de 1,3 los nombres de división y «Te faltan N aciertos» no se pegan, porque pegados no caben en el renglón y Android los corta por letras. Dentro de un `CapScale` lee la letra ya topada.
 */
@Composable
internal fun String.glueNamesForLetter(): String = glueNames(LocalDensity.current.fontScale)

/**
 * La hoja corta que abre cualquier cosa en gris de la pestaña «Contrarreloj» SIN Nubi Plus (Tarea 199: la abría la puerta de Avance, que desde la Tarea 215 ya no existe): qué trae Plus, en una lista breve, y UN solo botón, «Cerrar» (antes «Entendido»: en un panel de prueba se leyó como «acepto»). El botón de compra llega con Google Play Billing (Etapa 4, `docs/modelo-negocio.md`); hasta entonces la hoja solo cuenta qué hay detrás. Sin precios ni promesas.
 * [kind] dice con qué se compara la persona (`Comparison.Kind`): con una referencia estimada, la hoja dice lo gratis («Gratis: tu división de la semana. Con Nubi Plus, además:»), las tres líneas de lo que agrega Plus, que la referencia no son personas y la fila «De dónde sale la referencia ›» ([onSource]);
 * sin comparación (sin franja de edad o sin tabla), solo habla de «contigo»; [minor] (franja de 13 a 17 años guardada) lo dice: «De 13 a 17 años se comparan solo contigo» (Tarea 209).
 */
@Composable
internal fun PlusLockSheet(onDismiss: () -> Unit, kind: com.example.data.Comparison.Kind = com.example.data.Comparison.Kind.NONE, minor: Boolean = false, onSource: (() -> Unit)? = null) {
  Dialog(onDismissRequest = onDismiss) { PlusLockSheetContent(onDismiss, kind, minor, onSource) }
}

/**
 * El CONTENIDO de la hoja, sin su `Dialog` (Tarea 205c): un `Dialog` compone con la densidad de su propia ventana y una prueba que cambia `LocalDensity` no le cambia la letra (la captura «ahora_sheet_393_20» de la Tarea 205 era idéntica a la de 1,0). Con el contenido suelto se mide a 1,0, 1,3 y 2,0.
 * Se desliza: con tres líneas y letra 2,0 puede no caber entera en el alto de una pantalla, y «Cerrar» queda al final del deslizable.
 */
@Composable
internal fun PlusLockSheetContent(onClose: () -> Unit, kind: com.example.data.Comparison.Kind = com.example.data.Comparison.Kind.NONE, minor: Boolean = false, onSource: (() -> Unit)? = null) {
  ClayCard(Modifier.fillMaxWidth().padding(8.dp).testTag("plus_sheet"), color = Clay.Cream, size = SlabSize.LARGE, contentPadding = 20.dp) {
    // se desliza: con tres líneas (Tarea 203) y letra 2,0 la hoja puede no caber entera en el alto de una pantalla
    Column(Modifier.fillMaxWidth().verticalScroll(androidx.compose.foundation.rememberScrollState()), verticalArrangement = Arrangement.spacedBy(12.dp)) {
      Text(PlusAccess.SHEET_TITLE, color = Clay.Ink, fontSize = 20.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily, lineHeight = 24.sp, modifier = Modifier.semantics { heading() }.testTag("plus_sheet_title"))
      Text(PlusAccess.sheetLead(kind, minor), color = Clay.Ink, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("plus_sheet_lead"))
      Column(verticalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.testTag("plus_sheet_items")) {
        PlusAccess.sheetItems(kind).forEachIndexed { i, item ->
          Row(verticalAlignment = Alignment.Top) {
            KitGlyph(Glyph.CHECK, size = 20.dp, modifier = Modifier.padding(top = 1.dp))
            Spacer(Modifier.width(8.dp))
            Text(item, color = Clay.Ink, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.weight(1f).testTag("plus_sheet_item_$i"))
          }
        }
      }
      // Con la referencia estimada (Tarea 209): la razón primero y, debajo, la fila que abre «De dónde sale la referencia» (la hoja se cierra y se abre ese panel)
      if (kind == com.example.data.Comparison.Kind.ESTIMATED) {
        Text(PlusAccess.SHEET_NOTE_ESTIMATED, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("plus_sheet_note"))
        if (onSource != null) SourceLinkRow(onSource, com.example.ui.components.PanelAccent, "plus_sheet_source")
      }
      ClayButton(text = PlusAccess.SHEET_OK, onClick = onClose, modifier = Modifier.padding(top = 4.dp).testTag("btn_plus_sheet_ok"))
    }
  }
}
