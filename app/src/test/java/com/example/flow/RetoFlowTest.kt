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
    vm.playFromLibrary("radar", PlayMode.A_TU_MEDIDA, timed = true)
    assertTrue("la partida lanzada desde la ficha va con reloj", vm.activeGame.value!!.timed)
    assertTrue(saved("radar"))
    assertTrue(vm.retoFor("radar"))
    assertFalse("otro juego no cambia", vm.retoFor("calculo"))
    assertEquals(mapOf("radar" to true), vm.retoChoices.value)
  }

  @Test
  fun `elegir sin reloj en la ficha se recuerda y la partida va sin reloj`() {
    val vm = newViewModel()
    vm.playFromLibrary("calculo", PlayMode.A_TU_MEDIDA, timed = false)
    assertFalse(vm.activeGame.value!!.timed)
    assertEquals(mapOf("calculo" to false), vm.retoChoices.value)
  }

  @Test
  fun `el camino diario de Hoy va siempre sin reloj aunque el juego tenga el Reto elegido`() {
    val vm = newViewModel()
    vm.playFromLibrary("radar", PlayMode.A_TU_MEDIDA, timed = true)
    vm.launchGame("radar", isDailyFlow = true)
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
