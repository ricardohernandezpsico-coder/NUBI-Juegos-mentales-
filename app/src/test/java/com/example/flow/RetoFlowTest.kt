package com.example.flow

import android.app.Application
import androidx.test.core.app.ApplicationProvider
import com.example.TestSupport
import com.example.data.PlayMode
import com.example.viewmodel.NeuroVidaViewModel
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * «Sin reloj / Contra el reloj» en la ficha de cada juego (6-oct): la elección se recuerda por juego, el ajuste viejo de Ajustes pasa a ser la elección inicial y el camino diario de Hoy va siempre
 * sin reloj. Cada prueba hace pocas llamadas a la base (varias seguidas dejan trabajo de fondo que a veces rompe la prueba siguiente).
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class RetoFlowTest {

  private lateinit var app: Application

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    app.getSharedPreferences("profile_extra", android.content.Context.MODE_PRIVATE).edit().clear().commit()
  }

  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  private fun newViewModel(): NeuroVidaViewModel = NeuroVidaViewModel(app).also { viewModels.add(it) }

  @After
  fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())      // antes de soltar la base: ver TestSupport.release
    viewModels.clear()
    TestSupport.resetDatabase()
  }

  private fun saved(game: String) = app.getSharedPreferences("profile_extra", android.content.Context.MODE_PRIVATE).getBoolean("reto_$game", false)

  @Test
  fun `la eleccion de la ficha se usa en esa partida y se recuerda solo para ese juego`() {
    val vm = newViewModel()
    vm.playFromHome("freno", PlayMode.A_TU_MEDIDA, timed = true)
    assertTrue("la partida lanzada desde la ficha va con reloj", vm.activeGame.value!!.timed)
    assertTrue(saved("freno"))
    assertTrue(vm.retoFor("freno"))
    assertFalse("otro juego no cambia", vm.retoFor("calculo"))
    assertEquals(mapOf("freno" to true), vm.retoChoices.value)
  }

  @Test
  fun `elegir sin reloj en la ficha se recuerda y la partida va sin reloj`() {
    val vm = newViewModel()
    vm.playFromHome("calculo", PlayMode.A_TU_MEDIDA, timed = false)
    assertFalse(vm.activeGame.value!!.timed)
    assertEquals(mapOf("calculo" to false), vm.retoChoices.value)
  }

  /** Tarea 181: en los cuatro juegos con Contrarreloj ya no hay «¿Con reloj?»: lo que estuviera guardado se ignora (se juega sin reloj) y NO se borra (va en el respaldo). */
  @Test
  fun `en los cuatro juegos con Contrarreloj lo guardado de con reloj se ignora y no se borra`() {
    app.getSharedPreferences("profile_extra", android.content.Context.MODE_PRIVATE).edit().putBoolean("reto_stroop", true).putBoolean("reto_radar", true).putBoolean("reto_freno", true).commit()
    val vm = newViewModel()
    assertEquals("la clave se lee igual", mapOf("stroop" to true, "radar" to true, "freno" to true), vm.retoChoices.value)
    for (g in listOf("stroop", "disparate", "radar", "acoplamiento")) {
      assertFalse("$g: se ignora lo guardado", vm.retoFor(g))
      vm.launchGame(g)
      assertFalse("$g: la partida de siempre va sin reloj", vm.activeGame.value!!.timed)
      vm.playFromHome(g, PlayMode.DESAFIO, timed = true)
      assertFalse("$g: ni aunque llegue timed = true desde la ficha", vm.activeGame.value!!.timed)
    }
    assertTrue("Freno sigue con su reloj guardado", vm.retoFor("freno"))
    assertTrue("las claves de los cuatro siguen en el disco", saved("stroop") && saved("radar"))
    assertFalse("y jugar desde la ficha no escribió las de los otros dos", saved("disparate") || saved("acoplamiento"))
  }

  @Test
  fun `el camino diario de Hoy va siempre sin reloj aunque el juego tenga el Reto elegido`() {
    val vm = newViewModel()
    vm.playFromHome("freno", PlayMode.A_TU_MEDIDA, timed = true)
    vm.launchGame("freno", isDailyFlow = true)
    assertTrue(vm.activeGame.value!!.isDailyFlow)
    assertFalse("la sesión diaria nunca lleva reloj", vm.activeGame.value!!.timed)
  }

  @Test
  fun `el ajuste viejo de Ajustes pasa a ser la eleccion inicial de cada juego`() {
    val vm = newViewModel()
    vm.updateSettings(name = "", weeklyGoal = 4, defaultTimed = true, sound = true, haptics = true)
    TestSupport.awaitUntil(message = "El ajuste no llegó") { vm.userSettings.value.defaultTimed }
    assertTrue("quien tenía el Reto activado lo conserva en todos sus juegos", vm.retoFor("freno"))
    vm.launchGame("freno")
    assertTrue(vm.activeGame.value!!.timed)
  }
}
