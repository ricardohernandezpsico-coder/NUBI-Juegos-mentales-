package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import kotlin.math.cos
import kotlin.math.sin

class HomingTest {
  /** Una vuelta con desvío [deg] (+ = derecha) y recorrido [ratio] de la distancia a casa. */
  private fun trip(deg: Float, ratio: Float, beacon: Boolean = false): HomingTrip {
    val r = Math.toRadians(deg.toDouble())
    return HomingTrip((ratio * cos(r)).toFloat(), (ratio * sin(r)).toFloat(), beacon)
  }

  @Test
  fun `cada vuelta se separa en rumbo y distancia`() {
    val t = trip(20f, 0.8f)
    assertEquals(20f, t.angleDeg, 1e-3f)
    assertEquals(0.8f, t.distanceRatio, 1e-4f)
    assertEquals(0f, trip(0f, 1f).errorRatio, 1e-5f)
    assertEquals(0.5f, trip(0f, 0.5f).errorRatio, 1e-5f)
    assertEquals(-15f, trip(-15f, 1f).angleDeg, 1e-3f)
    assertEquals(17.5f, Homing.meanAbsAngle(listOf(trip(20f, 1f), trip(-15f, 1f)))!!, 1e-3f)
    assertNull(Homing.meanAbsAngle(emptyList()))
  }

  @Test
  fun `con pocos viajes no nombra patrones`() {
    val few = listOf(trip(30f, 0.5f), trip(30f, 0.5f), trip(30f, 0.5f))
    assertEquals(HomingDistance.SIN_DATOS, Homing.distance(few))
    assertEquals(HomingSource.SIN_DATOS, Homing.source(few))
  }

  @Test
  fun `distancia justa, corta, larga o variable`() {
    assertEquals(HomingDistance.JUSTA, Homing.distance(listOf(trip(5f, 1.02f), trip(-5f, 0.96f), trip(3f, 1.05f), trip(0f, 0.97f))))
    assertEquals(HomingDistance.CORTO, Homing.distance(listOf(trip(5f, 0.7f), trip(-5f, 0.8f), trip(3f, 0.75f), trip(0f, 1.0f))))
    assertEquals(HomingDistance.LARGO, Homing.distance(listOf(trip(5f, 1.3f), trip(-5f, 1.25f), trip(3f, 1.4f), trip(0f, 1.2f))))
    // Mitad corto y mitad largo: se compensan en promedio, pero no es "justa".
    assertEquals(HomingDistance.VARIA, Homing.distance(listOf(trip(0f, 0.6f), trip(0f, 1.4f), trip(0f, 0.65f), trip(0f, 1.35f))))
  }

  @Test
  fun `nombra lo que mas aleja de casa`() {
    // Distancia justa y rumbo torcido: lo que aleja es el rumbo.
    assertEquals(HomingSource.RUMBO, Homing.source(listOf(trip(30f, 1f), trip(-25f, 1f), trip(28f, 1f), trip(-30f, 1f))))
    // Rumbo justo y distancia mal: lo que aleja es la distancia.
    assertEquals(HomingSource.DISTANCIA, Homing.source(listOf(trip(2f, 0.6f), trip(-2f, 1.4f), trip(1f, 0.65f), trip(0f, 1.3f))))
    // Las dos partes pesan parecido.
    assertEquals(HomingSource.PAREJO, Homing.source(listOf(trip(15f, 0.75f), trip(-15f, 1.25f), trip(15f, 0.75f), trip(-15f, 1.25f))))
    assertNotNull(Homing.sourceMessage(HomingSource.RUMBO))
    assertNull(Homing.sourceMessage(HomingSource.PAREJO))
  }

  @Test
  fun `faro comparado solo con viajes suficientes de cada tipo`() {
    val trips = listOf(
      trip(5f, 1f, true), trip(-8f, 1f, true), trip(6f, 1f, true),
      trip(25f, 1f, false), trip(-20f, 1f, false), trip(18f, 1f, false)
    )
    val (with, without) = Homing.beaconAngles(trips)!!
    assertEquals(19f / 3f, with, 1e-3f)
    assertEquals(21f, without, 1e-3f)
    assertTrue(Homing.beaconMessage(with, without).contains("te ayudó"))
    assertNull(Homing.beaconAngles(trips.drop(1)))
    val built = Homing.trips(listOf(1f, 0.5f), listOf(0f, 0.1f), listOf(true, false))
    assertEquals(2, built.size)
    assertEquals(true, built[0].beacon)
  }
}
