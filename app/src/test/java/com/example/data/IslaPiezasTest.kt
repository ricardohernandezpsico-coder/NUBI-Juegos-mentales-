package com.example.data

import com.example.NoLossGuard
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import java.io.File

/**
 * «Tu isla crece» (Tarea 136, A1 de `auditoria-fable/spec-adopcion-A1-A7.md`): las diez piezas de la isla se DERIVAN de las medallas de conducta (sin estado de progreso nuevo), nunca se quitan, se revelan una vez en Hoy (la primera vez con la versión nueva
 * se siembra en silencio) y se dicen sin palabras prohibidas. Y `res/raw/isla.json` (`decoraciones`) dice lo mismo que el enum.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class IslaPiezasTest {
  private val allIds = Achievements.all.map { it.id }

  // ---- De dónde salen

  @Test
  fun cada_pieza_apunta_a_un_logro_que_se_otorga() {
    for (p in IslaPieza.entries) {
      val def = Achievements.all.firstOrNull { it.id == p.achievementId }
      assertNotNull("${p.id} apunta a «${p.achievementId}», que no está en Achievements.all", def)
      assertFalse("${p.id}: un logro de desempeño (legacy) no da piezas", def!!.legacy)
      assertTrue("${p.id}: la pieza es de la familia de su medalla", def.family == p.family)
    }
    assertTrue("ningún logro de liga ni de desempeño", IslaPieza.entries.none { p -> Achievements.legacy.any { it.id == p.achievementId } })
  }

  @Test
  fun hay_diez_piezas_con_ids_y_medallas_distintos() {
    assertEquals(10, IslaPieza.entries.size)
    assertEquals(10, IslaPieza.entries.map { it.id }.toSet().size)
    assertEquals("cada medalla da a lo más una pieza", 10, IslaPieza.entries.map { it.achievementId }.toSet().size)
    assertEquals(listOf("c1", "c2", "c3", "c4", "v1", "v2", "v3", "vo1", "vo2", "vo3"), IslaPieza.entries.map { it.id })
  }

  @Test
  fun las_tres_familias_tienen_piezas_y_Constancia_tiene_cuatro() {
    val by = IslaPieza.entries.groupBy { it.family }
    assertEquals(setOf(AchievementFamily.CONSTANCIA, AchievementFamily.VARIEDAD, AchievementFamily.VOLVER), by.keys)
    assertEquals(4, by.getValue(AchievementFamily.CONSTANCIA).size)
    assertEquals(3, by.getValue(AchievementFamily.VARIEDAD).size)
    assertEquals(3, by.getValue(AchievementFamily.VOLVER).size)
  }

  @Test
  fun las_medallas_de_los_hitos_chicos_no_traen_pieza() {
    // Constancia 1, 3, 6 y 8 (primer_paso, racha_3, partidas_25, partidas_100) y Variedad 1 (dia_completo): la medalla basta; así las piezas marcan hitos espaciados.
    for (id in listOf("primer_paso", "racha_3", "partidas_25", "partidas_100", "dia_completo")) assertNull(id, IslaPiezas.forAchievement(id))
    assertEquals(IslaPieza.C1, IslaPiezas.forAchievement("racha_7"))
    assertEquals(IslaPieza.VO3, IslaPiezas.forAchievement("vuelta_10"))
  }

  @Test
  fun sin_logros_sin_piezas() {
    assertTrue(IslaPiezas.earned(emptySet()).isEmpty())
    assertTrue("los logros de desempeño no dan piezas", IslaPiezas.earned(Achievements.legacy.map { it.id }).isEmpty())
  }

  @Test
  fun con_todas_las_medallas_estan_las_diez_en_el_orden_de_dibujo() {
    assertEquals(IslaPieza.entries.toList(), IslaPiezas.earned(allIds))
  }

  @Test
  fun con_mas_medallas_nunca_hay_menos_piezas() {
    // Las medallas no se revocan (`AchievementsTest`): cada medalla que se suma, sea cual sea el orden, solo suma piezas.
    for (order in listOf(allIds, allIds.reversed(), allIds.shuffled(java.util.Random(7)))) {
      var unlocked = emptySet<String>()
      var before = emptyList<IslaPieza>()
      for (id in order) {
        unlocked = unlocked + id
        val now = IslaPiezas.earned(unlocked)
        assertTrue("con «$id» se perdió alguna pieza: $before → $now", now.containsAll(before))
        before = now
      }
    }
  }

  // ---- Qué revelar en Hoy

  @Test
  fun la_primera_vez_no_se_revela_nada_y_se_siembra_todo_lo_que_ya_se_tiene() {
    val earned = IslaPiezas.earned(listOf("racha_7", "racha_30", "dominios"))
    assertTrue("seen = null (nunca se abrió Hoy con esta versión): nada «llueve»", IslaPiezas.plan(earned, null).isEmpty)
    assertEquals(setOf("c1", "c3", "v1"), IslaPiezas.consume(earned, null))
  }

  @Test
  fun una_pieza_nueva_se_revela_una_vez_y_se_anota() {
    val earned = IslaPiezas.earned(listOf("racha_7", "dominios", "vuelta_1"))
    val plan = IslaPiezas.plan(earned, setOf("c1"))
    assertEquals("las nuevas, en el orden de dibujo", listOf(IslaPieza.V1, IslaPieza.VO1), plan.reveal)
    val seen = IslaPiezas.consume(earned, setOf("c1"))
    assertEquals(setOf("c1", "v1", "vo1"), seen)
    assertTrue("al volver a Hoy no se repite", IslaPiezas.plan(earned, seen).isEmpty)
  }

  @Test
  fun lo_visto_nunca_se_olvida_aunque_la_pieza_ya_no_este_a_la_vista() {
    // El botón de depuración puede mostrar menos piezas; lo ya mostrado se queda anotado.
    assertEquals(setOf("c1", "c3"), IslaPiezas.consume(listOf(IslaPieza.C1), setOf("c1", "c3")))
  }

  // ---- Cómo se dicen

  @Test
  fun la_descripcion_nombra_las_piezas_sin_repetir() {
    assertEquals("", IslaPiezas.description(emptyList()))
    assertEquals(" · Tu isla tiene: dos arbustos, banderines y un muelle", IslaPiezas.description(listOf(IslaPieza.C1, IslaPieza.V1, IslaPieza.VO1)))
    assertEquals(" · Tu isla tiene: un muelle y una nave amarrada", IslaPiezas.description(listOf(IslaPieza.VO1, IslaPieza.VO2)))
    assertEquals(" · Tu isla tiene: banderines", IslaPiezas.description(listOf(IslaPieza.V1, IslaPieza.V2, IslaPieza.V3)))
    assertTrue(IslaPiezas.description(IslaPieza.entries).startsWith(" · Tu isla tiene: dos arbustos, flores, un árbol, frutos, banderines, un muelle"))
  }

  @Test
  fun la_linea_de_la_celebracion_dice_el_nombre_de_la_pieza() {
    assertEquals("Y en tu isla aparece: un muelle", IslaPiezas.announcement(IslaPieza.VO1))
    assertEquals("Y en tu isla aparece: banderines entre la Antena y la Biblioteca", IslaPiezas.announcement(IslaPieza.V1))
  }

  @Test
  fun los_nombres_no_usan_palabras_prohibidas_ni_hablan_de_perder() {
    val banned = listOf("cognitiv", "entren", "cerebro", "percentil", "mejora tu", "dominio", "estrella", "luna", "cruz", "perd")
    for (p in IslaPieza.entries) for (text in listOf(p.nombre, p.nombreCorto, IslaPiezas.announcement(p), IslaPiezas.description(listOf(p)))) {
      assertNull("«$text» usa una palabra que no va", banned.firstOrNull { it in text.lowercase() })
      assertNull("«$text»", NoLossGuard.hit(text, NoLossGuard.literals + NoLossGuard.reminderRoots))
    }
  }

  // ---- res/raw/isla.json dice lo mismo que el enum

  @Test
  fun isla_json_trae_las_diez_decoraciones_con_su_capa_y_su_ancla() {
    val layout = IslaLayout.parse(File("src/main/res/raw/isla.json").readText())
    assertEquals(IslaPieza.entries.map { it.id }.toSet(), layout.deco.keys)
    for (p in IslaPieza.entries) {
      val d = layout.deco.getValue(p.id)
      assertEquals("${p.id}: la capa del JSON es la del enum", p.capa.name.lowercase(), d.capa)
      assertTrue("${p.id}: el ancla cae dentro del recorte ${d.anchor}", d.anchor.x in 0f..1f && d.anchor.y in 0f..1f)
    }
  }
}
