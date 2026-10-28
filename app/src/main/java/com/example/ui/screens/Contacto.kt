package com.example.ui.screens

import android.content.ActivityNotFoundException
import android.content.Context
import android.content.Intent
import android.net.Uri
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.disabled
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.NubiContact
import com.example.ui.components.KitButton
import com.example.ui.components.OnBoardText
import com.example.ui.components.SlabColor
import com.example.ui.components.SpaceSectionTitle
import com.example.ui.theme.NubiTokens
import com.example.viewmodel.NeuroVidaViewModel

/**
 * «Contacto», al final de Tú (Tarea 185; `auditoria-fable/tu-pestana.md` §4): «Si algo falla o quieres contarnos algo».
 * 1. «Escribir a Nubi»: abre la app de correo con el correo de Nubi ya puesto ([NubiContact.EMAIL]). Mientras no haya correo (Ricardo todavía no lo crea), la losa se ve pero dice «Correo por definir» y no hace nada; con correo, el correo también va en texto plano debajo, para copiarlo.
 * 2. «Enviar informe de errores» (se mudó desde Ajustes: es contacto, no un ajuste): comparte el texto de `diag/ErrorLog.report` por donde la persona quiera. Sin servidores ni cuentas.
 * 3. Una línea plana: «Nubi 1.0 · sin cuenta, sin internet y sin anuncios».
 */
@Composable
fun ContactoSection(onSendReport: () -> Unit, modifier: Modifier = Modifier, email: String? = NubiContact.EMAIL) {
  val context = LocalContext.current
  Column(modifier.fillMaxWidth().testTag("tu_contacto"), verticalArrangement = Arrangement.spacedBy(10.dp)) {
    SpaceSectionTitle("Contacto", hint = "Si algo falla o quieres contarnos algo")
    val canWrite = NubiContact.canWrite(email)
    KitButton(
      text = NubiContact.WRITE_TITLE, secondLine = NubiContact.secondLine(email), onClick = { if (canWrite) writeToNubi(context, email!!) },
      color = SlabColor.CREAM, fontSize = 18.sp, modifier = Modifier.testTag("btn_write_to_nubi").semantics { if (!canWrite) disabled() }
    )
    if (canWrite) OnBoardText(email!!, color = NubiTokens.OnNight, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.testTag("contact_email"))
    KitButton(
      text = "Enviar informe de errores", onClick = onSendReport, color = SlabColor.CREAM, fontSize = 18.sp,
      modifier = Modifier.padding(top = 4.dp).testTag("btn_send_error_report")
    )
    OnBoardText(
      "Si algo falla, comparte este informe con quien te dio la app. No incluye tu nombre; si una partida no se pudo guardar, incluye los datos de esa partida para encontrar el error.",
      color = NubiTokens.OnNightDim, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.testTag("error_report_help")
    )
    OnBoardText(
      "Nubi ${com.example.BuildConfig.VERSION_NAME} · sin cuenta, sin internet y sin anuncios",
      color = NubiTokens.OnNightDim, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.padding(top = 6.dp).testTag("version_line")
    )
  }
}

/** Abre la app de correo con [email] de destinatario y «Nubi» de asunto; si no hay app de correo, no pasa nada (el correo ya está escrito en la pantalla). */
private fun writeToNubi(context: Context, email: String) {
  val intent = Intent(Intent.ACTION_SENDTO, Uri.parse("mailto:"))
    .putExtra(Intent.EXTRA_EMAIL, arrayOf(email))
    .putExtra(Intent.EXTRA_SUBJECT, NubiContact.SUBJECT)
    .addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
  try { context.startActivity(intent) } catch (_: ActivityNotFoundException) { }
}

/** El informe de errores, sin servidores ni cuentas: la persona lo comparte con quien le dio la app (ver `diag/ErrorLog`). */
internal fun sendErrorReport(context: Context, viewModel: NeuroVidaViewModel) {
  val send = Intent(Intent.ACTION_SEND)
    .setType("text/plain")
    .putExtra(Intent.EXTRA_SUBJECT, "Informe de errores de Nubi")
    .putExtra(Intent.EXTRA_TEXT, com.example.diag.ErrorLog.report(context, viewModel.unclearReport()))
  context.startActivity(Intent.createChooser(send, "Enviar informe").addFlags(Intent.FLAG_ACTIVITY_NEW_TASK))
}
