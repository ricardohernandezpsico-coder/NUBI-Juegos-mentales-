using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;
using Motion = NeuroVida.Games.Shared.Motion;

namespace NeuroVida.Games.Engranajes
{
    /// <summary>La máquina en pantalla y su animación por cuadro: armar y limpiar, el giro (todo junto: los dientes encajan siempre), las correas que se cruzan, los carteles con su veredicto, el pulso
    /// de luz, las piezas del cohete, lo que se marca al equivocarse, las luces de la cabecera, el despegue y las chispas.</summary>
    public sealed partial class EngranajesGameController
    {
        private readonly GearDef _introGearA = new GearDef { N = EngranajesContract.BigTeeth, Pitch = EngranajesContract.BigPitch, Tip = EngranajesContract.BigTip };
        private readonly GearDef _introGearB = new GearDef { X = 50f, N = EngranajesContract.SmallTeeth, Pitch = EngranajesContract.SmallPitch, Tip = EngranajesContract.SmallTip };
        private bool _sayHasSub = true;

        // posición de cada pieza y de su cartel, en coordenadas del boceto
        private static Vector2 PartPosition(Station s)
        {
            switch (s)
            {
                case Station.Antena: return new Vector2(306f, 110f);
                case Station.Compuerta: return new Vector2(312f, 250f);
                case Station.Carga: return new Vector2(312f, 316f);
                default: return new Vector2(306f, 442f);
            }
        }

        /// <summary>La altura del cartel de cada pieza (boceto). Su borde izquierdo queda justo donde terminan los dientes de los engranajes de la columna del cohete (273).</summary>
        private static float CartelY(Station s)
        {
            switch (s)
            {
                case Station.Antena: return 142f;
                case Station.Compuerta: return 294f;
                case Station.Carga: return 360f;
                default: return 398f;
            }
        }

        /// <summary>El centro de un cartel de ancho <paramref name="w"/>: pegado a los engranajes y sin pasarse del borde derecho de la pantalla (3 dp de margen) aunque la escena esté achicada.</summary>
        private float CartelX(float w)
        {
            float k = _lay.SceneScale <= 0f ? 1f : _lay.SceneScale;
            float maxEdge = 180f + (EngranajesLayout.W - 3f - (EngranajesLayout.W / 2f + EngranajesLayout.SceneShiftX)) / k;
            return Mathf.Min(273f + w / 2f, maxEdge - w / 2f);
        }

        // ------------------------------------------------------------------ armar y limpiar

        /// <summary>Pone una máquina en pantalla (sin contar nada): los engranajes, las correas con sus poleas, las barras, los ejes a las piezas, los carteles, la consigna y la cuenta de cambios.</summary>
        private void ShowMachine(Machine m)
        {
            ClearMachine();
            _mach = m;
            _sol = null;
            _results = null;
            _changes.Clear();
            _startRequested = false;
            _runAt = float.MaxValue;
            _verdictAt = float.MaxValue;
            _showFixAt = -10f;
            _okAnswer = false;
            _motorFlipAt = 0f;
            _beltFlipAt.Clear();
            _machineLayer.gameObject.SetActive(true);
            _lineCount = 0;

            // ejes de la antena y de la turbina hasta su pieza (primero, para que queden debajo)
            foreach (var kv in m.Stations)
            {
                if (kv.Key != Station.Antena && kv.Key != Station.Turbina) continue;
                var g = m.Gears[kv.Value];
                var p = PartPosition(kv.Key);
                var from = new Vector2(g.X, g.Y);
                var bend = new Vector2(p.x, g.Y);
                AddShaftSegment(from, bend, ShaftDark, 6f);
                AddShaftSegment(bend, p, ShaftDark, 6f);
                AddShaftSegment(from, bend, ShaftLight, 2.5f);
                AddShaftSegment(bend, p, ShaftLight, 2.5f);
            }
            // engranajes
            for (int i = 0; i < m.Gears.Count && i < GearPool; i++) PlaceGear(_gears[i], m.Gears[i], EngranajesSprites.PaletteOf(m.Gears[i], i));
            // barras dentadas, a la derecha del engranaje de la compuerta y del de la carga
            _rackOff.Clear();
            for (int r = 0; r < _racks.Length; r++)
            {
                var st = r == 0 ? Station.Compuerta : Station.Carga;
                bool on = m.Stations.TryGetValue(st, out int gi);
                _racks[r].gameObject.SetActive(on);
                if (!on) continue;
                var g = m.Gears[gi];
                _racks[r].rectTransform.sizeDelta = new Vector2(20f * Su, 84f * Su);
                _racks[r].rectTransform.anchoredPosition = B(g.X + g.Pitch + 7.5f, g.Y);
            }
            // correas: las poleas en los ejes de los extremos; las tiras se dibujan cada cuadro (se cruzan al tocarlas)
            _beltLinks.Clear();
            for (int i = 0; i < m.Links.Count; i++) if (m.Links[i].Type == LinkType.Belt && _beltLinks.Count < BeltPool) _beltLinks.Add(i);
            for (int b = 0; b < BeltPool; b++)
            {
                bool on = b < _beltLinks.Count;
                var bv = _belts[b];
                foreach (var l in bv.Lines) l.Img.gameObject.SetActive(on);
                foreach (var c in bv.Caps) c.gameObject.SetActive(on);
                foreach (var pu in bv.Pulleys) pu.gameObject.SetActive(on);
                if (!on) continue;
                var link = m.Links[_beltLinks[b]];
                bv.Pulleys[0].rectTransform.anchoredPosition = B(m.Gears[link.A].X, m.Gears[link.A].Y);
                bv.Pulleys[1].rectTransform.anchoredPosition = B(m.Gears[link.B].X, m.Gears[link.B].Y);
            }
            // el motor y su flecha
            var g0 = m.Gears[0];
            float rr = g0.Tip + 8f;
            _motorArrowRect.anchoredPosition = B(g0.X, g0.Y);
            _motorArrowRect.sizeDelta = Vector2.one * (2f * (rr + 14f) * Su);
            _motorArrowRect.localEulerAngles = Vector3.zero;
            _motorArrowDir = 0;                                        // fuerza a poner el sprite en el primer cuadro
            _motorArrow.gameObject.SetActive(true);
            _motorPill.Root.anchoredPosition = B(g0.X, g0.Y + 16f);
            _motorPill.Root.gameObject.SetActive(true);
            _motorPill.Root.sizeDelta = new Vector2(62f * Su, 24f * Su);
            SetRadius(_motorPill.Bg, 12f * Su);
            _motorPulse.rectTransform.anchoredPosition = B(g0.X, g0.Y + 16f);
            _motorPulse.rectTransform.sizeDelta = new Vector2(72f * Su, 34f * Su);
            SetRadius(_motorPulse, 17f * Su);
            // piezas y carteles: solo las piezas activas llevan cartel; las demás se ven atenuadas
            for (int s = 0; s < 4; s++)
            {
                var st = (Station)s;
                bool active = m.Stations.ContainsKey(st);
                _partGroups[s].alpha = active ? 1f : 0.45f;
                var cv = _cartels[s];
                cv.Root.gameObject.SetActive(active);
                if (active) SetUpCartel(cv, st, m.Mission[st]);
            }
            ResetParts();
            // consigna y cuenta de cambios
            var consigna = EngranajesContract.Consigna(m.Stage);
            _consigna1.text = consigna[0];
            _consigna2.text = consigna[1];
            _questionBar.gameObject.SetActive(true);
            _counterText.text = m.Stage.Keys == 1 ? "1 cambio" : "2 cambios";
            _counterRoot.gameObject.SetActive(true);
            _start.Root.gameObject.SetActive(true);
            LayoutBottom();
            _appearAt = GameClock.Time;
        }

        private void ClearMachine()
        {
            _mach = null;
            _sol = null;
            _results = null;
            _changes.Clear();
            _runAt = float.MaxValue;
            _verdictAt = float.MaxValue;
            _showFixAt = -10f;
            _okAnswer = false;
            _startRequested = false;
            if (_machineLayer == null) return;
            foreach (var g in _gears) g.Root.gameObject.SetActive(false);
            foreach (var l in _lines) l.Img.gameObject.SetActive(false);
            foreach (var r in _racks) r.gameObject.SetActive(false);
            foreach (var bv in _belts)
            {
                foreach (var l in bv.Lines) l.Img.gameObject.SetActive(false);
                foreach (var c in bv.Caps) c.gameObject.SetActive(false);
                foreach (var pu in bv.Pulleys) pu.gameObject.SetActive(false);
            }
            foreach (var c in _cartels) c.Root.gameObject.SetActive(false);
            _motorPill.Root.gameObject.SetActive(false);
            _motorPulse.gameObject.SetActive(false);
            foreach (var p in _pulses) p.gameObject.SetActive(false);
            _motorArrow.gameObject.SetActive(false);
            _questionBar.gameObject.SetActive(false);
            _counterRoot.gameObject.SetActive(false);
            _start.Root.gameObject.SetActive(false);
            for (int s = 0; s < 4; s++) if (_partGroups[s] != null) _partGroups[s].alpha = 0.45f;
            ResetParts();
            _machineLayer.gameObject.SetActive(false);
        }

        /// <summary>Las piezas del cohete vuelven a su reposo: portón cerrado, elevador abajo, radar y turbina sin girar, sin llama ni luz.</summary>
        private void ResetParts()
        {
            _doorRt.anchoredPosition = new Vector2(4f * Su, 6f * Su);
            _cargoLiftRt.anchoredPosition = new Vector2(4f * Su, -2f * Su);
            _dish.rectTransform.localEulerAngles = Vector3.zero;
            _fan.rectTransform.localEulerAngles = Vector3.zero;
            _gateGlow.color = new Color(Gold.r, Gold.g, Gold.b, 0f);
            _turbineFlame.gameObject.SetActive(false);
            _rackOff.Clear();
        }

        private void PlaceGear(GearView v, GearDef g, EngranajesSprites.Palette palette)
        {
            var size = EngranajesSprites.SizeOf(g);
            v.Tip = g.Tip;
            v.Root.anchoredPosition = B(g.X, g.Y);
            v.Root.sizeDelta = Vector2.zero;
            v.Body.sprite = EngranajesSprites.Gear(size, palette);
            v.Shadow.sprite = EngranajesSprites.Silhouette(size);
            v.Hub.sprite = EngranajesSprites.Hub();
            SizeGearParts(v, EngranajesSprites.GearBox(size), g.Tip, Su);
            v.Glow.color = new Color(1f, 214f / 255f, 120f / 255f, 0f);
            ApplyAngle(v, g.A);
            v.Root.gameObject.SetActive(true);
        }

        private static void ApplyAngle(GearView v, float radians)
        {
            var e = new Vector3(0f, 0f, -radians * Mathf.Rad2Deg);       // el ángulo del boceto es en el sentido del reloj: en la interfaz el giro positivo es al revés
            v.Body.rectTransform.localEulerAngles = e;
            v.Shadow.rectTransform.localEulerAngles = e;
        }

        private LineView NextLine()
        {
            var lv = _lines[Mathf.Min(_lineCount, _lines.Length - 1)];
            _lineCount++;
            return lv;
        }

        private static void SetLine(LineView lv, Vector2 a, Vector2 b, float widthDp, Color color)
        {
            var mid = (a + b) * 0.5f;
            float len = Vector2.Distance(a, b);
            lv.Rt.anchoredPosition = B(mid.x, mid.y);
            lv.Rt.sizeDelta = new Vector2((len + widthDp) * Su, widthDp * Su);
            float ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            lv.Rt.localEulerAngles = new Vector3(0f, 0f, -ang);
            lv.Img.color = color;
            SetRadius(lv.Img, widthDp * Su * 0.5f);
            lv.Img.gameObject.SetActive(true);
        }

        private void AddShaftSegment(Vector2 a, Vector2 b, Color c, float w)
        {
            if (Vector2.Distance(a, b) < 0.01f) return;
            SetLine(NextLine(), a, b, w, c);
        }

        // ------------------------------------------------------------------ correas

        /// <summary>El estado visible de una correa (0 = recta, 1 = cruzada), animado al tocarla (0,32 s; con «quitar animaciones», al instante).</summary>
        private float BeltK(int link, float now)
        {
            int target = BeltCrossed(link) ? 1 : 0;
            if (!_beltFlipAt.TryGetValue(link, out float at) || at <= 0f) return target;
            float k = Motion.Decorative ? Ease((now - at) / 0.32f) : 1f;
            return target == 1 ? k : 1f - k;
        }

        private bool BeltCrossed(int link) => _mach.Links[link].Crossed != _changes.Contains(link);

        private static float Ease(float t) => t < 0f ? 0f : t > 1f ? 1f : 1f - Mathf.Pow(1f - t, 3f);

        /// <summary>Una correa: una tira recta a cada lado de las poleas y una media vuelta alrededor de cada una; recta = óvalo, cruzada = un ocho. <paramref name="k"/> va de 0 (recta) a 1 (cruzada).
        /// <paramref name="hot"/> = brillo dorado (la pista o la solución), <paramref name="gold"/> = ya está cambiada (la banda clara pasa a dorado).</summary>
        private void DrawBelt(BeltView bv, GearDef a, GearDef b, float k, float hot, bool gold)
        {
            float ang = Mathf.Atan2(b.Y - a.Y, b.X - a.X), n = ang + Mathf.PI / 2f, r = EngranajesContract.PulleyRadius;
            var nv = new Vector2(Mathf.Cos(n), Mathf.Sin(n));
            var pa = new Vector2(a.X, a.Y);
            var pb = new Vector2(b.X, b.Y);
            for (int layer = 0; layer < 3; layer++)
            {
                float w = EngranajesSprites.BeltWidths[layer];
                Color col = layer == 0 ? new Color(Gold.r, Gold.g, Gold.b, hot) : layer == 1 ? BeltDark : (gold ? Gold : BeltLight);
                bool show = layer != 0 || hot > 0.01f;
                for (int s = 0; s < 2; s++)
                {
                    float sgn = s == 0 ? 1f : -1f;
                    float sb = sgn * (1f - 2f * k);
                    var lv = bv.Lines[layer * 2 + s];
                    var cap = bv.Caps[layer * 2 + s];
                    lv.Img.gameObject.SetActive(show);
                    cap.gameObject.SetActive(show);
                    if (!show) continue;
                    SetLine(lv, pa + nv * r * sgn, pb + nv * r * sb, w, col);
                    // la media vuelta de cada extremo (los dos lados de una capa comparten una: el índice 0 va en A y el 1 en B)
                    var center = s == 0 ? pa : pb;
                    cap.rectTransform.anchoredPosition = B(center.x, center.y);
                    cap.rectTransform.localEulerAngles = new Vector3(0f, 0f, -ang * Mathf.Rad2Deg + (s == 0 ? 0f : 180f));
                    cap.color = col;
                }
            }
        }

        // ------------------------------------------------------------------ carteles

        /// <summary>El contenido de un cartel: el nombre y, abajo, el ícono con la palabra de lo que debe hacer. Las medidas dependen del texto (se recalculan en <see cref="Layout"/>).</summary>
        private void SetUpCartel(CartelView c, Station st, Act act)
        {
            c.Name.text = EngranajesContract.StationLabel(st);
            c.Word.text = EngranajesContract.MissionWord(act);
            c.Icon.sprite = st == Station.Antena || st == Station.Turbina
                ? EngranajesSprites.MiniArrow(act == Act.Cw)
                : EngranajesSprites.GlyphSprite(act == Act.Up || act == Act.Open ? EngranajesSprites.Glyph.Up : EngranajesSprites.Glyph.Down);
            c.Icon.rectTransform.sizeDelta = Vector2.one * ((st == Station.Antena || st == Station.Turbina ? 22f : 26f) * Su);
            LayoutCartel(c, st);
        }

        /// <summary>Mide el texto de un cartel y lo acomoda: ancho (96 en el boceto; crece si el texto de 14 dp no cabe con la escena achicada) sin salirse de la escena por la derecha.</summary>
        private void LayoutCartel(CartelView c, Station st)
        {
            float k = _lay.SceneScale <= 0f ? 1f : _lay.SceneScale;
            float nameW = c.Name.preferredWidth / Su;
            float wordW = c.Word.preferredWidth / Su;
            float iconW = 18f;
            float need = Mathf.Max(nameW + 24f, wordW + iconW) + 16f;       // el nombre deja lugar al ✓ de la derecha
            float w = Mathf.Max(CartelW, need);
            c.Root.anchoredPosition = B(CartelX(w), CartelY(st));
            c.Root.sizeDelta = new Vector2(w * Su, CartelH * Su);
            SetChildScene(c.Rim.rectTransform, 0f, 0f, w + 3f, CartelH + 3f);
            SetChildScene(c.Bg.rectTransform, 0f, 0f, w, CartelH);
            SetRadius(c.Rim, 13f * Su);
            SetRadius(c.Bg, 12f * Su);
            SetChildScene(c.Name.rectTransform, 0f, -9f, w, 16f / k);
            float group = wordW + iconW;
            float ix = -group / 2f + 6f;                                                  // la flechita o el triángulo, a la izquierda del grupo
            c.Word.rectTransform.sizeDelta = new Vector2((wordW + 4f) * Su, 16f / k * Su);
            c.Word.rectTransform.anchoredPosition = new Vector2((ix + 10f + (wordW + 4f) / 2f) * Su, -9.5f * Su);
            c.Icon.rectTransform.anchoredPosition = new Vector2(ix * Su, -9.5f * Su);
            c.Check.rectTransform.anchoredPosition = new Vector2((w / 2f - 11f) * Su, 10f * Su);
        }

        /// <summary>Coloca una pieza respecto del centro de su padre en unidades de la escena (dp del boceto; y hacia abajo).</summary>
        private static void SetChildScene(RectTransform r, float dx, float dy, float w, float h)
        {
            r.sizeDelta = new Vector2(w * Su, h * Su);
            r.anchoredPosition = new Vector2(dx * Su, -dy * Su);
        }

        private void AnimateCartels(float now, bool deco)
        {
            if (_mach == null) return;
            for (int s = 0; s < 4; s++)
            {
                var c = _cartels[s];
                if (!c.Root.gameObject.activeSelf) continue;
                var st = (Station)s;
                int idx = System.Array.IndexOf(_mach.Targets, st);
                bool done = _phase == Phase.Reveal && _results != null && idx >= 0 && now >= _verdictAt + idx * 0.12f;
                bool ok = done && _results[idx].Ok, bad = done && !_results[idx].Ok;
                float shake = bad && deco ? Mathf.Sin((now - _verdictAt) * 33f) * Mathf.Max(0f, 1f - (now - _verdictAt) / 0.7f) * 4f : 0f;
                var rt = c.Root;
                float w = rt.sizeDelta.x / Su;
                rt.anchoredPosition = B(CartelX(w) + shake, CartelY(st));
                c.Bg.color = ok ? CartelOkFill : bad ? CartelBadFill : CartelFill;
                c.Rim.color = ok ? Mint : bad ? Coral : Gold;
                var col = ok ? Mint : bad ? CartelBadText : Gold;
                c.Word.color = col;
                c.Icon.color = col;
                c.Check.gameObject.SetActive(ok);
            }
        }

        // ------------------------------------------------------------------ giro y cascada

        private void CaptureAngles()
        {
            for (int i = 0; i < _mach.Gears.Count && i < _baseA.Length; i++) _baseA[i] = _mach.Gears[i].A;
        }

        /// <summary>∫ de la rampa de aceleración: el giro arranca suave y llega a su velocidad en <see cref="Accel"/> s.</summary>
        private static float Ramp(float t)
        {
            if (t <= 0f) return 0f;
            return t < Accel ? t * t / (2f * Accel) : Accel / 2f + (t - Accel);
        }

        private bool IsFixing => _showFixAt > 0f && _phase == Phase.Reveal && !_okAnswer;

        private void AnimateScene(float now, float dt)
        {
            if (!_sceneRoot.gameObject.activeSelf) return;
            bool deco = Motion.Decorative;
            for (int k = 0; k < _windows.Length; k++)
            {
                bool on = k < _shownLights;
                _windows[k].color = on ? Gold : WindowOff;
                _windowGlows[k].gameObject.SetActive(on && deco);
            }
            float appear = Mathf.Clamp01((now - _appearAt) / (deco ? 0.3f : Motion.FadeSeconds));
            _machineGroup.alpha = appear;
            _questionGroup.alpha = appear;
            AnimateRocket(now);
            if (_mach == null) return;

            var m = _mach;
            bool running = _sol != null && now >= _runAt;
            float run = running ? now - _runAt : 0f;
            float step = EngranajesContract.StepSeconds(_senior);

            // giro: todo junto, cada engranaje a su velocidad (los dientes encajan siempre)
            if (running && deco)
            {
                float R = Ramp(run);
                for (int i = 0; i < m.Gears.Count && i < GearPool; i++)
                    if (_sol.Depth[i] >= 0) m.Gears[i].A = _baseA[i] + Spin * _sol.Speed[i] * R;
            }
            var fixHot = new HashSet<int>();
            bool fixing = IsFixing;
            if (fixing) foreach (int id in _changes) fixHot.UnionWith(EngranajesContract.Downstream(m, id));
            for (int i = 0; i < m.Gears.Count && i < GearPool; i++)
            {
                var g = m.Gears[i];
                var v = _gears[i];
                ApplyAngle(v, g.A);
                // el destello cuando la fuerza llega a ese engranaje (la cascada: un paso cada 0,15 s); al equivocarse, en celeste lo que movió tu cambio
                float glow = 0f;
                Color glowColor = new Color(1f, 214f / 255f, 120f / 255f);
                if (running && deco && _sol.Depth[i] >= 0)
                {
                    float k = (run - _sol.Depth[i] * step) / 0.5f;
                    if (k > 0f && k < 1f) glow = Mathf.Sin(k * Mathf.PI) * 0.55f;
                }
                if (fixing && fixHot.Contains(i)) { glow = 0.5f; glowColor = Cyan; }
                v.Glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, glow);
                v.Glow.rectTransform.sizeDelta = Vector2.one * ((fixing && fixHot.Contains(i) ? g.Tip * 2f + 24f : g.Tip * 3f) * Su);
            }

            AnimateMotor(m, now, deco, fixing);
            AnimateBelts(m, now, deco, fixing);
            AnimateParts(m, running, run, step, deco);
            AnimatePulses(m, running, run, step, deco);
            AnimateCartels(now, deco);
        }

        /// <summary>El motor: su flecha de giro (se da vuelta al tocarlo con un giro de 0,3 s y queda dorada) y el rótulo «MOTOR» (que late en la pista y en la solución).</summary>
        private void AnimateMotor(Machine m, float now, bool deco, bool fixing)
        {
            var g0 = m.Gears[0];
            bool changed = _changes.Contains(EngranajesContract.MotorId);
            int md = m.MotorDir * (changed ? -1 : 1);
            float fk = _motorFlipAt > 0f && deco ? Mathf.Min(1f, (now - _motorFlipAt) / 0.3f) : 1f;
            int shown = fk < 0.5f ? -md : md;                       // a mitad del giro la flecha cambia de sentido
            if (shown != _motorArrowDir)
            {
                _motorArrowDir = shown;
                _motorArrow.sprite = EngranajesSprites.Arrow(g0.Tip + 8f, 4f, shown > 0);
            }
            _motorArrowRect.localScale = new Vector3(fk < 0.5f ? 1f - 2f * fk : 2f * fk - 1f, 1f, 1f);
            _motorArrow.color = changed ? Gold : Cyan;
            _motorPill.Bg.color = changed ? Gold : ButtonFill;
            _motorPill.Label.color = changed ? NeuroStyle.Ink : ButtonText;
            float pulse = 0f;
            if (fixing && System.Array.IndexOf(m.Solution, EngranajesContract.MotorId) >= 0) pulse = 0.55f + 0.45f * Mathf.Sin((now - _showFixAt) * 5.9f);
            else if (_phase == Phase.Play && deco && m.Stage.Hint == Hint.Motor && now - _hintAt < 2.6f) pulse = 0.35f + 0.3f * Mathf.Sin(now * 4.5f);
            _motorPulse.gameObject.SetActive(pulse > 0.01f);
            _motorPulse.color = new Color(Gold.r, Gold.g, Gold.b, pulse);
        }

        private void AnimateBelts(Machine m, float now, bool deco, bool fixing)
        {
            float idle = _phase == Phase.Play && deco && m.Stage.Hint != Hint.Motor && now - _hintAt < 2.6f ? 0.25f + 0.2f * Mathf.Sin(now * 4.5f) : 0f;
            for (int b = 0; b < _beltLinks.Count; b++)
            {
                int li = _beltLinks[b];
                var link = m.Links[li];
                float hot = idle;
                if (fixing && System.Array.IndexOf(m.Solution, li) >= 0) hot = 0.55f + 0.45f * Mathf.Sin((now - _showFixAt) * 5.9f);
                DrawBelt(_belts[b], m.Gears[link.A], m.Gears[link.B], BeltK(li, now), hot, _changes.Contains(li));
            }
        }

        private void AnimateParts(Machine m, bool running, float run, float step, bool deco)
        {
            // radar y turbina giran con el engranaje de su pieza
            if (m.Stations.TryGetValue(Station.Antena, out int ia)) _dish.rectTransform.localEulerAngles = new Vector3(0f, 0f, -m.Gears[ia].A * Mathf.Rad2Deg);
            float flame = 0f;
            if (m.Stations.TryGetValue(Station.Turbina, out int it))
            {
                _fan.rectTransform.localEulerAngles = new Vector3(0f, 0f, -m.Gears[it].A * Mathf.Rad2Deg);
                if (running && deco && _sol.Depth[it] >= 0)
                    flame = Mathf.Clamp01((run - _sol.Depth[it] * step) / Accel) * Mathf.Min(1f, Mathf.Abs(_sol.Speed[it]));       // la llama llega con la fuerza
            }
            _turbineFlame.gameObject.SetActive(flame > 0.01f);
            _turbineFlame.color = new Color(Coral.r, Coral.g, Coral.b, flame);
            // la barra de la compuerta y la del elevador siguen a su engranaje
            _rackOff.Clear();
            foreach (var st in new[] { Station.Compuerta, Station.Carga })
            {
                if (!m.Stations.TryGetValue(st, out int gi)) continue;
                float off = 0f;
                if (running && _sol.Depth[gi] >= 0)
                {
                    var g = m.Gears[gi];
                    float t = Mathf.Max(0f, run - _sol.Depth[gi] * step);
                    off = deco ? Spin * _sol.Speed[gi] * g.Pitch * Mathf.Max(0f, t - 0.15f) * 0.9f : (_sol.Dir[gi] > 0 ? 16f : -40f);
                }
                _rackOff[st] = off;
            }
            // las barras dentadas acompañan a su pieza
            for (int r = 0; r < _racks.Length; r++)
            {
                var st = r == 0 ? Station.Compuerta : Station.Carga;
                if (!m.Stations.TryGetValue(st, out int gi)) continue;
                var g = m.Gears[gi];
                float off = Mathf.Clamp(_rackOff.TryGetValue(st, out float ro) ? ro : 0f, -40f, 18f);
                _racks[r].rectTransform.anchoredPosition = B(g.X + g.Pitch + 7.5f, g.Y + off);
            }
            float gate = Mathf.Clamp(_rackOff.TryGetValue(Station.Compuerta, out float go) ? go : 0f, -44f, 10f);
            _doorRt.anchoredPosition = new Vector2(4f * Su, (6f - gate) * Su);
            float lift = Mathf.Clamp(_rackOff.TryGetValue(Station.Carga, out float lo) ? lo : 0f, -26f, 18f);
            _cargoLiftRt.anchoredPosition = new Vector2(4f * Su, (-2f - lift) * Su);
            float open = deco && m.Stations.ContainsKey(Station.Compuerta) ? Mathf.Max(0f, -gate / 40f) : 0f;
            _gateGlow.color = new Color(Gold.r, Gold.g, Gold.b, open);
        }

        /// <summary>El pulso de luz que recorre cada unión, de a un paso por cada 0,15 s (con «quitar animaciones» no hay).</summary>
        private void AnimatePulses(Machine m, bool running, float run, float step, bool deco)
        {
            int used = 0;
            if (running && deco)
            {
                foreach (var l in m.Links)
                {
                    int da = _sol.Depth[l.A], db = _sol.Depth[l.B];
                    if (da < 0 || db < 0 || used >= _pulses.Length) continue;
                    int i = da < db ? l.A : l.B, j = da < db ? l.B : l.A;
                    float k = (run - Mathf.Min(da, db) * step) / step;
                    if (k < 0f || k > 1.4f) continue;
                    var a = m.Gears[i];
                    var b = m.Gears[j];
                    float e = Mathf.Min(1f, k);
                    var img = _pulses[used++];
                    img.rectTransform.anchoredPosition = B(a.X + (b.X - a.X) * e, a.Y + (b.Y - a.Y) * e);
                    img.color = new Color(1f, 214f / 255f, 120f / 255f, Mathf.Max(0f, 1f - (k - 1f) / 0.4f));
                    img.gameObject.SetActive(true);
                }
            }
            for (int i = used; i < _pulses.Length; i++) _pulses[i].gameObject.SetActive(false);
        }

        private void AnimateRocket(float now)
        {
            bool launching = _phase == Phase.Launch;
            if (launching)
            {
                bool deco = Motion.Decorative;
                float t = deco ? Mathf.Min(1f, (now - _launchAt) / 3.2f) : 1f;
                float lift = Mathf.Pow(Mathf.Max(0f, (t - 0.25f) / 0.75f), 2f) * 620f;
                float shake = deco && t < 0.25f ? Mathf.Sin(now * 40f) * 2f : 0f;
                _rocketRoot.anchoredPosition = new Vector2(shake * Su, lift * Su);
                _flame.gameObject.SetActive(deco);
                if (deco)
                {
                    _flame.color = new Color(1f, 180f / 255f, 90f / 255f, 0.95f);
                    _flame.rectTransform.sizeDelta = new Vector2(72f * Su, (80f + Mathf.Sin(now * 31f) * 10f + 10f) * Su);
                    var nozzle = L(306f + shake, 444f - lift);
                    for (int j = 0; j < 2; j++) Emit(nozzle + new Vector2((float)(_rng.NextDouble() - 0.5) * 20f, 0f), 2, 70f, 0.8f, j % 2 == 0 ? Gold : Coral, -40f);
                }
                // el rocket entero se va: sin él no hay nada que mostrar al final (con «quitar animaciones» ya no está)
                return;
            }
            _rocketRoot.anchoredPosition = Vector2.zero;
            _flame.gameObject.SetActive(false);
        }

        private void AnimateStrip(float now)
        {
            for (int k = 0; k < _lights.Length; k++) _lights[k].color = k < _shownLights ? Gold : LightOff;
            float a = Motion.Decorative && _shownLights > 0 ? Mathf.Max(0f, 1f - (now - _lastLightAt) / 0.6f) : 0f;
            _stripGlow.gameObject.SetActive(a > 0f);
            if (a > 0f)
            {
                _stripGlow.color = new Color(Gold.r, Gold.g, Gold.b, a);
                _stripGlow.rectTransform.anchoredPosition = _lights[Mathf.Clamp(_shownLights - 1, 0, 9)].rectTransform.anchoredPosition;
            }
        }

        public void RefreshStrip()
        {
            _rocketTitle.text = "Cohete n.º " + (_shownOrbit + 1);
            _orbitText.text = _shownOrbit > 0 ? $"{_shownOrbit} {(_shownOrbit == 1 ? "cohete" : "cohetes")} en órbita" : "";
        }

        /// <summary>Abajo: las llaves de la cuenta de cambios (se llenan de dorado al usarlas) y «Arrancar» (se hunde al tocarlo).</summary>
        private void AnimateBottom(float now)
        {
            if (_mach == null) return;
            bool can = _phase == Phase.Play;
            _counterGroup.alpha = can ? 1f : 0.5f;
            for (int k = 0; k < 2; k++)
            {
                bool shown = k < _mach.Stage.Keys;
                _wrenchBg[k].gameObject.SetActive(shown);
                _wrenches[k].gameObject.SetActive(shown);
                if (!shown) continue;
                bool used = k < _changes.Count;
                _wrenchBg[k].color = used ? new Color(Gold.r, Gold.g, Gold.b, 0.18f) : new Color(1f, 1f, 1f, 0.06f);
                _wrenches[k].color = used ? Gold : new Color(110f / 255f, 106f / 255f, 154f / 255f);
            }
            var b = _start;
            b.Group.alpha = can ? 1f : 0.45f;
            float pr = _startPressAt > 0f ? Mathf.Max(0f, 1f - (now - _startPressAt) / 0.2f) : 0f;
            b.Bg.color = Cyan;
            b.Rim.color = NeuroStyle.Ink;
            b.Label.color = NeuroStyle.Ink;
            b.Icon.color = NeuroStyle.Ink;
            var top = new Vector2(0f, -pr * 4f * _s);
            b.Bg.rectTransform.anchoredPosition = top;
            b.Rim.rectTransform.anchoredPosition = top;
            b.Icon.rectTransform.anchoredPosition = new Vector2((-b.Rect.width / 2f + 63f) * _s, 0f) + top;
            b.Label.rectTransform.anchoredPosition = new Vector2((-b.Rect.width / 2f + 88f + (b.Rect.width - 96f) / 2f) * _s, 0f) + top;
        }

        // ------------------------------------------------------------------ aviso, luz que vuela, chispas, tarjeta «NUEVO»

        /// <summary>Lo que dice el aviso de abajo: <paramref name="good"/> = verde, si no ámbar; <paramref name="sub"/> (la pista) va en dorado debajo.</summary>
        private void Tell(string title, string sub, bool good)
        {
            _sayTitleFull = title ?? "";
            _saySubFull = sub ?? "";
            _sayGood = good;
            _sayAt = GameClock.Time;
        }

        private void AnimateSay(float now)
        {
            float k = (now - _sayAt) / 3.2f;
            bool on = k >= 0f && k < 1f && _sayTitleFull.Length > 0;
            _sayRoot.gameObject.SetActive(on);
            if (!on) return;
            _sayGroup.alpha = Mathf.Clamp01(Mathf.Min(k * 8f, (1f - k) * 4f));
            _sayTitle.text = _sayTitleFull;
            _saySub.text = _saySubFull;
            _sayTitle.color = _sayGood ? Mint : WarnText;
            _sayRim.color = _sayGood ? new Color(159f / 255f, 245f / 255f, 214f / 255f, 0.5f) : new Color(1f, 214f / 255f, 160f / 255f, 0.5f);
            bool hasSub = _saySubFull.Length > 0;
            if (hasSub != _sayHasSub)
            {
                _sayHasSub = hasSub;
                LayoutSay();
            }
            if (!hasSub)
            {
                float h = _lay.SayH;
                SetChild(_sayTitle.rectTransform, 0f, 0f, EngranajesLayout.W - 40f, h * 0.8f);
            }
            _saySub.gameObject.SetActive(hasSub);
        }

        private void StartFlyer(Vector2 fromLogical)
        {
            _flyerOn = true;
            _flyerFrom = fromLogical;
            _flyerAt = GameClock.Time;
        }

        private void AnimateFlyer(float now)
        {
            if (!_flyerOn)
            {
                _flyerGlow.gameObject.SetActive(false);
                _flyerDot.gameObject.SetActive(false);
                return;
            }
            bool deco = Motion.Decorative;
            int idx = Mathf.Clamp(_shownLights, 0, EngranajesContract.RocketLights - 1);
            var to = new Vector2(150f + idx * 19f, _lay.StripTop + 14f);
            float k = (now - _flyerAt) / (deco ? 0.7f : 0.01f);
            if (k >= 1f)
            {
                _flyerOn = false;
                _shownLights = Mathf.Min(EngranajesContract.RocketLights, _shownLights + 1);
                _lastLightAt = now;
                Emit(to, 14, 90f, 0.6f, Gold, 20f);
                PlayClip(EngranajesSounds.Light(idx), 0.8f);
                return;
            }
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(k), 3f);
            var pos = Vector2.Lerp(_flyerFrom, to, e) + new Vector2(0f, -Mathf.Sin(Mathf.PI * e) * 50f);
            _flyerGlow.gameObject.SetActive(deco);
            _flyerDot.gameObject.SetActive(true);
            _flyerGlow.rectTransform.anchoredPosition = P(pos);
            _flyerGlow.rectTransform.sizeDelta = Vector2.one * (44f * _s);
            _flyerDot.rectTransform.anchoredPosition = P(pos);
            _flyerDot.rectTransform.sizeDelta = Vector2.one * (12f * _s);
        }

        private void Emit(Vector2 logicalAt, int n, float spread, float life, Color color, float up)
        {
            if (!Motion.Decorative) return;
            for (int i = 0; i < n; i++)
            {
                var p = _sparks.Find(x => !x.Alive);
                if (p == null) return;
                p.Alive = true;
                p.Pos = logicalAt;
                p.Vel = new Vector2(((float)_rng.NextDouble() - 0.5f) * spread, ((float)_rng.NextDouble() - 0.5f) * spread - up);
                p.Age = 0f;
                p.Life = life * (0.6f + (float)_rng.NextDouble() * 0.7f);
                p.Size = 0.8f + (float)_rng.NextDouble() * 1.8f;
                p.Color = color;
                p.Img.gameObject.SetActive(true);
            }
        }

        private void AnimateSparks(float dt)
        {
            foreach (var p in _sparks)
            {
                if (!p.Alive) continue;
                p.Age += dt;
                if (p.Age >= p.Life) { p.Alive = false; p.Img.gameObject.SetActive(false); continue; }
                p.Vel.y += 60f * dt;
                p.Pos += p.Vel * dt;
                float k = p.Age / p.Life;
                p.Img.rectTransform.anchoredPosition = P(p.Pos);
                p.Img.rectTransform.sizeDelta = Vector2.one * (p.Size * 2f * (1f - k * 0.5f) * _s);
                p.Img.color = NeuroStyle.WithAlpha(p.Color, 0.9f * (1f - k));
            }
        }

        private void AnimateIntro(float now)
        {
            if (!_introLayer.gameObject.activeSelf) return;
            bool deco = Motion.Decorative;
            float k = deco ? 1f - Mathf.Pow(1f - Mathf.Clamp01((now - _introAt) / 0.35f), 3f) : 1f;
            _dim.color = new Color(2f / 255f, 3f / 255f, 15f / 255f, 0.74f * k);
            _introGroup.alpha = k;
            float t = deco ? now * 1.667f : 0f;
            _introGearA.A = t;
            _introGearB.A = EngranajesContract.MeshPhase(_introGearA, _introGearB);
            ApplyAngle(_introA, _introGearA.A);
            ApplyAngle(_introB, _introGearB.A);
            _introTap.color = deco ? new Color(Cyan.r, Cyan.g, Cyan.b, 0.7f + 0.3f * Mathf.Sin(now * 4f)) : Cyan;
        }

        // ------------------------------------------------------------------ qué se tocó

        /// <summary>Qué interruptor cae bajo un toque en <paramref name="p"/> (coordenadas de la escena, del boceto): el motor (su engranaje, a 12 de la punta, o su rótulo) o una correa (a
        /// <see cref="EngranajesContract.BeltTouch"/> o menos de su línea). Con varios cerca, el más cercano. <see cref="int.MinValue"/> si no tocó ninguno.</summary>
        private int HitSwitch(Vector2 p)
        {
            var m = _mach;
            if (m == null) return int.MinValue;
            int best = int.MinValue;
            float bestRaw = float.MaxValue;
            void Consider(int id, float raw)
            {
                if (raw < bestRaw) { bestRaw = raw; best = id; }
            }
            var g0 = m.Gears[0];
            float dm = Vector2.Distance(p, new Vector2(g0.X, g0.Y));
            var chip = new Rect(g0.X - 31f, g0.Y + 16f - 12f, 62f, 24f);
            if (dm - g0.Tip - 12f <= 0f || chip.Contains(p)) Consider(EngranajesContract.MotorId, dm);
            for (int b = 0; b < _beltLinks.Count; b++)
            {
                var l = m.Links[_beltLinks[b]];
                float d = SegmentDistance(p, new Vector2(m.Gears[l.A].X, m.Gears[l.A].Y), new Vector2(m.Gears[l.B].X, m.Gears[l.B].Y));
                if (d <= EngranajesContract.BeltTouch) Consider(_beltLinks[b], d);
            }
            return best;
        }

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var v = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, v) / Mathf.Max(0.0001f, Vector2.Dot(v, v)));
            return Vector2.Distance(p, a + v * t);
        }

        // ------------------------------------------------------------------ verificación de que lo dibujado se ve

        /// <summary>Para las pruebas: arma una máquina de cada nivel y devuelve lo que NO se vería (pieza apagada, sin imagen o sin opacidad): la sala, el cohete, los engranajes con su cubo y su
        /// sombra, las poleas y las tiras de las correas, las barras, los carteles, el motor, la consigna, la cuenta de cambios y «Arrancar». Una pieza horneada sin sprite se dibuja como un cuadrado blanco
        /// (lo que pasó con las fichas de Punta el 3-oct).</summary>
        public List<string> AuditVisibility()
        {
            var problems = new List<string>();
            _s = 3f; _playW = 1080f; _playH = 1920f; _logicalH = 640f;
            _lay = EngranajesLayout.Compute(_logicalH);
            if (_rng == null) _rng = new System.Random(1);
            var bake = EngranajesSprites.Prewarm();
            while (bake.MoveNext()) { }
            AssignSprites();
            void Check(string what, Graphic g, bool needsSprite, bool needsAlpha = true)
            {
                if (g == null) { problems.Add(what + ": no existe"); return; }
                if (!g.gameObject.activeInHierarchy) problems.Add(what + ": apagada");
                else if (needsAlpha && g.color.a <= 0.01f) problems.Add(what + ": transparente");
                if (needsSprite && g is Image im && im.sprite == null) problems.Add(what + ": sin imagen");
            }
            Check("sala", _room, true);
            Check("cohete.casco", _hull, true);
            for (int k = 0; k < _windows.Length; k++) Check("cohete.ventanilla " + k, _windows[k], true, false);
            Check("antena.radar", _dish, true);
            Check("turbina.aspas", _fan, true);
            Check("compuerta.portón", _door, true);
            Check("carga.marco", _cargoFrame, true);
            Check("carga.plataforma", _cargoPlate, true);
            Check("carga.caja", _cargoBox, true);
            int shown = 0;
            for (int level = 1; level <= EngranajesContract.MaxLevel; level++)
                for (int n = 0; n < 12; n++)
                {
                    var m = EngranajesContract.Generate(level, _rng);
                    if (m.Gears.Count > GearPool) problems.Add("nivel " + level + ": la máquina tiene " + m.Gears.Count + " engranajes y solo hay " + GearPool);
                    ShowMachine(m);
                    if (n != 0 || (level != 3 && level != 10)) continue;
                    shown++;
                    string tag = "nivel " + level + ".";
                    for (int i = 0; i < m.Gears.Count; i++)
                    {
                        var v = _gears[i];
                        Check(tag + "engranaje " + i + ".cuerpo", v.Body, true);
                        Check(tag + "engranaje " + i + ".sombra", v.Shadow, true);
                        Check(tag + "engranaje " + i + ".cubo", v.Hub, true);
                        Check(tag + "engranaje " + i + ".soporte", v.Support, true);
                    }
                    Check(tag + "flecha del motor", _motorArrow, false);
                    Check(tag + "MOTOR", _motorPill.Bg, true);
                    Check(tag + "consigna", _questionBg, true);
                    Check(tag + "consigna.línea 1", _consigna1, false);
                    Check(tag + "consigna.línea 2", _consigna2, false);
                    Check(tag + "cuenta de cambios", _counterBg, true);
                    Check(tag + "cuenta de cambios.texto", _counterText, false);
                    Check(tag + "Arrancar.fondo", _start.Bg, true);
                    Check(tag + "Arrancar.borde", _start.Rim, true);
                    Check(tag + "Arrancar.ícono", _start.Icon, true);
                    Check(tag + "Arrancar.texto", _start.Label, false);
                    for (int b = 0; b < _beltLinks.Count; b++)
                    {
                        var bv = _belts[b];
                        foreach (var pu in bv.Pulleys) Check(tag + "correa " + b + ".polea", pu, true);
                        for (int i = 2; i < 6; i++) { Check(tag + "correa " + b + ".tira " + i, bv.Lines[i].Img, true, false); Check(tag + "correa " + b + ".media vuelta " + i, bv.Caps[i], true, false); }
                    }
                    foreach (var kv in m.Stations)
                    {
                        var cv = _cartels[(int)kv.Key];
                        Check(tag + "cartel " + kv.Key, cv.Bg, true);
                        Check(tag + "cartel " + kv.Key + ".borde", cv.Rim, true);
                        Check(tag + "cartel " + kv.Key + ".nombre", cv.Name, false);
                        Check(tag + "cartel " + kv.Key + ".palabra", cv.Word, false);
                        Check(tag + "cartel " + kv.Key + ".ícono", cv.Icon, true);
                        if (kv.Key == Station.Compuerta || kv.Key == Station.Carga) Check(tag + "barra de " + kv.Key, _racks[kv.Key == Station.Compuerta ? 0 : 1], true);
                    }
                }
            if (shown < 2) problems.Add("no se revisaron las dos máquinas de muestra");
            foreach (var sp in _sparks) if (sp.Img == null) problems.Add("chispas: no existe");
            ClearMachine();
            return problems;   // nada se destruye aquí (en modo edición no se puede): quien llama destruye el objeto entero
        }
    }
}
