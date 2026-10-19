#!/usr/bin/env python3
"""Arma la PÁGINA DE ESCUCHA de sonido (Tarea 119, Fase A) a partir del catálogo que escribe el exportador de Unity.

Flujo (docs/sonido-madera-plan.md §7):
  1. En Unity (Editor): menú «NeuroVida/Sonido/Exportar página de escucha», o
       Unity -batchmode -projectPath unity/NeuroVidaCore -executeMethod NeuroVida.Bridge.EditorTools.SoundListeningExporter.ExportBatch
     Escribe un WAV por sonido en docs/previews/sonido-escucha/ y el catálogo en tools/sonido/catalogo.json. Los sonidos salen del MISMO C# que suena en
     los juegos (las clases XSounds, GameFeel y la paleta Games/Shared/WoodSounds.cs).
  2. Este script arma docs/previews/sonido-escucha.html:
       python tools/sonido/armar_escucha.py                 # la página liviana: pide los WAV de la carpeta sonido-escucha/ que está al lado
       python tools/sonido/armar_escucha.py --incrustar     # UN solo archivo con los WAV adentro (para el teléfono o para publicarla como página)
     Con --incrustar el archivo pesa unos MB: se escribe donde diga --salida (por defecto docs/previews/sonido-escucha-incrustado.html, que no va a git).
  3. POR FASE (Tarea 133 en adelante, spec auditoria-fable/spec-sonido-CDE.md §1.5): una página con SOLO los juegos de una fase, «hoy | Madera»:
       python tools/sonido/armar_escucha.py --fase C                       # docs/previews/sonido-escucha-C.html (liviana, va a git; pide los WAV de sonido-escucha/)
       python tools/sonido/armar_escucha.py --fase C --incrustar           # docs/previews/sonido-escucha-C-incrustado.html: UN solo archivo para el teléfono (no va a git)
     «Hoy» sale CONGELADO del 17-oct (tools/sonido/catalogo-hoy-17-oct.json: el exportador no lo renderiza); «Madera» sale de las mismas recetas que van al juego.
"""
import argparse
import base64
import io
import json
import os
import sys

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
CATALOGO = os.path.join(RAIZ, 'tools', 'sonido', 'catalogo.json')
CARPETA_WAV = os.path.join(RAIZ, 'docs', 'previews', 'sonido-escucha')

# (id del grupo, título, área)
GRUPOS = [
    ('comun', 'Lo común', 'todos los juegos'),
    ('parejas', 'Constelaciones', 'Memoria'),
    ('secuencia', 'Rastro de luz', 'Memoria'),
    ('bodega', 'Bodega de carga', 'Memoria'),
    ('correo', 'Correo Estelar', 'Memoria'),
    ('pista', 'La pista de Constelaciones', 'Memoria'),
    ('stroop', 'Tinta o Palabra', 'Atención'),
    ('piloto', 'Piloto Estelar', 'Atención'),
    ('freno', 'Freno de emergencia', 'Atención'),
    ('satelites', 'Satélites', 'Atención'),
    ('radar', 'Rescate relámpago', 'Atención · ya en Madera'),
    ('calculo', 'Carga exacta', 'Razonamiento'),
    ('engranajes', 'Engranajes', 'Razonamiento'),
    ('acoplamiento', 'Acoplamiento', 'Razonamiento · ya en Madera'),
    ('aterrizaje', 'Aterrizaje Lunar', 'Razonamiento · ya en Madera'),
    ('anagramas', 'En la punta de la lengua', 'Lenguaje'),
    ('meteoros', 'Lluvia de meteoros', 'Lenguaje'),
    ('disparate', '¿Verdad o disparate?', 'Lenguaje'),
    ('intrusa', 'La estrella intrusa', 'Lenguaje'),
]

# Las fases del plan (spec-sonido-CDE.md §7): qué juegos entran y cómo se presenta cada página
FASES = {
    'C': {
        'grupos': ['calculo', 'anagramas', 'parejas', 'bodega', 'correo'],
        'etiqueta': 'Tarea 133 · Fase C',
        'titulo': 'Fase C · hoy | Madera',
        'tag': 'Fase C: hoy y Madera cálida',
        'lead': 'Estos son los cinco juegos de la Fase C: <b>Carga exacta, En la punta de la lengua, Constelaciones, Bodega de carga y Correo Estelar</b>. El botón azul es cómo suena HOY (tal como sonaba el 17-oct); el amarillo, cómo sonará con «Madera cálida» (kalimba, marimba y bloque de madera con la sala pequeña de Rescate); donde una nota lleva información, el verde es la misma nota con marimba para comparar. Ningún juego está cableado todavía: aprueba de oído y yo conecto después.',
        'tip': 'Qué escuchar: (1) las <b>37 notas de memoria</b> en kalimba (11 de Constelaciones, 12 de Bodega, 10 de Punta, 4 de Correo): que se reconozca cada una y que se entienda el orden; (2) los <b>8 sonidos propios nuevos</b> (Bodega 5: llega la carga, el haz, abrir, volar y la matraca; Correo 3: la bocina, el saco y la radio); (3) tres cambios de criterio que decides tú: <b>quitar el destello</b> en Constelaciones, la <b>bocina de fondo</b> bajo la fanfarria del faro de Correo y la <b>pista</b> de las luces hermanas como «ayuda». Mejor con audífonos o con el parlante a volumen medio; los sonidos están al volumen real del juego. Para comparar solo el timbre, prende «Igualar volumen».',
    },
    'P': {
        'grupos': ['pista'],
        'archivo': 'pista',
        'etiqueta': 'Tareas 135 y 137 · La pista de Constelaciones: elegida la (a)',
        'titulo': 'La pista de Constelaciones: Ricardo eligió la (a)',
        'tag': 'La pista de Constelaciones: elegida la (a)',
        'lead': '<b>ELEGIDA por Ricardo (19-oct): la opción (a), ya cableada en el juego (Tarea 137).</b> Las otras dos quedan aquí solo como registro. <b>Qué hace la pista en el juego:</b> si abres una luz y después otra que NO es su pareja, un momento después las luces hermanas de la primera (las que ya habías visto) brillan un instante para que recuerdes dónde estaban. Hoy suenan dos campanitas muy suaves; el soplo de «Hint» de la paleta no te sonó a ayuda y no se cableó. Escucha las tres opciones: cada una tiene su botón y, al lado, «Hoy» para comparar con lo que suena ahora.',
        'tip': 'Las tres opciones suenan SUAVES a propósito (−6 dB; la pista es un aviso, no un acierto). En (a) y (b) cada luz hermana suena la nota de SU grupo en el momento en que brilla (el ejemplo tiene dos hermanas: dos notas); con una pareja es una sola nota y con un trío son dos. Mejor con audífonos o con el parlante a volumen medio.',
    },
    'D': {
        'grupos': ['secuencia', 'satelites', 'piloto', 'intrusa'],
        'etiqueta': 'Tarea 138 · Fase D (registro)',
        'titulo': 'Fase D · hoy | Madera (ya cableada)',
        'tag': 'Fase D: hoy y Madera cálida',
        'lead': '<b>Esta fase se cableó el 19-oct (Tarea 138) SIN pausa de escucha</b> (Ricardo: «hazlos con tu criterio»): la página queda como registro de lo que suena. Los cuatro juegos: <b>Rastro de luz, Satélites, Piloto Estelar y La estrella intrusa</b> (información fuerte, marimba). El botón azul es cómo sonaba HOY (congelado del 17-oct) y el amarillo, Madera: lo que ya suena en el juego (las notas de marimba con la misma altura, orden y momento de siempre, y los propios hechos con las mismas recetas del juego).',
        'tip': 'Qué escuchar si quieres revisarlo: (1) las <b>27 notas con información</b> en marimba (9 de Rastro, 5 campanas y 7 marcas de Satélites, 4 balizas de Piloto, 9 de Intrusa), con la kalimba al lado para comparar; (2) los 8 propios: los 4 avisos de modo de Rastro, la misión de Piloto (dos notas a 0,4 s), el grabado y la estrella fugaz de Intrusa; (3) los criterios: la ronda de Rastro es un Correct que sube con el largo del camino, el blip de Piloto es el mismo para todas las señales (−6 dB) y la estrella con palabra de Intrusa suena 6 dB más fuerte. Mejor con audífonos o con el parlante a volumen medio.',
    },
    'E': {
        'grupos': ['stroop', 'meteoros', 'freno', 'disparate', 'engranajes'],
        'etiqueta': 'Tarea 139 · Fase E (registro)',
        'titulo': 'Fase E · hoy | Madera (ya cableada)',
        'tag': 'Fase E: hoy y Madera cálida',
        'lead': '<b>Esta fase se cableó el 19-oct (Tarea 139) SIN pausa de escucha</b> (Ricardo: «hazlos con tu criterio»): la página queda como registro de lo que suena y CIERRA el plan de sonido «Madera cálida». Los cinco juegos: <b>Tinta o Palabra, Lluvia de meteoros, Freno de emergencia, ¿Verdad o disparate? y Engranajes</b> (señales y propios largos). El botón azul es cómo sonaba HOY (congelado del 17-oct) y el amarillo, Madera: lo que ya suena en el juego (las señales con la misma altura y duración de siempre, y los 14 propios de Engranajes de las mismas recetas del juego).',
        'tip': 'Qué escuchar si quieres revisarlo: (1) las <b>señales con altura</b> (las tres de Tinta, la salida de Freno y el 330 Hz de Meteoros): la misma altura y duración, el inicio llega antes; (2) los <b>14 propios de Engranajes</b>: los diez pasos de la cascada (la escalera), la antena, la turbina, la compuerta y el despegue; (3) lo <b>QUITADO</b> (el rugido y el resoplido de Freno, la onda y la estática de Disparate: «Madera: silencio, ya suena el acierto o el error») y lo que se queda como hoy (la sirena de ¡ALTO!). Mejor con audífonos o con el parlante a volumen medio.',
    },
}

PLANTILLA = r"""<!doctype html>
<html lang="es">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>__TITLE_TAG__</title>
<style>
:root{
  --bg:#07081F; --panel:#121236; --panel2:#1B1A4A; --line:#2C2A66; --ink:#EEEBFC; --dim:#ABA5D2; --faint:#8C86BD;
  --sky:#7FD8FF; --sun:#FFC94A; --lime:#A6E36B; --coral:#FF8A6B; --grape:#B79BFF; --inkd:#1A1240;
  --f-display:'Fredoka','Nunito',system-ui,sans-serif; --f-body:'Nunito',system-ui,sans-serif;
  color-scheme:dark;
}
*{box-sizing:border-box}
html,body{margin:0}
body{background:var(--bg);color:var(--ink);font:16px/1.5 var(--f-body)}
.wrap{max-width:980px;margin:0 auto;padding:24px 16px 80px}
h1{font:700 clamp(26px,5vw,38px)/1.1 var(--f-display);margin:0 0 8px}
.eyebrow{font:700 14px var(--f-display);letter-spacing:.08em;text-transform:uppercase;color:var(--sun);margin:0 0 4px}
.lead{color:var(--dim);margin:0 0 8px;max-width:68ch}
.tip{color:var(--faint);font-size:15px;margin:0 0 18px;max-width:68ch}
.bar{position:sticky;top:0;z-index:5;background:linear-gradient(var(--bg) 85%,transparent);padding:10px 0 12px;margin:0 -16px 8px;padding-inline:16px}
.bar .row{display:flex;flex-wrap:wrap;gap:0 16px;align-items:center}
.switch{display:flex;align-items:center;gap:10px;min-height:44px;cursor:pointer;font-weight:600;font-size:15px}
.switch input{width:22px;height:22px;accent-color:var(--sky)}
label.vol{display:flex;align-items:center;gap:10px;font-weight:600;font-size:15px}
input[type=range]{width:120px;accent-color:var(--sky)}
.now{min-height:22px;color:var(--sky);font:600 14px var(--f-display);margin-top:2px}
.chips{display:flex;gap:8px;overflow-x:auto;padding:4px 0 6px;scrollbar-width:thin}
.chip{flex:none;min-height:44px;padding:8px 14px;border-radius:999px;border:2px solid var(--line);background:var(--panel);color:var(--ink);font:600 14px var(--f-display);text-decoration:none;display:flex;align-items:center}
details.grupo{margin:14px 0;border:3px solid var(--line);border-radius:22px;background:var(--panel)}
details.grupo>summary{list-style:none;cursor:pointer;padding:14px 18px;min-height:56px;display:flex;flex-direction:column;justify-content:center}
details.grupo>summary::-webkit-details-marker{display:none}
details.grupo>summary .t{font:700 20px var(--f-display)}
details.grupo>summary .a{font-size:14px;color:var(--dim)}
.eventos{padding:0 12px 14px;display:grid;gap:10px}
.ev{background:var(--panel2);border:2px solid var(--line);border-radius:18px;padding:12px 14px}
.ev h3{font:600 17px/1.25 var(--f-display);margin:0 0 6px}
.tags{display:flex;flex-wrap:wrap;gap:6px;margin:0 0 8px}
.tag{font:700 12px var(--f-display);letter-spacing:.04em;text-transform:uppercase;padding:3px 9px;border-radius:999px;background:var(--line);color:var(--ink)}
.tag.info{background:var(--grape);color:var(--inkd)}
.tag.senal{background:var(--coral);color:var(--inkd)}
.tag.tiempo{background:var(--sky);color:var(--inkd)}
.tag.bucle{background:var(--faint);color:var(--inkd)}
.tag.nuevo{background:var(--lime);color:var(--inkd)}
.tag.elegida{background:var(--lime);color:var(--inkd)}
.btns{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:8px}
.btn{display:flex;flex-direction:column;align-items:flex-start;justify-content:center;gap:2px;min-height:64px;padding:8px 14px;border-radius:16px;border:3px solid var(--inkd);color:var(--inkd);font:700 17px var(--f-display);cursor:pointer;text-align:left;box-shadow:0 4px 0 #03041A}
.btn:active{transform:translateY(3px);box-shadow:0 1px 0 #03041A}
.btn small{font:600 12px/1.25 var(--f-body);opacity:.85}
.btn.hoy{background:var(--sky)}
.btn.madera{background:var(--sun)}
.btn.alt{background:var(--lime)}
.btn.ya{background:var(--grape)}
.btn.playing{outline:3px solid var(--ink);outline-offset:2px}
.mudo{display:flex;align-items:center;min-height:64px;padding:8px 14px;border-radius:16px;border:2px dashed var(--line);color:var(--dim);font:600 15px var(--f-display)}
.map{margin:8px 0 0;font-size:14px;color:var(--dim)}
.map b{color:var(--ink)}
.note{margin:6px 0 0;font-size:14px;color:var(--sun)}
details.mas{margin-top:6px}
details.mas>summary{cursor:pointer;color:var(--faint);font-size:14px;min-height:32px;display:flex;align-items:center}
.mas p{margin:4px 0;font-size:14px;color:var(--dim)}
.pie{color:var(--faint);font-size:14px;margin-top:28px;max-width:70ch}
:focus-visible{outline:3px solid var(--sky);outline-offset:2px}
@media (max-width:520px){.btns{grid-template-columns:1fr 1fr}}
</style>
</head>
<body>
<div class="wrap">
  <p class="eyebrow">__ETIQUETA__</p>
  <h1>__TITULO__</h1>
  __LEAD_Y_TIP__

  <div class="bar">
    <div class="row">
      <label class="switch"><input type="checkbox" id="igual"> Igualar volumen</label>
      <label class="vol">Volumen <input type="range" id="vol" min="0.1" max="1" step="0.05" value="1"></label>
    </div>
    <div class="now" id="now" aria-live="polite"></div>
    <nav class="chips" id="chips" aria-label="Juegos"></nav>
  </div>

  <div id="grupos"></div>

  <p class="pie">Etiquetas: <b>INFO</b> = la altura, el orden o el momento de la nota es información del juego (no se cambia, solo el timbre). <b>SEÑAL</b> = es parte del estímulo o de la instrucción. <b>TIEMPO</b> = va sincronizado con algo y no puede atrasarse. <b>BUCLE</b> = suena debajo del juego y se queda como está. <b>NUEVO</b> = hoy no suena nada. Los números de cada botón son el pico en dBFS y la sonoridad aproximada (LUFS, ventana de 400 ms) ya con el volumen que usa el juego. Detalle completo en docs/sonido-madera-plan.md.</p>
</div>

<script>
const DATA = __DATOS__;
const INCRUSTADO = __INCRUSTADO__;
const AUDIO = __AUDIO__;
const CARPETA = 'sonido-escucha/';

const $ = (s, r = document) => r.querySelector(s);
const fmt = (v, d = 1) => (v == null ? '—' : v.toFixed(d).replace('.', ','));
let ctx = null, current = null, currentBtn = null;
const buffers = new Map(), elements = new Map();

function url(file) { return INCRUSTADO ? AUDIO[file] : CARPETA + file; }

async function loadBuffer(file) {
  if (buffers.has(file)) return buffers.get(file);
  if (!ctx) { const C = window.AudioContext || window.webkitAudioContext; if (C) ctx = new C(); }
  if (ctx) {
    try {
      const r = await fetch(url(file));
      const ab = await r.arrayBuffer();
      const buf = await new Promise((ok, err) => ctx.decodeAudioData(ab, ok, err));
      buffers.set(file, buf);
      return buf;
    } catch (e) { /* si no se puede leer con fetch (archivo local), se usa un <audio> */ }
  }
  buffers.set(file, null);
  return null;
}

function stopAll() {
  if (current) { try { current.stop(); } catch (e) {} current = null; }
  for (const el of elements.values()) { el.pause(); }
  if (currentBtn) currentBtn.classList.remove('playing');
  currentBtn = null;
}

async function play(btn, key, gain, label) {
  stopAll();
  const igual = $('#igual').checked;
  const clip = DATA.clips[key], file = clip.file;
  const g = (igual ? 0.7 : gain * clip.scale) * parseFloat($('#vol').value);
  btn.classList.add('playing'); currentBtn = btn;
  $('#now').textContent = label;
  const buf = await loadBuffer(file);
  if (currentBtn !== btn) return;
  if (buf) {
    const src = ctx.createBufferSource(), gn = ctx.createGain();
    src.buffer = buf; gn.gain.value = g;
    src.connect(gn); gn.connect(ctx.destination);
    if (ctx.state === 'suspended') await ctx.resume();
    src.onended = () => { if (current === src) { btn.classList.remove('playing'); current = null; currentBtn = null; } };
    current = src; src.start();
  } else {
    let el = elements.get(file);
    if (!el) { el = new Audio(url(file)); elements.set(file, el); }
    el.volume = Math.max(0, Math.min(1, g)); el.currentTime = 0;
    el.onended = () => { btn.classList.remove('playing'); if (currentBtn === btn) currentBtn = null; };
    el.play();
  }
}

function levels(key, gain) {
  const c = DATA.clips[key]; if (!c) return '';
  const k = 20 * Math.log10(gain);
  return 'pico ' + fmt(c.peakDb + k) + ' dBFS · ' + fmt(c.lufs + k) + ' LUFS · inicio ' + fmt(c.onsetMs) + ' ms';
}

function button(cls, title, key, gain, extra) {
  const b = document.createElement('button');
  b.type = 'button'; b.className = 'btn ' + cls;
  b.innerHTML = '<span>' + title + '</span><small>' + levels(key, gain) + (extra ? ' · ' + extra : '') + '</small>';
  b.addEventListener('click', () => play(b, key, gain, title));
  return b;
}

function evento(e) {
  const d = document.createElement('div'); d.className = 'ev';
  const tags = [];
  if (e.flag) tags.push('<span class="tag ' + e.flag.replace('ñ', 'n') + '">' + e.flag + '</span>');
  if (e.note && e.note.indexOf('NUEVO') === 0) tags.push('<span class="tag nuevo">nuevo</span>');
  d.innerHTML = '<h3>' + e.title + '</h3>' + (tags.length ? '<div class="tags">' + tags.join('') + '</div>' : '');
  const btns = document.createElement('div'); btns.className = 'btns';
  if (!e.wood && e.noWood) {
    if (e.hoy) btns.appendChild(button('hoy', 'Hoy', e.hoy, e.hoyGain, ''));
    const m = document.createElement('div'); m.className = 'mudo'; m.textContent = e.noWood; btns.appendChild(m);
  } else if (!e.wood) {
    const m = document.createElement('div'); m.className = 'mudo';
    m.textContent = e.flag === 'bucle' ? 'Suena en bucle y se queda como está.' : (e.alreadyWood ? 'Ya suena en Madera cálida (aprobado): sin botón.' : 'Sonido propio del juego: se diseña en su fase.');
    btns.appendChild(m);
  } else {
    if (e.hoy) btns.appendChild(button('hoy', 'Hoy', e.hoy, e.hoyGain, ''));
    else { const m = document.createElement('div'); m.className = 'mudo'; m.textContent = 'Hoy: silencio'; btns.appendChild(m); }
    btns.appendChild(button('madera', e.woodLabel || 'Madera', e.wood, e.woodGain, ''));
    if (e.woodAlt) btns.appendChild(button('alt', e.woodAltLabel, e.woodAlt, e.woodGain, ''));
  }
  d.appendChild(btns);
  d.insertAdjacentHTML('beforeend', '<p class="map">' + (e.alreadyWood ? 'Ya suena en Madera cálida. ' : '') + 'Paleta: <b>' + e.map + '</b></p>');
  if (e.note) d.insertAdjacentHTML('beforeend', '<p class="note">' + e.note + '</p>');
  d.insertAdjacentHTML('beforeend', '<details class="mas"><summary>Cómo suena hoy y dónde se llama</summary><p>' + e.today + '</p><p>' + e.sites + '</p></details>');
  return d;
}

function armar() {
  const cont = $('#grupos'), chips = $('#chips');
  DATA.grupos.forEach((g, i) => {
    const evs = DATA.events.filter(e => e.group === g.id);
    if (!evs.length) return;
    const det = document.createElement('details'); det.className = 'grupo'; det.id = 'g-' + g.id;
    if (i === 0) det.open = true;
    det.innerHTML = '<summary><span class="t">' + g.title + '</span><span class="a">' + g.area + ' · ' + evs.length + ' sonidos</span></summary>';
    const box = document.createElement('div'); box.className = 'eventos';
    evs.forEach(e => box.appendChild(evento(e)));
    det.appendChild(box); cont.appendChild(det);
    const a = document.createElement('a'); a.className = 'chip'; a.href = '#g-' + g.id; a.textContent = g.title;
    a.addEventListener('click', () => { det.open = true; });
    chips.appendChild(a);
  });
}
armar();
</script>
</body>
</html>
"""


def cargar():
    with io.open(CATALOGO, encoding='utf-8') as f:
        return json.load(f)


def datos_pagina(cat, fase=None):
    """Lo mínimo que necesita la página: los grupos, los eventos y, por cada sonido, su archivo y sus niveles. Con `fase`, solo los juegos de esa fase (en su orden) y solo los sonidos que sus eventos nombran."""
    campos = ('group', 'id', 'title', 'today', 'sites', 'map', 'flag', 'note', 'hoy', 'hoyGain', 'wood', 'woodGain', 'woodAlt', 'woodAltLabel', 'alreadyWood', 'noWood', 'woodLabel')
    eventos = []
    for e in cat['events']:
        if fase and (e.get('phase') != fase or e['group'] not in FASES[fase]['grupos']):
            continue
        eventos.append({k: e.get(k, '') for k in campos})
    usados = set()
    for e in eventos:
        for k in ('hoy', 'wood', 'woodAlt'):
            if e[k]:
                usados.add(e[k])
    clips = {}
    for clave, c in cat['clips'].items():
        if c['file'] and (not fase or clave in usados):
            clips[clave] = {'file': c['file'], 'peakDb': c['peakDb'], 'lufs': c['lufs'], 'onsetMs': c['onsetMs'], 'scale': c['scale']}
    if fase:
        orden = {g: i for i, g in enumerate(FASES[fase]['grupos'])}
        grupos = sorted((g for g in GRUPOS if g[0] in orden), key=lambda g: orden[g[0]])
    else:
        grupos = GRUPOS
    return {'grupos': [{'id': g[0], 'title': g[1], 'area': g[2]} for g in grupos], 'events': eventos, 'clips': clips}


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--incrustar', action='store_true', help='mete los WAV adentro de la página (un solo archivo)')
    ap.add_argument('--salida', help='archivo de salida (por defecto docs/previews/sonido-escucha.html, o -incrustado si se incrusta; con --fase X, sonido-escucha-X.html)')
    ap.add_argument('--fase', choices=sorted(FASES), help='una página solo con los juegos de esa fase del plan (C, D o E): «hoy | Madera»')
    args = ap.parse_args()
    if not os.path.exists(CATALOGO):
        sys.exit('Falta tools/sonido/catalogo.json: corre primero el exportador en Unity (ver el encabezado de este script).')
    cat = cargar()
    datos = datos_pagina(cat, args.fase)
    if not datos['events']:
        sys.exit('No hay eventos de la fase %s en el catálogo.' % args.fase)
    audio = {}
    if args.incrustar:
        for archivo in sorted({c['file'] for c in datos['clips'].values()}):
            with open(os.path.join(CARPETA_WAV, archivo), 'rb') as f:
                audio[archivo] = 'data:audio/wav;base64,' + base64.b64encode(f.read()).decode('ascii')
    if args.fase:
        f = FASES[args.fase]
        encabezado = '<p class="lead">%s</p>\n  <p class="tip">%s</p>' % (f['lead'], f['tip'])
        etiqueta, titulo, tag = f['etiqueta'], f['titulo'], f['tag']
    else:
        encabezado = ('<p class="lead">Cada fila es algo que pasa en un juego. El botón azul es cómo suena HOY; el amarillo, cómo sonaría con la paleta «Madera cálida» (kalimba, marimba y bloque de madera con una sala pequeña: lo que aprobaste en Rescate).</p>\n'
                      '  <p class="tip">Mejor con audífonos o con el parlante a volumen medio. Para comparar solo el timbre, prende «Igualar volumen».</p>')
        etiqueta, titulo, tag = 'Tarea 119 · Fase A', 'Escucha: hoy y Madera cálida', 'Escucha: hoy y Madera cálida'
    html = (PLANTILLA
            .replace('__LEAD_Y_TIP__', encabezado).replace('__ETIQUETA__', etiqueta).replace('__TITULO__', titulo).replace('__TITLE_TAG__', tag)
            .replace('__DATOS__', json.dumps(datos, ensure_ascii=False, separators=(',', ':')))
            .replace('__INCRUSTADO__', 'true' if args.incrustar else 'false')
            .replace('__AUDIO__', json.dumps(audio, separators=(',', ':'))))
    sufijo = '-' + FASES[args.fase].get('archivo', args.fase) if args.fase else ''
    salida = args.salida or os.path.join(RAIZ, 'docs', 'previews', 'sonido-escucha%s%s.html' % (sufijo, '-incrustado' if args.incrustar else ''))
    with io.open(salida, 'w', encoding='utf-8', newline='\n') as f:
        f.write(html)
    peso = os.path.getsize(salida)
    archivos = {c['file'] for c in datos['clips'].values()}
    wavs = sum(os.path.getsize(os.path.join(CARPETA_WAV, a)) for a in archivos)
    print('%s: %.0f KB (%d eventos, %d archivos WAV, %.1f MB de audio)' % (os.path.relpath(salida, RAIZ), peso / 1024, len(datos['events']), len(archivos), wavs / 1048576))


if __name__ == '__main__':
    main()
