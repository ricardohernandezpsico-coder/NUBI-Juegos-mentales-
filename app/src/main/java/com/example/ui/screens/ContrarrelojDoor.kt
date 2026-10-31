package com.example.ui.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.example.data.PlusAccess
import com.example.data.TimeTrialMarks
import com.example.data.glueNumbers
import com.example.ui.components.ClayClockIcon
import com.example.ui.components.Glyph
import com.example.ui.components.KitGlyph
import com.example.ui.components.SlabSize
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.ClayCard
import com.example.ui.theme.TitleFamily

/**
 * La PUERTA de la página «Contrarreloj» en Avance (Tarea 199; Fable §1.2 y §11): UNA losa crema entre «Tus áreas» y «Tus mejores marcas», sin título de sección ni línea de ayuda sobre el cielo (los textos sueltos sobre los luceros se leían mal): el reloj de plasticina de Hoy, «Tus marcas del Contrarreloj» y,
 * debajo, «Tu récord de hoy: 44 aciertos» (el juego del día; sin marcas, «Todavía sin marcas»). Es la única puerta (el final del Contrarreloj lleva un atajo a la misma página, solo con Plus). 64 dp o más de alto, tocable entera.
 *
 * Con Nubi Plus lleva a la página; el chevron del kit lo dice. Sin Plus, la losa sigue diciendo el récord de hoy (es tuyo y ya se ve en Hoy) y suma una tercera línea con el candado, «Con Nubi Plus»; al tocarla se abre [PlusLockSheet]. [record] = la marca del juego de hoy.
 */
@Composable
internal fun ContrarrelojDoor(record: Int?, plus: Boolean, onClick: () -> Unit, modifier: Modifier = Modifier) {
  ClayCard(
    modifier.fillMaxWidth().semantics(mergeDescendants = true) { contentDescription = TimeTrialMarks.doorSpoken(record, plus) }.testTag("avance_contrarreloj_door"),
    onClick = onClick, contentPadding = 0.dp
  ) {
    Row(Modifier.fillMaxWidth().heightIn(min = 64.dp).padding(horizontal = 12.dp, vertical = 12.dp), verticalAlignment = Alignment.CenterVertically) {
      ClayClockIcon(size = 40.dp)
      Spacer(Modifier.width(10.dp))
      Column(Modifier.weight(1f)) {
        Text("Tus marcas del Contrarreloj", color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily, lineHeight = 22.sp, modifier = Modifier.testTag("avance_contrarreloj_title"))
        Text(TimeTrialMarks.doorLine(record).glueNumbers(), color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.testTag("avance_contrarreloj_line"))
        if (!plus) {
          Row(Modifier.padding(top = 4.dp).testTag("avance_contrarreloj_lock"), verticalAlignment = Alignment.CenterVertically) {
            KitGlyph(Glyph.LOCK, size = 20.dp)
            Spacer(Modifier.width(6.dp))
            Text(PlusAccess.LOCK_LABEL, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 21.sp)
          }
        }
      }
      if (plus) {
        Spacer(Modifier.width(4.dp))
        KitGlyph(Glyph.RIGHT, size = 20.dp, modifier = Modifier.testTag("avance_contrarreloj_chevron"))
      }
    }
  }
}

/**
 * La hoja corta que abre la puerta de Avance SIN Nubi Plus (Tarea 199): qué trae la página, en una lista breve, y UN solo botón, «Entendido». El botón de compra llega con Google Play Billing (Etapa 4, `docs/modelo-negocio.md`); hasta entonces la hoja solo cuenta qué hay detrás. Sin precios ni promesas.
 */
@Composable
internal fun PlusLockSheet(onDismiss: () -> Unit) {
  Dialog(onDismissRequest = onDismiss) {
    ClayCard(Modifier.fillMaxWidth().padding(8.dp).testTag("plus_sheet"), color = Clay.Cream, size = SlabSize.LARGE, contentPadding = 20.dp) {
      // se desliza: con seis líneas (Tarea 202 sumó «Tu ritmo») y letra 2,0 la hoja no cabe entera en el alto de una pantalla
      Column(Modifier.fillMaxWidth().verticalScroll(androidx.compose.foundation.rememberScrollState()), verticalArrangement = Arrangement.spacedBy(12.dp)) {
        Text(PlusAccess.SHEET_TITLE, color = Clay.Ink, fontSize = 20.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily, lineHeight = 24.sp, modifier = Modifier.semantics { heading() }.testTag("plus_sheet_title"))
        Text(PlusAccess.SHEET_LEAD, color = Clay.Ink, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("plus_sheet_lead"))
        Column(verticalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.testTag("plus_sheet_items")) {
          PlusAccess.SHEET_ITEMS.forEachIndexed { i, item ->
            Row(verticalAlignment = Alignment.Top) {
              KitGlyph(Glyph.CHECK, size = 20.dp, modifier = Modifier.padding(top = 1.dp))
              Spacer(Modifier.width(8.dp))
              Text(item, color = Clay.Ink, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.weight(1f).testTag("plus_sheet_item_$i"))
            }
          }
        }
        ClayButton(text = PlusAccess.SHEET_OK, onClick = onDismiss, modifier = Modifier.padding(top = 4.dp).testTag("btn_plus_sheet_ok"))
      }
    }
  }
}
