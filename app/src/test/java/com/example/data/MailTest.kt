package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class MailTest {
  @Test
  fun `planetas tocados por error, sin culpa y con el detalle de los parecidos`() {
    assertNull(Mail.commissionMessage(0, 0))
    assertEquals("Tocaste 1 planeta que no era del encargo.", Mail.commissionMessage(1, 0))
    assertTrue(Mail.commissionMessage(2, 2)!!.contains("color parecido"))
    assertTrue(Mail.commissionMessage(3, 1)!!.contains("(1 de color parecido)"))
  }

  @Test
  fun `el reloj - estrategia, sin mirar, mirando todo el rato`() {
    assertNull(Mail.clockMessage(5, 4, 1))                              // con una sola hora no se dice nada
    assertTrue(Mail.clockMessage(0, 0, 4)!!.startsWith("No miraste"))
    assertTrue(Mail.clockMessage(6, 4, 4)!!.contains("justo antes"))
    assertTrue(Mail.clockMessage(20, 3, 4)!!.contains("repartidas"))
    assertTrue(Mail.clockMessage(5, 1, 4)!!.contains("Truco"))
    assertTrue(Mail.clockMessage(1, 1, 3)!!.contains("1 vez"))
  }

  @Test
  fun `lugar contra hora solo con datos suficientes y diferencia clara`() {
    assertNull(Mail.compareMessage(5, 6, 1, 2))                         // pocas horas
    assertNull(Mail.compareMessage(5, 6, 3, 4))                         // 83% contra 75%: parecido
    assertTrue(Mail.compareMessage(6, 6, 1, 4)!!.contains("por hora"))
    assertTrue(Mail.compareMessage(2, 6, 4, 4)!!.contains("por lugar"))
    assertEquals(0.5f, Mail.rate(2, 4)!!, 1e-6f)
    assertNull(Mail.rate(0, 0))
  }
}
