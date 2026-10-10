// Muestras de REFERENCIA del motor «Cohete» de Piloto Estelar (Tarea 66), generadas con las recetas del laboratorio del motor
// (docs/previews/motor-laboratorio.html), sin descargar nada: se toma el código de las recetas del propio HTML y se corre en node.
// La prueba `PilotEngineTests` (EditMode) compara el C# (SoundKit + PilotSounds.MakeEngineLoop) con este archivo con tolerancia 1e-4.
//
// Uso:  node tools/sonido/referencia-motor.js      -> escribe unity/NeuroVidaCore/Assets/Scripts/Games/Piloto/Tests/PilotEngineReference.json
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const root = path.resolve(__dirname, '..', '..');
const html = fs.readFileSync(path.join(root, 'docs', 'previews', 'motor-laboratorio.html'), 'utf8');
const a = html.indexOf('const SR = 44100');
const b = html.indexOf('// ---------- audio');
if (a < 0 || b < 0) throw new Error('no se encontró el código de las recetas en el laboratorio del motor');
const recipes = html.slice(a, b);

// Las mismas funciones del laboratorio (ENGINES.cohete.cruise/fast/boost, que ya traen el bucle sin costura y el nivel) y un resumen de lo que sale de ellas.
const extra = `
function summary(x, head){ let s = 0, pk = 0; for (let i = 0; i < x.length; i++){ s += Math.abs(x[i]); pk = Math.max(pk, Math.abs(x[i])); }
  const out = { length: x.length, sumAbs: s, peak: pk, head: Array.from(x.slice(0, head), v => +v.toPrecision(9)), mid: Array.from(x.slice(88200, 88210), v => +v.toPrecision(9)), tail: Array.from(x.slice(x.length - 10), v => +v.toPrecision(9)) };
  return out; }
const r7 = rng(7), seedRng = []; for (let i = 0; i < 8; i++) seedRng.push(r7());
const br = brown(11), sm = smoothRand(12, 3), brownSeries = [], smoothSeries = []; for (let i = 0; i < 6; i++){ brownSeries.push(br()); smoothSeries.push(sm()); }
JSON.stringify({
  rng7: seedRng,
  brown11: brownSeries,
  smooth12_3: smoothSeries,
  cruise: summary(ENGINES.cohete.cruise(), 2000),
  fast: summary(ENGINES.cohete.fast(), 2000),
  boost: summary(ENGINES.cohete.boost(), 2000),
});
`;
const result = vm.runInNewContext(recipes + '\n' + extra, { Math, Float32Array, Array, JSON });
const out = path.join(root, 'unity', 'NeuroVidaCore', 'Assets', 'Scripts', 'Games', 'Piloto', 'Tests', 'PilotEngineReference.json');
fs.writeFileSync(out, result);
console.log('listo:', out, (result.length / 1024).toFixed(0) + ' KB');
