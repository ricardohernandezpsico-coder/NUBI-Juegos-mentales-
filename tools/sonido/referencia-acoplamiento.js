// Muestras de REFERENCIA del sonido «Madera cálida» de Acoplamiento (Tarea 68), generadas con las recetas del propio boceto aprobado (`SFX` de docs/previews/acoplamiento-boceto.html, versión 2), sin descargar nada: se toma el código de las recetas del HTML y se corre en node.
// La prueba `DockingSoundTests` (EditMode) compara el C# (SoundKit + RadarSounds + DockingSounds) con este archivo con tolerancia 1e-4.
//
// Uso:  node tools/sonido/referencia-acoplamiento.js   -> escribe unity/NeuroVidaCore/Assets/Scripts/Games/Acoplamiento/Tests/DockingSoundReference.json
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const root = path.resolve(__dirname, '..', '..');
const html = fs.readFileSync(path.join(root, 'docs', 'previews', 'acoplamiento-boceto.html'), 'utf8');
const rngLine = html.split('\n').find(l => l.startsWith('function rng('));
const a = html.indexOf('const SR = 44100');
const b = html.indexOf('const sfxCache');
if (!rngLine || a < 0 || b < 0) throw new Error('no se encontró el código de las recetas en el boceto');
const recipes = rngLine + '\n' + html.slice(a, b);

const extra = `
function summary(st, head){ const [L, R] = st; let sl = 0, sr = 0, pk = 0; for (let i = 0; i < L.length; i++){ sl += Math.abs(L[i]); sr += Math.abs(R[i]); pk = Math.max(pk, Math.abs(L[i]), Math.abs(R[i])); }
  const out = { length: L.length, sumAbsL: sl, sumAbsR: sr, peak: pk, tailL: Array.from(L.slice(L.length - 40, L.length - 30), v => +v.toPrecision(8)) };
  if (head){ out.headL = Array.from(L.slice(0, head), v => +v.toPrecision(8)); out.headR = Array.from(R.slice(0, head), v => +v.toPrecision(8)); }
  return out; }
const r7 = rng(7), seedRng = []; for (let i = 0; i < 8; i++) seedRng.push(r7());
JSON.stringify({
  rng7: seedRng,
  arrive: summary(SFX.arrive(), 2000),
  tap: summary(SFX.tap(), 2000),
  flip: summary(SFX.flip(), 2000),
  dock: summary(SFX.dock(), 2000),
  slot1: summary(SFX.slot(1), 2000),
  slot8: summary(SFX.slot(8)),
  ring: summary(SFX.ring(), 2000),
  miss: summary(SFX.miss(), 2000),
  finale: summary(SFX.finale()),
});
`;
const result = vm.runInNewContext(recipes + '\n' + extra, { Math, Float32Array, Array, JSON });
const out = path.join(root, 'unity', 'NeuroVidaCore', 'Assets', 'Scripts', 'Games', 'Acoplamiento', 'Tests', 'DockingSoundReference.json');
fs.writeFileSync(out, result);
console.log('listo:', out, (result.length / 1024).toFixed(0) + ' KB');
