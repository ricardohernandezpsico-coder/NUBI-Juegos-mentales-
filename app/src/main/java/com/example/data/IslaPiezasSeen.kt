package com.example.data

import android.content.Context

/** Dónde se guardan las piezas que Hoy ya mostró (las que no hay que volver a revelar). */
interface IslaPiezasStore {
  /** Los ids de las piezas ya vistas, o null si Hoy nunca se abrió con esta versión (la primera vez se siembra en silencio). */
  fun load(): Set<String>?
  fun save(ids: Set<String>)
}

/**
 * En preferencias propias y pasajeras (`isla_piezas`, clave `vistas`: no entra en `backup_rules.xml`; `BackupRulesTest` lo lista como pasajero).
 * Lo único que se pierde al cambiar de teléfono es «ya la vi»: Hoy siembra en silencio y las piezas aparecen puestas, sin animación.
 */
class PrefsIslaPiezasStore(context: Context) : IslaPiezasStore {
  private val prefs = context.applicationContext.getSharedPreferences("isla_piezas", Context.MODE_PRIVATE)

  override fun load(): Set<String>? = if (prefs.contains(KEY)) prefs.getStringSet(KEY, emptySet()).orEmpty().toSet() else null

  override fun save(ids: Set<String>) {
    prefs.edit().putStringSet(KEY, ids).apply()
  }

  companion object {
    const val FILE = "isla_piezas"
    private const val KEY = "vistas"
  }
}
