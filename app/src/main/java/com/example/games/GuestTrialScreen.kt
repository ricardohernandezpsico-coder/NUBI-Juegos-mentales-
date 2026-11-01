package com.example.games

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialMarks
import com.example.data.glueNumbers
import com.example.model.GameRegistry
import com.example.ui.components.Glyph
import com.example.ui.components.OnBoardText
import com.example.ui.i18n.getGameTitle
import com.example.ui.theme.AppFamily
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.NubiTokens
import com.example.ui.theme.TitleFamily

/**
 * Lo que dejó una partida del «[Debug] Contrarreloj de invitado» (Tarea 205): solo la marca, para ANOTARLA. Nada de esto se guarda en el teléfono (ni la partida, ni el récord, ni la racha, ni las farolas de la semana, ni lo «ya visto» de Unity).
 */
data class GuestTrial(val gameId: String, val hits: Int, val errors: Int) {
  /** La marca de siempre: aciertos menos errores, con piso en 0 (`TimeTrial.Mark` de Unity). */
  val mark: Int get() = (hits - errors).coerceAtLeast(0)
}

/** Los textos del final de invitado (solo depuración; los vigila la guardia de textos como todos). */
object GuestTrialText {
  const val TITLE = "Contrarreloj de invitado"
  const val NOTE = "Esta partida no se guardó: ni récord, ni racha, ni la semana. Anota tu marca."
  const val AGAIN = "Otra vez"
  const val DONE = "Listo"
  fun line(t: GuestTrial): String = "${TimeTrialDay.hitsText(t.hits)} menos ${TimeTrialMarks.errorsText(t.errors)}, en 2 minutos de Contrarreloj."
}

/**
 * El final de una partida de invitado: el juego, la marca con su unidad, cómo se cuenta («34 aciertos menos 2 errores, en 2 minutos de Contrarreloj.»), el aviso de que NO se guardó y dos botones («Otra vez» con el mismo juego y la misma pista de hoy, y «Listo»). Es para que Ricardo anote las marcas de sus 5 personas en su teléfono sin mezclar sus récords.
 */
@Composable
fun GuestTrialScreen(trial: GuestTrial, onAgain: () -> Unit, onDone: () -> Unit, modifier: Modifier = Modifier) {
  val lang = com.example.ui.i18n.LocalAppLanguage.current
  val title = GameRegistry.getById(trial.gameId)?.let { getGameTitle(it.id, lang, it.title) } ?: "Contrarreloj"
  BackHandler(onBack = onDone)
  Column(
    modifier = modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = 24.dp).padding(top = 28.dp, bottom = 32.dp).testTag("guest_trial_result"),
    horizontalAlignment = Alignment.Start
  ) {
    OnBoardText(GuestTrialText.TITLE, color = NubiTokens.OnNightDim, fontFamily = TitleFamily, fontWeight = FontWeight.Bold, fontSize = 18.sp, lineHeight = 24.sp, modifier = Modifier.fillMaxWidth().testTag("guest_trial_title"))
    OnBoardText(title, color = NubiTokens.OnNight, fontFamily = TitleFamily, fontWeight = FontWeight.Bold, fontSize = 26.sp, lineHeight = 32.sp, modifier = Modifier.fillMaxWidth().padding(top = 4.dp).testTag("guest_trial_game"))
    Spacer(Modifier.height(20.dp))
    OnBoardText(TimeTrialDay.hitsText(trial.mark).glueNumbers(), color = NubiTokens.SunOnNight, fontFamily = TitleFamily, fontWeight = FontWeight.Bold, fontSize = 40.sp, lineHeight = 46.sp, modifier = Modifier.fillMaxWidth().testTag("guest_trial_mark"))
    OnBoardText(GuestTrialText.line(trial).glueNumbers(), color = NubiTokens.OnNight, fontFamily = AppFamily, fontWeight = FontWeight.SemiBold, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.fillMaxWidth().padding(top = 6.dp).testTag("guest_trial_line"))
    OnBoardText(GuestTrialText.NOTE, color = NubiTokens.OnNightDim, fontFamily = AppFamily, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.fillMaxWidth().padding(top = 16.dp).testTag("guest_trial_note"))
    Spacer(Modifier.height(28.dp))
    ClayButton(text = GuestTrialText.AGAIN, onClick = onAgain, glyph = Glyph.CLOCK, modifier = Modifier.testTag("btn_guest_trial_again"))
    Spacer(Modifier.height(14.dp))
    ClayButton(text = GuestTrialText.DONE, onClick = onDone, color = Clay.Cream, glyph = Glyph.CHECK, modifier = Modifier.testTag("btn_guest_trial_done"))
  }
}
