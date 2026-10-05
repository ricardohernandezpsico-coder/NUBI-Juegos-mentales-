using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;
using Motion = NeuroVida.Games.Shared.Motion;

namespace NeuroVida.Games.Engranajes
{
    /// <summary>La máquina en pantalla y su animación por cuadro: armar y limpiar, el giro (todo junto: los dientes encajan siempre), el pulso de luz, las piezas del cohete, las flechas de
    /// sentido y del truco de Nubi, las luces de la cabecera, el despegue y las chispas.</summary>
    public sealed partial class EngranajesGameController
    {
        private readonly GearDef _introGearA = new GearDef { N = EngranajesContract.BigTeeth, Pitch = EngranajesContract.BigPitch, Tip = EngranajesContract.BigTip };
        private readonly GearDef _introGearB = new GearDef { X = 50f, N = EngranajesContract.SmallTeeth, Pitch = EngranajesContract.SmallPitch, Tip = EngranajesContract.SmallTip };
        private Station _labelTarget = Station.Antena;
        private bool _sayHasSub = true;

        // posición de cada pieza y de su rótulo, en coordenadas del boceto
        private static Vector2 PartPosition(Station s)
        {
            switch (s)
            {
                case Station.Antena: return new Vector2(306f, 98f);
                case Station.Compuerta: return new Vector2(312f, 250f);
                case Station.Carga: return new Vector2(312f, 316f);
                default: return new Vector2(306f, 442f);
            }
        }

        private static Vector2 LabelPosition(Station s)
        {
            switch (s)
            {
                case Station.Antena: return new Vector2(306f, 126f);
                case Station.Compuerta: return new Vector2(304f, 281f);
                case Station.Carga: return new Vector2(304f, 360f);
                default: return new Vector2(306f, 404f);
            }
        }

        // ------------------------------------------------------------------ armar y limpiar

        /// <summary>Pone una máquina en pantalla (sin contar nada): los engranajes, las correas, los ejes a las piezas, los rótulos, la pregunta y los botones.</summary>
        private void ShowMachine(Machine m)
        {
            ClearMachine();
            _mach = m;
            _placed = Piece.None;
            _sol = null;
            _runAt = float.MaxValue;
            _trickAt = -10f;
            _answered = Answer.None;
            _okAnswer = false;
            _jamRun = false;
            _pressIndex = -1;
            if (m.Q == Question.Build) EngranajesContract.Phase(m, Piece.Gear, _rng);   // el hueco, aunque no se ve, ya engrana con sus dos vecinos: así los de después también encajan
            _machineLayer.gameObject.SetActive(true);
            _lineCount = 0;

            // ejes de cada engranaje de pieza al cohete (primero, para que queden debajo)
            foreach (var kv in m.Stations)
            {
                var g = m.Gears[kv.Value];
                var p = PartPosition(kv.Key);
                var from = new Vector2(g.X, g.Y);
                if (kv.Key == Station.Antena || kv.Key == Station.Turbina)
                {
                    var bend = new Vector2(p.x, g.Y);
                    AddShaftSegment(from, bend, ShaftDark, 6f);
                    AddShaftSegment(bend, p, ShaftDark, 6f);
                    AddShaftSegment(from, bend, ShaftLight, 2.5f);
                    AddShaftSegment(bend, p, ShaftLight, 2.5f);
                }
                else
                {
                    var end = new Vector2(p.x - 14f, g.Y);
                    AddShaftSegment(from, end, ShaftDark, 6f);
                    AddShaftSegment(from, end, ShaftLight, 2.5f);
                }
            }
            // correas
            int pill = 0;
            foreach (var l in m.Links)
            {
                if (l.Type != LinkType.Straight && l.Type != LinkType.Crossed) continue;
                var a = m.Gears[l.A];
                var b = m.Gears[l.B];
                AddBelt(a, b, l.Type == LinkType.Crossed, null);
                if (pill < _beltPills.Length) PlaceBeltPill(_beltPills[pill++], a, b, l.Type == LinkType.Crossed);
            }
            if (m.Slot != null)
            {
                // la correa cruzada del hueco (se ve cuando se elige) y el hueco punteado con su «?»
                AddBelt(m.Gears[m.Slot.From], m.Gears[m.Slot.To], true, _slotBelt);
                foreach (var lv in _slotBelt) lv.Img.gameObject.SetActive(false);
                float tip = m.Slot.Gear.Tip;
                _slotRing.rectTransform.anchoredPosition = B(m.Slot.Gear.X, m.Slot.Gear.Y);
                _slotRing.rectTransform.sizeDelta = Vector2.one * (tip * 2.1f * Su);
                _slotRing.gameObject.SetActive(true);
                _slotMark.rectTransform.anchoredPosition = B(m.Slot.Gear.X, m.Slot.Gear.Y + 1f);
                _slotMark.gameObject.SetActive(true);
                PlaceGear(_slotGear, m.Slot.Gear, EngranajesSprites.Palette.Main);
                _slotGear.Root.gameObject.SetActive(false);
            }
            // engranajes
            for (int i = 0; i < m.Gears.Count && i < GearPool; i++)
            {
                var g = m.Gears[i];
                if (g.Removed) continue;
                PlaceGear(_gears[i], g, EngranajesSprites.PaletteOf(g, i));
            }
            // el motor y su flecha
            var g0 = m.Gears[0];
            float r = g0.Tip + 10f;
            _motorArrow.sprite = EngranajesSprites.Arrow(r, 4f, m.MotorDir > 0);
            _motorArrowRect.anchoredPosition = B(g0.X, g0.Y);
            _motorArrowRect.sizeDelta = Vector2.one * (2f * (r + 14f) * Su);
            _motorArrowRect.localEulerAngles = Vector3.zero;
            _motorArrow.gameObject.SetActive(true);
            _motorPill.Root.anchoredPosition = B(g0.X, g0.Y + g0.Tip * 0.55f);
            _motorPill.Root.gameObject.SetActive(true);
            _motorPill.Root.sizeDelta = new Vector2(62f * Su, 22f * Su);
            SetRadius(_motorPill.Bg, 11f * Su);
            // rótulos y piezas
            _labelTarget = m.TargetStation;
            for (int s = 0; s < 4; s++)
            {
                var st = (Station)s;
                bool active = m.Stations.ContainsKey(st), target = st == m.TargetStation;
                var pv = _stationPills[s];
                pv.Label.text = EngranajesContract.StationLabel(st);
                float dp = target ? 16f : 14f;
                float w = pv.Label.text.Length * 0.6f * dp + 16f;
                pv.Root.sizeDelta = new Vector2(w * Su, 22f * Su);
                SetRadius(pv.Bg, 11f * Su);
                pv.Bg.color = target ? Gold : PillIdle;
                pv.Label.color = target || active ? NeuroStyle.Ink : NeuroStyle.WithAlpha(NeuroStyle.Ink, 0.5f);
                pv.Root.anchoredPosition = B(LabelPosition(st).x, LabelPosition(st).y);
                pv.Root.gameObject.SetActive(true);
                _partGroups[s].alpha = active ? 1f : 0.45f;
            }
            ApplyLabelFonts();
            ResetParts();
            // pregunta y botones
            string q = EngranajesLayout.BreakQuestion(EngranajesContract.QuestionText(m), out bool two);
            _questionText.text = q;
            _questionDp = two ? 16f : 18f;
            ApplyQuestionFont();
            _questionBar.gameObject.SetActive(true);
            var defs = EngranajesContract.Buttons(m);
            _buttonCount = defs.Length;
            for (int i = 0; i < defs.Length; i++)
            {
                _buttons[i].Def = defs[i];
                _buttons[i].Label.text = defs[i].Label;
                _buttons[i].Icon.sprite = EngranajesSprites.Icon(defs[i].Icon);
            }
            LayoutButtons();
            _appearAt = GameClock.Time;
        }

        private void ClearMachine()
        {
            _mach = null;
            _sol = null;
            _placed = Piece.None;
            _runAt = float.MaxValue;
            _trickAt = -10f;
            _answered = Answer.None;
            _jamRun = false;
            _buttonCount = 0;
            _pressIndex = -1;
            if (_machineLayer == null) return;
            foreach (var g in _gears) g.Root.gameObject.SetActive(false);
            _slotGear.Root.gameObject.SetActive(false);
            foreach (var l in _lines) l.Img.gameObject.SetActive(false);
            foreach (var l in _slotBelt) l.Img.gameObject.SetActive(false);
            foreach (var p in _beltPills) p.Root.gameObject.SetActive(false);
            foreach (var p in _stationPills) p.Root.gameObject.SetActive(false);
            _motorPill.Root.gameObject.SetActive(false);
            foreach (var p in _pulses) p.gameObject.SetActive(false);
            foreach (var d in _dashes) d.gameObject.SetActive(false);
            foreach (var a in _arrows) a.gameObject.SetActive(false);
            _motorArrow.gameObject.SetActive(false);
            _slotRing.gameObject.SetActive(false);
            _slotMark.gameObject.SetActive(false);
            _targetRing.gameObject.SetActive(false);
            _questionBar.gameObject.SetActive(false);
            for (int s = 0; s < 4; s++) if (_partGroups[s] != null) _partGroups[s].alpha = 0.45f;
            ResetParts();
            foreach (var b in _buttons) b.Root.gameObject.SetActive(false);
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

        /// <summary>Una correa entre dos engranajes: dos tiras (arriba y abajo) en oscuro y en claro; cruzada = las tiras se cruzan. Con <paramref name="slotLines"/> usa esas líneas (la del hueco).</summary>
        private void AddBelt(GearDef a, GearDef b, bool crossed, LineView[] slotLines)
        {
            float ang = Mathf.Atan2(b.Y - a.Y, b.X - a.X), n = ang + Mathf.PI / 2f;
            float ra = a.Pitch - 1f, rb = b.Pitch - 1f;
            var nv = new Vector2(Mathf.Cos(n), Mathf.Sin(n));
            for (int layer = 0; layer < 2; layer++)
                for (int k = 0; k < 2; k++)
                {
                    float sgn = k == 0 ? 1f : -1f;
                    var p0 = new Vector2(a.X, a.Y) + nv * ra * sgn;
                    var p1 = new Vector2(b.X, b.Y) + nv * rb * (crossed ? -sgn : sgn);
                    var lv = slotLines != null ? slotLines[layer * 2 + k] : NextLine();
                    SetLine(lv, p0, p1, layer == 0 ? 8f : 4f, layer == 0 ? BeltDark : BeltLight);
                }
        }

        private void PlaceBeltPill(PillView pv, GearDef a, GearDef b, bool crossed)
        {
            float mx = (a.X + b.X) / 2f, my = (a.Y + b.Y) / 2f;
            bool vertical = Mathf.Abs(a.X - b.X) < 2f;
            float lx = vertical ? (mx + 50f < 240f ? mx + 50f : mx - 50f) : mx;
            float ly = vertical ? my : my - 30f;
            pv.Label.text = crossed ? "cruzada" : "recta";
            pv.Root.anchoredPosition = B(lx, ly);
            pv.Root.gameObject.SetActive(true);
        }

        private void ApplyLabelFonts()
        {
            float k = _lay.SceneScale <= 0f ? 1f : _lay.SceneScale;
            for (int s = 0; s < 4; s++) _stationPills[s].Label.fontSize = Mathf.RoundToInt((s == (int)_labelTarget ? 16f : 14f) * Su / k);
        }

        private void CaptureAngles()
        {
            for (int i = 0; i < _mach.Gears.Count && i < _baseA.Length - 2; i++) _baseA[i] = _mach.Gears[i].A;
            _slotBaseA = _mach.Slot != null ? _mach.Slot.Gear.A : 0f;
        }

        // ------------------------------------------------------------------ animación por cuadro

        /// <summary>∫ de la rampa de aceleración: el giro arranca suave y llega a su velocidad en <see cref="Accel"/> s.</summary>
        private static float Ramp(float t)
        {
            if (t <= 0f) return 0f;
            return t < Accel ? t * t / (2f * Accel) : Accel / 2f + (t - Accel);
        }

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

            // giro: todo junto, cada engranaje a su velocidad (los dientes encajan siempre); el motor de la trampa solo forcejea
            if (running && deco)
            {
                if (_jamRun)
                {
                    m.Gears[0].A = _baseA[0] + 0.096f * Mathf.Sin(25f * run);
                }
                else
                {
                    float R = Ramp(run);
                    for (int i = 0; i < m.Gears.Count && i < GearPool; i++)
                        if (!m.Gears[i].Removed && _sol.Depth[i] >= 0) m.Gears[i].A = _baseA[i] + Spin * _sol.Speed[i] * R;
                    if (_placed == Piece.Gear && m.Slot != null)
                    {
                        var from = m.Gears[m.Slot.From];
                        m.Slot.Gear.A = _slotBaseA + Spin * (-_sol.Speed[m.Slot.From] * from.Pitch / m.Slot.Gear.Pitch) * R;
                    }
                }
            }
            for (int i = 0; i < m.Gears.Count && i < GearPool; i++)
            {
                var g = m.Gears[i];
                if (g.Removed) continue;
                var v = _gears[i];
                ApplyAngle(v, g.A);
                // el destello cuando la fuerza llega a ese engranaje (la cascada: un paso cada 0,15 s)
                float glow = 0f;
                if (running && deco && !_jamRun && _sol.Depth[i] >= 0)
                {
                    float k = (run - _sol.Depth[i] * step) / 0.5f;
                    if (k > 0f && k < 1f) glow = Mathf.Sin(k * Mathf.PI) * 0.55f;
                }
                v.Glow.color = new Color(1f, 214f / 255f, 120f / 255f, glow);
            }
            // el hueco de «Arma tú»: el aro punteado que late con su «?», o el engranaje / la correa elegidos
            bool placedShown = _placed != Piece.None && (!deco || now >= _runAt - 0.15f);
            if (m.Slot != null)
            {
                bool open = _placed == Piece.None;
                _slotRing.gameObject.SetActive(open);
                _slotMark.gameObject.SetActive(open);
                if (open) _slotRing.color = new Color(Gold.r, Gold.g, Gold.b, 0.6f + (deco ? 0.3f * Mathf.Sin(now * 3.85f) : 0.3f));
                _slotGear.Root.gameObject.SetActive(_placed == Piece.Gear && placedShown);
                if (_slotGear.Root.gameObject.activeSelf) ApplyAngle(_slotGear, m.Slot.Gear.A);
                foreach (var lv in _slotBelt) lv.Img.gameObject.SetActive(_placed == Piece.Crossed && placedShown);
            }
            // flecha del motor: sigue su sentido (con «quitar animaciones» queda quieta)
            _motorArrowRect.localEulerAngles = new Vector3(0f, 0f, deco ? -now * 0.9f * m.MotorDir * Mathf.Rad2Deg : 0f);
            _targetRing.gameObject.SetActive(_phase == Phase.Play);
            if (_phase == Phase.Play)
            {
                var tp = PartPosition(m.TargetStation);
                _targetRing.rectTransform.anchoredPosition = B(tp.x, tp.y);
                _targetRing.color = new Color(Gold.r, Gold.g, Gold.b, deco ? 0.5f + 0.4f * Mathf.Sin(now * 3.57f) : 0.9f);
            }
            AnimateParts(m, running, run, step, deco);
            AnimatePulses(m, running, run, step, deco);
            AnimateTrap(m, running);
            AnimateArrows(m, running, now, deco);
        }

        private void AnimateParts(Machine m, bool running, float run, float step, bool deco)
        {
            // radar y turbina giran con el engranaje de su pieza
            if (m.Stations.TryGetValue(Station.Antena, out int ia)) _dish.rectTransform.localEulerAngles = new Vector3(0f, 0f, -m.Gears[ia].A * Mathf.Rad2Deg);
            float flame = 0f;
            if (m.Stations.TryGetValue(Station.Turbina, out int it))
            {
                _fan.rectTransform.localEulerAngles = new Vector3(0f, 0f, -m.Gears[it].A * Mathf.Rad2Deg);
                if (running && deco && !_jamRun && _sol.Depth[it] >= 0)
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
                if (running && !_jamRun && _sol.Depth[gi] >= 0)
                {
                    var g = m.Gears[gi];
                    float t = Mathf.Max(0f, run - _sol.Depth[gi] * step);
                    off = deco ? Spin * _sol.Speed[gi] * g.Pitch * Mathf.Max(0f, t - 0.15f) * 0.9f : (_sol.Dir[gi] > 0 ? 16f : -40f);
                }
                _rackOff[st] = off;
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
            if (running && deco && !_jamRun)
            {
                foreach (var l in m.Links)
                {
                    if (l.Type == LinkType.Gap && _placed == Piece.None) continue;
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

        /// <summary>La trampa: un triángulo punteado coral entre los tres engranajes que se tocan.</summary>
        private void AnimateTrap(Machine m, bool running)
        {
            int used = 0;
            if (running && _jamRun && m.JamTri != null)
            {
                var pts = new[] { m.Gears[m.JamTri[0]], m.Gears[m.JamTri[1]], m.Gears[m.JamTri[2]] };
                for (int e = 0; e < 3; e++)
                {
                    var a = new Vector2(pts[e].X, pts[e].Y);
                    var b = new Vector2(pts[(e + 1) % 3].X, pts[(e + 1) % 3].Y);
                    float len = Vector2.Distance(a, b);
                    var dir = (b - a) / len;
                    for (float d = 0f; d < len - 2f && used < _dashes.Length; d += 11f)
                    {
                        var p0 = a + dir * d;
                        var p1 = a + dir * Mathf.Min(len, d + 6f);
                        var img = _dashes[used++];
                        var mid = (p0 + p1) * 0.5f;
                        img.rectTransform.anchoredPosition = B(mid.x, mid.y);
                        img.rectTransform.sizeDelta = new Vector2((Vector2.Distance(p0, p1) + 3f) * Su, 3f * Su);
                        img.rectTransform.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                        img.gameObject.SetActive(true);
                    }
                }
            }
            for (int i = used; i < _dashes.Length; i++) _dashes[i].gameObject.SetActive(false);
        }

        /// <summary>Flechas de sentido sobre los engranajes: las del truco de Nubi al fallar (por el camino del motor a la pieza, una tras otra) y, con «quitar animaciones», las de todos los que giran.</summary>
        private void AnimateArrows(Machine m, bool running, float now, bool deco)
        {
            bool trick = _trickAt >= 0f && !_okAnswer && !_jamRun && _sol != null;
            for (int i = 0; i < _arrows.Length; i++)
            {
                var img = _arrows[i];
                bool show = false;
                float alpha = 1f;
                Color color = Mint;
                int pathIndex = trick ? _trickPath.IndexOf(i) : -1;
                if (trick && pathIndex >= 0)
                {
                    float k = deco ? Mathf.Clamp01((now - _trickAt - pathIndex * 0.17f) / 0.25f) : 1f;
                    show = k > 0f;
                    alpha = k;
                    color = pathIndex % 2 == 1 ? Gold : Mint;
                }
                else if (!deco && running && !_jamRun && i < m.Gears.Count && !m.Gears[i].Removed && _sol.Depth[i] >= 0 && !trick)
                {
                    show = true;
                    color = Cyan;
                }
                if (!show || i >= m.Gears.Count) { img.gameObject.SetActive(false); continue; }
                var g = m.Gears[i];
                float r = g.Tip + 7f;
                img.sprite = EngranajesSprites.Arrow(r, 3.5f, _sol.Speed[i] >= 0f);
                img.rectTransform.anchoredPosition = B(g.X, g.Y);
                img.rectTransform.sizeDelta = Vector2.one * (2f * (r + 14f) * Su);
                img.color = new Color(color.r, color.g, color.b, alpha);
                img.gameObject.SetActive(true);
            }
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

        private void AnimateButtons(float now)
        {
            for (int i = 0; i < _buttonCount; i++)
            {
                var b = _buttons[i];
                bool chosen = _answered != Answer.None && b.Def.Value == _answered;
                bool can = _phase == Phase.Play;
                b.Group.alpha = can || chosen ? 1f : 0.45f;
                float pr = _pressIndex == i ? Mathf.Max(0f, 1f - (now - _pressAt) / 0.2f) : 0f;
                var text = chosen ? NeuroStyle.Ink : ButtonText;
                b.Bg.color = chosen ? (_okAnswer ? Gold : WrongFill) : ButtonFill;
                b.Rim.color = chosen ? NeuroStyle.Ink : new Color(1f, 1f, 1f, 0.16f);
                b.Label.color = text;
                b.Icon.color = text;
                var top = new Vector2(0f, -pr * 4f * _s);
                b.Bg.rectTransform.anchoredPosition = top;
                b.Rim.rectTransform.anchoredPosition = top;
                b.Icon.rectTransform.anchoredPosition = new Vector2(0f, (b.Rect.height * 0.5f - b.Rect.height * 0.36f) * _s) + top;
                b.Label.rectTransform.anchoredPosition = new Vector2(0f, -(b.Rect.height * 0.5f - 24f) * _s) + top;
            }
        }

        // ------------------------------------------------------------------ aviso, luz que vuela, chispas, tarjeta «NUEVO»

        /// <summary>Lo que dice el aviso de abajo: <paramref name="good"/> = verde, si no ámbar; <paramref name="sub"/> (el truco de Nubi) va en dorado debajo.</summary>
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
            _introA.Root.gameObject.SetActive(true);
            _introB.Root.gameObject.SetActive(true);
            ApplyAngle(_introA, _introGearA.A);
            ApplyAngle(_introB, _introGearB.A);
            _introTap.color = deco ? new Color(Cyan.r, Cyan.g, Cyan.b, 0.7f + 0.3f * Mathf.Sin(now * 4f)) : Cyan;
        }

        private void AnimateLaunch(float now)
        {
            // el texto del despegue lo muestra su capa; nada más que animar acá (el cohete sube en AnimateRocket)
        }

        // ------------------------------------------------------------------ verificación de que lo dibujado se ve

        /// <summary>Para las pruebas: arma una máquina de cada nivel y devuelve lo que NO se vería (pieza apagada, sin imagen o sin opacidad): la sala, el cohete, los engranajes con su cubo y su
        /// sombra, las piezas del cohete, los rótulos y los botones con su ícono. Una pieza horneada sin sprite se dibuja como un cuadrado blanco (lo que pasó con las fichas de Punta el 3-oct).</summary>
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
                        if (m.Gears[i].Removed) continue;
                        var v = _gears[i];
                        Check(tag + "engranaje " + i + ".cuerpo", v.Body, true);
                        Check(tag + "engranaje " + i + ".sombra", v.Shadow, true);
                        Check(tag + "engranaje " + i + ".cubo", v.Hub, true);
                        Check(tag + "engranaje " + i + ".soporte", v.Support, true);
                    }
                    Check(tag + "flecha del motor", _motorArrow, true);
                    foreach (var kv in m.Stations) Check(tag + "rótulo " + kv.Key, _stationPills[(int)kv.Key].Bg, true);
                    Check(tag + "MOTOR", _motorPill.Bg, true);
                    Check(tag + "pregunta", _questionBg, true);
                    Check(tag + "pregunta.texto", _questionText, false);
                    for (int i = 0; i < _buttonCount; i++)
                    {
                        Check(tag + "botón " + i + ".fondo", _buttons[i].Bg, true);
                        Check(tag + "botón " + i + ".borde", _buttons[i].Rim, true);
                        Check(tag + "botón " + i + ".ícono", _buttons[i].Icon, true);
                        Check(tag + "botón " + i + ".texto", _buttons[i].Label, false);
                    }
                    if (m.Slot != null) { Check(tag + "hueco.aro", _slotRing, true); Check(tag + "hueco.signo", _slotMark, false); }
                }
            if (shown < 2) problems.Add("no se revisaron las dos máquinas de muestra");
            foreach (var sp in _sparks) if (sp.Img == null) problems.Add("chispas: no existe");
            ClearMachine();
            return problems;   // nada se destruye aquí (en modo edición no se puede): quien llama destruye el objeto entero
        }
    }
}
