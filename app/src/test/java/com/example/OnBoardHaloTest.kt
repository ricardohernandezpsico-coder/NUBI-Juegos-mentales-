package com.example

import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.text.SpanStyle
import androidx.compose.ui.text.buildAnnotatedString
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.withStyle
import com.example.ui.components.withoutSpanColors
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * La orilla de un texto con tramos de color (Tarea 194): el trazo de 3,5 dp se dibuja en `OnBoardHalo.Tone` aunque un tramo sea amarillo. Esta prueba fija la función pura que quita los colores de los tramos; que el trazo salga navy se prueba con píxeles en `OnBoardTextTest`.
 */
class OnBoardHaloTest {
  private val sun = Color(0xFFFFE6A6)

  private fun text(): AnnotatedString = buildAnnotatedString {
    append("Practicar ")
    withStyle(SpanStyle(color = sun, fontWeight = FontWeight.Bold)) { append("poco y seguido") }
    append(" rinde más")
  }

  @Test
  fun quita_el_color_de_cada_tramo_y_deja_todo_lo_demas() {
    val original = text()
    val halo = original.withoutSpanColors()
    assertEquals("el mismo texto", original.text, halo.text)
    assertEquals("los mismos tramos, en el mismo lugar", original.spanStyles.map { it.start to it.end }, halo.spanStyles.map { it.start to it.end })
    assertTrue("ningún tramo conserva su color", halo.spanStyles.all { it.item.color == Color.Unspecified })
    assertEquals("la negrita se queda: el trazado coincide letra por letra con el del texto", FontWeight.Bold, halo.spanStyles.single().item.fontWeight)
    assertEquals("el original no se toca", sun, original.spanStyles.single().item.color)
  }

  @Test
  fun un_texto_sin_colores_queda_igual() {
    val plain = buildAnnotatedString { append("hola "); withStyle(SpanStyle(fontWeight = FontWeight.Bold)) { append("mundo") } }
    val halo = plain.withoutSpanColors()
    assertEquals(plain.text, halo.text)
    assertEquals(plain.spanStyles.map { Triple(it.start, it.end, it.item.fontWeight) }, halo.spanStyles.map { Triple(it.start, it.end, it.item.fontWeight) })
  }

  @Test
  fun un_texto_sin_tramos_queda_igual() {
    assertEquals("sin tramos", AnnotatedString("sin tramos").withoutSpanColors().text)
  }
}
