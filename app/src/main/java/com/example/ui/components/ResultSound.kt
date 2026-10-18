package com.example.ui.components

import android.content.Context
import android.media.MediaPlayer
import com.example.R

/**
 * El sonido de un récord en la app (Tarea 126): el `Record` de la paleta Madera (una escalera de kalimba y un acorde de marimba; `res/raw/madera_record.wav`, el mismo que se escucha en `docs/previews/sonido-escucha.html`). En Unity el récord se tapaba con la cortina del
 * final; ahora lo celebra la pantalla de resultado, que dice «¡Récord nuevo!» con su sello. Respeta «Efectos de sonido» de Ajustes (quien llama no lo suena con el sonido apagado). Si el dispositivo no puede reproducirlo, no pasa nada.
 */
object RecordSound {
  fun play(context: Context) {
    try {
      MediaPlayer.create(context.applicationContext, R.raw.madera_record)?.apply {
        setOnCompletionListener { it.release() }
        setOnErrorListener { mp, _, _ -> mp.release(); true }
        start()
      }
    } catch (_: Throwable) {
      // sin reproductor (pruebas, dispositivo sin audio): el récord se ve igual
    }
  }
}
