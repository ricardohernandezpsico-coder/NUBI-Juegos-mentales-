// Muestras de REFERENCIA del sonido «Madera cálida» de Rescate relámpago (Tarea 65), generadas con las recetas del laboratorio de sonido
// (docs/previews/sonido-laboratorio.html), sin descargar nada: se toma el código de las recetas del propio HTML y se corre en node.
// La prueba `RadarSoundFidelityTests` (EditMode) compara el C# (SoundKit + RadarSounds) con este archivo con tolerancia 1e-4.
//
// Uso:  node tools/sonido/referencia.js            -> escribe unity/NeuroVidaCore/Assets/Scripts/Games/Radar/Tests/RadarSoundReference.json
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const root = path.resolve(__dirname, '..', '..');
const html = fs.readFileSync(path.join(root, 'docs', 'previews', 'sonido-laboratorio.html'), 'utf8');
const a = html.indexOf('const SR = 44100');
const b = html.indexOf('// ---------- audio');
if (a < 0 || b < 0) throw new Error('no se encontró el código de las recetas en el laboratorio');
const recipes = html.slice(a, b);

// Las mismas funciones del laboratorio, más lo que sale de ellas: el sonido de madera ya con sala y cierre (lo mismo que hace `raw` del laboratorio).
const extra = `
function rawMadera(ev, arg){ const S = STYLES.madera, o = S.ev[ev](arg), v = { ...S.verb, ...(o.wet !== undefined ? { wet: o.wet } : {}), ...(o.tail ? { tail: o.tail } : {}) }; return finish(reverb(o.x, v), o.lvl); }
// El viaje a la estación no está en el laboratorio: «beam» con notas cada 0,05 s y una kalimba do6 a 0,45 s, nivel 0,3 (diseno-rescate-v4.md §5).
function rawTrip(){ const x = seq([261.63, 293.66, 329.63, 392, 440, 523.25, 587.33, 659.25], 0.05, f => marimba(f, 0.18), 1.2); add(x, kalimba(P[5]), 0.45, 0.6); return finish(reverb(x, { ...STYLES.madera.verb }), 0.3); }
function summary(st, head){ const [L, R] = st; let sl = 0, sr = 0, pk = 0; for (let i = 0; i < L.length; i++){ sl += Math.abs(L[i]); sr += Math.abs(R[i]); pk = Math.max(pk, Math.abs(L[i]), Math.abs(R[i])); }
  const out = { length: L.length, sumAbsL: sl, sumAbsR: sr, peak: pk, tailL: Array.from(L.slice(L.length - 40, L.length - 30), v => +v.toPrecision(8)) }; if (head) { out.headL = Array.from(L.slice(0, head), v => +v.toPrecision(8)); out.headR = Array.from(R.slice(0, 200), v => +v.toPrecision(8)); } return out; }
const r7 = rng(7), seedRng = []; for (let i = 0; i < 8; i++) seedRng.push(r7());
JSON.stringify({
  rng7: seedRng,
  mark1: summary(rawMadera('mark', 1), 2000),
  flash: summary(rawMadera('flash'), 2000),
  dock0: summary(rawMadera('dock', 0), 2000),
  ping: summary(rawMadera('ping')),
  beam: summary(rawMadera('beam')),
  perfect: summary(rawMadera('perfect')),
  finale: summary(rawMadera('finale')),
  trip: summary(rawTrip()),
});
`;
const result = vm.runInNewContext(recipes + '\n' + extra, { Math, Float32Array, Array, JSON });
const out = path.join(root, 'unity', 'NeuroVidaCore', 'Assets', 'Scripts', 'Games', 'Radar', 'Tests', 'RadarSoundReference.json');
fs.writeFileSync(out, result);
console.log('listo:', out, (result.length / 1024).toFixed(0) + ' KB');
