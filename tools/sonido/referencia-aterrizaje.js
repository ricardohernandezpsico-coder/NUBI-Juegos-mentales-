// Muestras de REFERENCIA del sonido «Madera cálida» de Aterrizaje Lunar (Tarea 70), generadas con las recetas del propio boceto aprobado (`SFX` y `hoverOn` de docs/previews/aterrizaje-boceto.html, versión 4), sin descargar nada: se toma el código de las recetas del HTML y se corre en node.
// La prueba `LandingSoundTests` (EditMode) compara el C# (SoundKit + RadarSounds + LandingSounds) con este archivo con tolerancia 1e-4.
//
// Uso:  node tools/sonido/referencia-aterrizaje.js   -> escribe unity/NeuroVidaCore/Assets/Scripts/Games/Aterrizaje/Tests/LandingSoundReference.json
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const root = path.resolve(__dirname, '..', '..');
const html = fs.readFileSync(path.join(root, 'docs', 'previews', 'aterrizaje-boceto.html'), 'utf8');
const rngLine = html.split('\n').find(l => l.startsWith('function rng('));
const a = html.indexOf('const SR = 44100');
const b = html.indexOf('const sfxCache');
if (!rngLine || a < 0 || b < 0) throw new Error('no se encontró el código de las recetas en el boceto');
const recipes = rngLine + '\n' + html.slice(a, b);

// el bucle del propulsor (mientras baja), tal cual `hoverOn` del boceto, sin la parte de Web Audio
const extra = `
function hoverData(){ const lp = biquad('lp', 420, .7), r = rng(43); let b = 0; const x = render(6, () => { b = (b + .02*(r()*2 - 1))/1.02; return lp.run(b*3.5); });
  const N = 4*SR, F = 2*SR, out = x.slice(0, N); for (let i=0;i<F;i++){ const k = i/F; out[i] = x[i]*k + x[i + N]*(1 - k); } let pk = 1e-9; for (const v of out) pk = Math.max(pk, Math.abs(v)); for (let i=0;i<N;i++) out[i] *= .5/pk; return out; }
function summary(st, head){ const [L, R] = st; let sl = 0, sr = 0, pk = 0; for (let i = 0; i < L.length; i++){ sl += Math.abs(L[i]); sr += Math.abs(R[i]); pk = Math.max(pk, Math.abs(L[i]), Math.abs(R[i])); }
  const out = { length: L.length, sumAbsL: sl, sumAbsR: sr, peak: pk, tailL: Array.from(L.slice(L.length - 40, L.length - 30), v => +v.toPrecision(8)) };
  if (head){ out.headL = Array.from(L.slice(0, head), v => +v.toPrecision(8)); out.headR = Array.from(R.slice(0, head), v => +v.toPrecision(8)); }
  return out; }
function loopSummary(x){ let s = 0, pk = 0; for (let i = 0; i < x.length; i++){ s += Math.abs(x[i]); pk = Math.max(pk, Math.abs(x[i])); }
  return { length: x.length, sumAbs: s, peak: pk, head: Array.from(x.slice(0, 2000), v => +v.toPrecision(8)), mid: Array.from(x.slice(88200, 88300), v => +v.toPrecision(8)), tail: Array.from(x.slice(x.length - 40), v => +v.toPrecision(8)) }; }
const r7 = rng(7), seedRng = []; for (let i = 0; i < 8; i++) seedRng.push(r7());
JSON.stringify({
  rng7: seedRng,
  release: summary(SFX.release(), 2000),
  touch: summary(SFX.touch(), 2000),
  flag: summary(SFX.flag(), 2000),
  hit: summary(SFX.hit(), 2000),
  bull: summary(SFX.bull(), 2000),
  miss: summary(SFX.miss(), 2000),
  dome: summary(SFX.dome(), 2000),
  finale: summary(SFX.finale()),
  hover: loopSummary(hoverData()),
});
`;
const result = vm.runInNewContext(recipes + '\n' + extra, { Math, Float32Array, Array, JSON });
const out = path.join(root, 'unity', 'NeuroVidaCore', 'Assets', 'Scripts', 'Games', 'Aterrizaje', 'Tests', 'LandingSoundReference.json');
fs.writeFileSync(out, result);
console.log('listo:', out, (result.length / 1024).toFixed(0) + ' KB');
