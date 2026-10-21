package com.example.flow

import android.app.Application
import androidx.test.core.app.ApplicationProvider
import com.example.TestSupport
import com.example.viewmodel.NeuroVidaViewModel
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * El nombre se edita en el Perfil y se guarda al salir del campo (Tarea 152, B6.3 y B6.5): `setName` guarda el nombre sin espacios sobrantes, deja la meta y el resto de los ajustes como estaban y acepta quedar en blanco (Hoy saluda sin nombre).
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class NombreFlowTest {
  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
  }

  @After
  fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())
    viewModels.clear()
    TestSupport.resetDatabase()
  }

  @Test
  fun `setName guarda el nombre sin espacios y no toca el resto de los ajustes`() {
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    val before = vm.userSettings.value
    vm.setName("  Ana María ")
    TestSupport.awaitUntil(message = "El nombre no se guardó") { vm.userSettings.value.name == "Ana María" }
    val after = vm.userSettings.value
    assertEquals(before.weeklyGoal, after.weeklyGoal)
    assertEquals(before.soundEnabled, after.soundEnabled)
    assertEquals(before.hapticsEnabled, after.hapticsEnabled)
    assertEquals(before.reminderHour, after.reminderHour)
  }

  @Test
  fun `el nombre puede quedar en blanco`() {
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    vm.setName("Ana")
    TestSupport.awaitUntil(message = "El nombre no se guardó") { vm.userSettings.value.name == "Ana" }
    vm.setName("   ")
    TestSupport.awaitUntil(message = "El nombre en blanco no se guardó") { vm.userSettings.value.name == "" }
  }
}
