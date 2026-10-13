package com.example

import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.remember
import androidx.compose.ui.test.junit4.createComposeRule
import com.example.data.RoutePlan
import com.example.ui.components.RouteFx
import com.example.ui.components.RouteMotion
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * El movimiento de las farolas de la ruta (Tarea 88): la matemática del pulso, del saltito de Nubi y de las chispas, y `RouteFx` (el encendido, la mirada de Nubi y el saltito) con el reloj de la prueba; con «quitar animaciones» no se mueve nada.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class RouteMotionTest {
  @get:Rule val rule = createComposeRule()

  // ---- Matemática pura

  @Test
  fun el_pulso_crece_de_0_pasa_un_poco_de_su_tamano_y_se_asienta_en_1() {
    assertEquals(0f, RouteMotion.pulseScale(0f), 1e-6f)
    assertEquals(1f, RouteMotion.pulseScale(1f), 1e-6f)
    assertTrue("pasa de su tamaño a media animación", RouteMotion.pulseScale(0.5f) > 1.05f)
    assertTrue("y vuelve a asentarse", RouteMotion.pulseScale(0.78f) < 1f)
    assertTrue("la opacidad nunca pasa de 1", (0..100).all { RouteMotion.pulseAlpha(it / 100f) <= 1f })
    assertTrue("y no baja de 0", (0..100).all { RouteMotion.pulseAlpha(it / 100f) >= 0f })
    assertEquals("el encendido dura unos 600 ms", 600, RouteMotion.PULSE_MS)
  }

  @Test
  fun el_saltito_son_dos_arcos_de_12_dp_en_900_ms_con_un_giro_leve() {
    assertEquals(900, RouteMotion.JUMP_MS)
    assertEquals(12f, RouteMotion.JUMP_DP, 0f)
    assertEquals("en su sitio al empezar", 0f, RouteMotion.jumpLiftDp(0f), 1e-4f)
    assertEquals("primer salto: arriba", 12f, RouteMotion.jumpLiftDp(0.25f), 1e-3f)
    assertEquals("a la mitad vuelve a pisar", 0f, RouteMotion.jumpLiftDp(0.5f), 1e-3f)
    assertEquals("segundo salto: arriba", 12f, RouteMotion.jumpLiftDp(0.75f), 1e-3f)
    assertEquals("en su sitio al terminar", 0f, RouteMotion.jumpLiftDp(1f), 1e-3f)
    assertTrue("nunca pasa de 12 dp ni baja del suelo", (0..200).all { RouteMotion.jumpLiftDp(it / 200f) in 0f..12.0001f })
    assertTrue("gira a un lado en el primer salto y al otro en el segundo", RouteMotion.jumpTiltDeg(0.25f) > 0f && RouteMotion.jumpTiltDeg(0.75f) < 0f)
    assertTrue("leve: menos de 8 grados", (0..200).all { Math.abs(RouteMotion.jumpTiltDeg(it / 200f)) < 8f })
    assertEquals("recto al empezar y al terminar", 0f, RouteMotion.jumpTiltDeg(1f), 1e-3f)
  }

  @Test
  fun las_chispas_salen_hacia_arriba_se_alejan_y_se_apagan() {
    assertEquals(RouteMotion.SPARKS_PER_LAMP, RouteMotion.sparkAngles(0).size)
    assertTrue("todas salen hacia arriba (ángulo entre −180 y 0)", (0..3).all { lamp -> RouteMotion.sparkAngles(lamp).all { it < -20f && it > -160f } })
    assertTrue("cada farola abre su abanico distinto", RouteMotion.sparkAngles(0) != RouteMotion.sparkAngles(1))
    assertEquals(1f, RouteMotion.sparkAlpha(0f), 1e-6f)
    assertEquals("al terminar no se ven", 0f, RouteMotion.sparkAlpha(1f), 1e-6f)
    assertTrue("se apagan poco a poco", RouteMotion.sparkAlpha(0.3f) > RouteMotion.sparkAlpha(0.7f))
    val (dx0, dy0) = RouteMotion.sparkOffset(-90f, 0f)
    assertEquals(0f, dx0, 1e-6f); assertEquals(0f, dy0, 1e-6f)
    val (_, dy1) = RouteMotion.sparkOffset(-90f, 1f)
    assertEquals("hacia arriba, el ancho de la isla × 0,05", -RouteMotion.SPARK_TRAVEL, dy1, 1e-5f)
  }

  // ---- RouteFx con el reloj de la prueba

  @Test
  fun con_quitar_animaciones_las_farolas_nacen_encendidas_y_nada_se_mueve() {
    val plan = RoutePlan(listOf("observatorio", "hangar", "biblioteca"), complete = true)
    val fx = RouteFx(plan, reduceMotion = true)
    assertFalse(fx.moves)
    for (k in plan.pulse) assertEquals("$k encendida de entrada, sin pulso", 1f, fx.pulseProgress(k), 0f)
    rule.setContent { LaunchedEffect(fx) { fx.run() } }
    rule.mainClock.advanceTimeBy(3_000)
    rule.waitForIdle()
    assertNull("Nubi no mira a ninguna", fx.lookKey)
    assertEquals("Nubi no se mueve", 0f, fx.look.value, 0f)
    assertEquals("sin saltito", 0f, fx.jump.value, 0f)
    assertEquals("sin chispas (en 1 no se ven)", 1f, fx.sparks.value, 0f)
  }

  @Test
  fun las_farolas_que_no_estan_en_el_plan_valen_1() {
    val fx = RouteFx(RoutePlan(listOf("observatorio"), false), reduceMotion = false)
    assertEquals("la del plan empieza apagada", 0f, fx.pulseProgress("observatorio"), 0f)
    assertEquals("las demás ya están encendidas", 1f, fx.pulseProgress("hangar"), 0f)
  }

  @Test
  fun una_farola_pulsa_en_600_ms_y_Nubi_mira_hacia_su_lugar_1_5_s_y_vuelve_al_frente() {
    val fx = RouteFx(RoutePlan(listOf("antena"), false), reduceMotion = false)
    rule.mainClock.autoAdvance = false
    rule.setContent { LaunchedEffect(fx) { fx.run() } }
    rule.mainClock.advanceTimeBy(100)
    assertEquals("mira hacia la Antena", "antena", fx.lookKey)
    rule.mainClock.advanceTimeBy(250)
    assertTrue("a los 350 ms el halo ya creció y Nubi ya miró", fx.pulseProgress("antena") in 0.3f..0.8f && fx.look.value > 0.9f)
    rule.mainClock.advanceTimeBy(300)
    assertEquals("a los 650 ms la farola quedó encendida y quieta", 1f, fx.pulseProgress("antena"), 1e-3f)
    assertEquals("y Nubi sigue mirando (1,5 s en total)", "antena", fx.lookKey)
    rule.mainClock.advanceTimeBy(1_200)
    assertNull("a los ~1,9 s volvió al frente", fx.lookKey)
    assertEquals(0f, fx.look.value, 1e-3f)
    assertEquals("sin ruta completa no hay saltito", 0f, fx.jump.value, 0f)
  }

  @Test
  fun al_completar_la_ruta_Nubi_salta_dos_veces_y_salen_chispas_que_se_apagan() {
    val fx = RouteFx(RoutePlan(listOf("biblioteca"), complete = true), reduceMotion = false)
    rule.mainClock.autoAdvance = false
    rule.setContent { LaunchedEffect(fx) { fx.run() } }
    rule.mainClock.advanceTimeBy(1_550)            // pulso y mirada (1,5 s): el saltito y las chispas empiezan después, con un respiro de 120 ms
    assertEquals("las chispas todavía no salen", 1f, fx.sparks.value, 0f)
    var maxLift = 0f; var sawSparks = false
    repeat(40) {
      rule.mainClock.advanceTimeBy(50)
      maxLift = maxOf(maxLift, RouteMotion.jumpLiftDp(fx.jump.value))
      if (fx.sparks.value < 1f) sawSparks = true
    }
    assertTrue("Nubi sube cerca de 12 dp ($maxLift)", maxLift > 10f)
    assertTrue("hubo chispas", sawSparks)
    rule.mainClock.advanceTimeBy(1_500)
    assertEquals("Nubi vuelve a su sitio", 0f, RouteMotion.jumpLiftDp(fx.jump.value), 1e-3f)
    assertEquals("las chispas se apagaron", 1f, fx.sparks.value, 0f)
  }

  @Test
  fun si_se_encienden_varias_a_la_vez_pulsan_una_tras_otra_y_Nubi_no_mira_a_ninguna() {
    val fx = RouteFx(RoutePlan(listOf("observatorio", "hangar", "biblioteca"), false), reduceMotion = false)
    rule.mainClock.autoAdvance = false
    rule.setContent { LaunchedEffect(fx) { fx.run() } }
    rule.mainClock.advanceTimeBy(300)
    assertNull("sin mirada cuando son varias", fx.lookKey)
    assertTrue("la primera ya va más avanzada que la segunda", fx.pulseProgress("observatorio") > fx.pulseProgress("hangar"))
    assertEquals("la tercera todavía no empezó", 0f, fx.pulseProgress("biblioteca"), 0f)
    rule.mainClock.advanceTimeBy(1_200)
    for (k in listOf("observatorio", "hangar", "biblioteca")) assertEquals("$k al final: encendida", 1f, fx.pulseProgress(k), 1e-3f)
  }
}
