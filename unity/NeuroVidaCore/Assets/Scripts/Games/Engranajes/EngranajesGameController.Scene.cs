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

        /// <summary>El centro de un cartel de ancho <paramref name="w"/>: pegado a los engranajes y sin pasarse del borde derecho de la pantalla (16 dp de margen: <see cref="EngranajesLayout.CartelMargin"/>) aunque la escena esté achicada.</summary>
        private float CartelX(float w)
        {
            float k = _lay.SceneScale <= 0f ? 1f : _lay.SceneScale;
            float maxEdge = 180f + (EngranajesLayout.W - EngranajesLayout.CartelMargin - (EngranajesLayout.W / 2f + EngranajesLayout.SceneShiftX)) / k;
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
            var pill = EngranajesLayout.MotorPillCenter(m, out _);                // Tarea 166: el rótulo «Motor» ya no tapa media rueda: va fuera del círculo del motor (debajo o al costado)
            _motorPill.Root.anchoredPosition = B(pill.x, pill.y);
            _motorPill.Root.gameObject.SetActive(true);
            _motorPill.Root.sizeDelta = new Vector2(MotorW * Su, MotorH * Su);
            SetRadius(_motorPill.Bg, MotorH * Su / 2f);
            _motorPulse.rectTransform.anchoredPosition = B(pill.x, pill.y);
            _motorPulse.rectTransform.sizeDelta = new Vector2((MotorW + 10f) * Su, (MotorH + 10f) * Su);
            SetRadius(_motorPulse, (MotorH + 10f) * Su / 2f);
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
            _hud.SetPhase(EngranajesContract.HintShort(m.Stage));                                  // la consigna vive en la fila del marcador (Tarea 127); la barra de antes ya no se dibuja
            _hud.SetPromptFit(null, consigna[0], EngranajesContract.RuleShort);
            _questionBar.gameObject.SetActive(false);
            if (_counterSlab != null) { _counterNum.text = m.Stage.Keys.ToString(); _counterText.text = m.Stage.Keys == 1 ? "cambio" : "cambios"; }
            else _counterText.text = m.Stage.Keys == 1 ? "1 cambio" : "2 cambios";
            _counterRoot.gameObject.SetActive(true);
            _start.Root.gameObject.SetActive(true);
            LayoutBottom();
            _appearAt = GameClock.Time;
        }

        private void ClearMachine()
        {
            _mach = null;
            _blockAt = -10f;                                   // la moneda de un toque bloqueado no pasa a la máquina siguiente
            _blockId = int.MinValue;
            _counterPulseAt = -10f;
            _sol = null;
            _results = null;
            _changes.Clear();
            _runAt = float.MaxValue;
            _verdictAt = float.MaxValue;
            _showFixAt = -10f;
            _okAnswer = false;
            _startRequested = false;
            if (_machineLayer == null) return;
            foreach (var g in _gears)
            {
                g.Root.gameObject.SetActive(false);
                if (g.Soft != null) { g.Soft.gameObject.SetActive(false); g.Shadow.gameObject.SetActive(false); }                    // el canto y la sombra blanda viven en sus capas, no dentro del engranaje
                g.LastAngle = float.NaN;
            }
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
            _hud.SetPrompt("");
            _hud.SetPhase("");
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
            if (v.Soft != null) PlaceGearVolume(v, size, palette);
            ApplyAngle(v, g.A);
            v.Root.gameObject.SetActive(true);
        }

        // el canto de cada engranaje: el color profundo (arriba, junto a la cara, el claro) de su rol; paleta «Madera clara» (Tarea 164): motor celeste, camino de madera y pieza en uva (el morado de Razonamiento)
        private static readonly Color[] GearCantoLight = { new Color(0.06f, 0.37f, 0.50f), new Color(0.52f, 0.32f, 0.16f), new Color(0.29f, 0.20f, 0.54f) };
        private static readonly Color[] GearCantoDeep = { new Color(0.04f, 0.23f, 0.32f), new Color(0.33f, 0.20f, 0.10f), new Color(0.19f, 0.13f, 0.34f) };

        /// <summary>El VOLUMEN de un engranaje de la máquina: su canto (la silueta corrida 4,5 dp hacia ABAJO en la pantalla, que no gira) y la sombra de contacto blanda (un poco más abajo), ambos en las capas de debajo de todos los engranajes.</summary>
        private void PlaceGearVolume(GearView v, EngranajesSprites.GearSize size, EngranajesSprites.Palette palette)
        {
            var pos = v.Root.anchoredPosition;
            int pal = (int)palette;
            v.Shadow.rectTransform.anchoredPosition = pos;
            v.Shadow.color = GearCantoLight[pal];
            v.Canto.Apply(4.5f * Su, CantoEffect.DefaultSteps, GearCantoDeep[pal], GearCantoLight[pal]);
            v.Soft.sprite = EngranajesSprites.SilhouetteSoft(size);
            v.Soft.rectTransform.sizeDelta = Vector2.one * (EngranajesSprites.SoftBox(size) * Su);
            v.Soft.rectTransform.anchoredPosition = pos + new Vector2(0f, -7f * Su);
            v.Shadow.gameObject.SetActive(true);
            v.Soft.gameObject.SetActive(true);
            v.LastAngle = float.NaN;
        }

        private static void ApplyAngle(GearView v, float radians)
        {
            if (!float.IsNaN(v.LastAngle) && Mathf.Approximately(v.LastAngle, radians)) return;      // quieto: no hay nada que girar (ni malla de canto que rehacer)
            v.LastAngle = radians;
            var e = new Vector3(0f, 0f, -radians * Mathf.Rad2Deg);       // el ángulo del boceto es en el sentido del reloj: en la interfaz el giro positivo es al revés
            v.Body.rectTransform.localEulerAngles = e;
            v.Shadow.rectTransform.localEulerAngles = e;
            if (v.Soft != null)
            {
                v.Soft.rectTransform.localEulerAngles = e;
                if (v.Canto != null) v.Canto.Refresh();                                              // el canto NO gira: su corrimiento se gira al revés que el engranaje
            }
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
            if (lv.Canto != null) lv.Canto.Refresh();
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
                    var capCanto = bv.CapCanto[layer * 2 + s];
                    if (capCanto != null) capCanto.Refresh();
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
            float iconW = 24f;                                                              // el ícono de la acción (22 dp) y un respiro antes de la palabra de 16 dp (Tarea 129)
            float need = Mathf.Max(nameW + 24f, wordW + iconW) + 16f;       // el nombre deja lugar al ✓ de la derecha
            float w = Mathf.Max(CartelW, need);
            c.Root.anchoredPosition = B(CartelX(w), CartelY(st));
            c.Root.sizeDelta = new Vector2(w * Su, CartelH * Su);
            SetChildScene(c.Rim.rectTransform, 0f, 0f, w + 3f, CartelH + 3f);
            SetChildScene(c.Bg.rectTransform, 0f, 0f, w, CartelH);
            SetRadius(c.Rim, 13f * Su);
            SetRadius(c.Bg, 12f * Su);
            SetChildScene(c.Name.rectTransform, 0f, -11f, w, 20f / k);
            float group = wordW + iconW;
            float ix = -group / 2f + 9f;                                                  // la flechita o el triángulo, a la izquierda del grupo
            c.Word.rectTransform.sizeDelta = new Vector2((wordW + 4f) * Su, 20f / k * Su);
            c.Word.rectTransform.anchoredPosition = new Vector2((ix + 15f + (wordW + 4f) / 2f) * Su, -11f * Su);
            c.Icon.rectTransform.anchoredPosition = new Vector2(ix * Su, -11f * Su);
            c.Check.rectTransform.anchoredPosition = new Vector2((w / 2f - 11f) * Su, 12f * Su);
            c.Cross.rectTransform.anchoredPosition = c.Check.rectTransform.anchoredPosition;
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
                bool neutralCream = c.Slab != null && !ok && !bad && EngranajesSprites.HasCreamChip;                  // el cartel neutro es crema (Tarea 129): nombre #514B78 y palabra en tinta
                if (c.Slab != null)
                {
                    if (ok || bad) PlasticSlab.SetCustom(c.Slab, EngranajesSprites.PlasticGroup, EngranajesSprites.PlateName(ok ? EngranajesSprites.PlateState.Ok : EngranajesSprites.PlateState.Bad));
                    else PlasticSlab.SetCustom(c.Slab, EngranajesSprites.NeutralGroup, EngranajesSprites.NeutralPlate);
                }
                c.Name.color = neutralCream ? NeuroStyle.InkMuted : ButtonText;
                c.Bg.color = ok ? CartelOkFill : bad ? CartelBadFill : CartelFill;
                c.Rim.color = ok ? Mint : bad ? Coral : Gold;
                var col = ok ? Mint : bad ? CartelBadText : neutralCream ? NeuroStyle.Ink : Gold;
                c.Word.color = col;
                c.Icon.color = col;
                c.Check.gameObject.SetActive(ok);
                c.Cross.gameObject.SetActive(bad);                                          // el cartel que falla lleva su aspa (diagonal), no solo el coral
            }
        }

        /// <summary>Lo que lleva cada cartel según el veredicto (para las pruebas): { ✓ del que cumplió, aspa del que cumplió, ✓ del que falló, aspa del que falló }. Aplica el mismo código de <see cref="AnimateCartels"/> a un cartel de muestra.</summary>
        public bool[] VerdictMarks()
        {
            var c = _cartels[0];
            var result = new bool[4];
            c.Root.gameObject.SetActive(true);
            foreach (bool ok in new[] { true, false })
            {
                bool bad = !ok;
                c.Check.gameObject.SetActive(ok);
                c.Cross.gameObject.SetActive(bad);
                result[ok ? 0 : 2] = c.Check.gameObject.activeSelf;
                result[ok ? 1 : 3] = c.Cross.gameObject.activeSelf;
            }
            return result;
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
            AnimatePressed(m, now);
            AnimateBlocked(m, now);
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
            if (_motorPill.Slab != null)
            {
                if (changed) PlasticSlab.SetCustom(_motorPill.Slab, EngranajesSprites.GoldChipGroup, EngranajesSprites.GoldChip);
                else PlasticSlab.SetCustom(_motorPill.Slab, EngranajesSprites.NeutralGroup, EngranajesSprites.NeutralPlate);
            }
            _motorPill.Bg.color = changed ? Gold : ButtonFill;
            _motorPill.Label.color = changed || (_motorPill.Slab != null && EngranajesSprites.HasCreamChip) ? NeuroStyle.Ink : ButtonText;
            float pulse = 0f;
            if (fixing && System.Array.IndexOf(m.Solution, EngranajesContract.MotorId) >= 0) pulse = 0.55f + 0.45f * Mathf.Sin((now - _showFixAt) * 5.9f);
            else if (_phase == Phase.Play && deco && m.Stage.Hint == Hint.Motor && now - _hintAt < 2.6f) pulse = 0.35f + 0.3f * Mathf.Sin(now * 4.5f);
            _motorPulse.gameObject.SetActive(pulse > 0.01f);
            _motorPulse.color = new Color(Gold.r, Gold.g, Gold.b, pulse);
        }

        /// <summary>Con plasticina, lo que se toca se ve PRESIONADO al menos 80 ms (<see cref="PlasticTouch.MinSeconds"/>): el engranaje del motor (más oscuro y más plano; el contorno de sus dientes no cambia) y las dos poleas de la correa tocada. El estado del juego no espera: el cambio ya está hecho.</summary>
        private void AnimatePressed(Machine m, float now)
        {
            if (!EngranajesSprites.Plastic) return;
            var g0 = m.Gears[0];
            var size = EngranajesSprites.SizeOf(g0);
            bool motorDown = _motorPressAt > 0f && now - _motorPressAt < PlasticTouch.MinSeconds;
            _gears[0].Body.sprite = (motorDown ? EngranajesSprites.GearPressed(size) : null) ?? EngranajesSprites.Gear(size, EngranajesSprites.PaletteOf(g0, 0));
            for (int b = 0; b < _beltLinks.Count; b++)
            {
                bool down = _beltPressAt.TryGetValue(_beltLinks[b], out float at) && at > 0f && now - at < PlasticTouch.MinSeconds;
                var pulley = (down ? EngranajesSprites.PulleyPressed() : null) ?? EngranajesSprites.Pulley();
                foreach (var pu in _belts[b].Pulleys) pu.sprite = pulley;
            }
        }

        private float _motorPressAt = -10f;
        private float _blockAt = -10f, _counterPulseAt = -10f;
        private int _blockId = int.MinValue;

        /// <summary>La moneda coral con la aspa sobre lo tocado cuando ya no quedaban llaves: aparece con un saltito y una sacudida corta (0,3 s; con «quitar animaciones» ni saltito ni sacudida: solo aparece y se apaga), se sostiene ~1 s y se desvanece. Va sobre el engranaje del motor o a la mitad de la correa tocada.</summary>
        private void AnimateBlocked(Machine m, float now)
        {
            float t = now - _blockAt;
            bool on = t >= 0f && t < BlockCoinSeconds && _blockId != int.MinValue;
            _blockCoin.gameObject.SetActive(on);
            if (!on) return;
            Vector2 at;
            if (_blockId == EngranajesContract.MotorId) at = new Vector2(m.Gears[0].X, m.Gears[0].Y);
            else
            {
                var l = m.Links[_blockId];
                at = new Vector2((m.Gears[l.A].X + m.Gears[l.B].X) * 0.5f, (m.Gears[l.A].Y + m.Gears[l.B].Y) * 0.5f);
            }
            bool deco = Motion.Decorative;
            float shake = deco && t < 0.3f ? Mathf.Sin(t * 70f) * 4f * (1f - t / 0.3f) : 0f;
            float pop = deco && t < 0.14f ? Mathf.Lerp(0.6f, 1.15f, t / 0.14f) : deco && t < 0.26f ? Mathf.Lerp(1.15f, 1f, (t - 0.14f) / 0.12f) : 1f;
            _blockCoin.rectTransform.anchoredPosition = B(at.x + shake, at.y);
            _blockCoin.rectTransform.localScale = Vector3.one * pop;
            float fade = Mathf.Clamp01((BlockCoinSeconds - t) / 0.25f);
            var c = _blockCoin.color; _blockCoin.color = new Color(c.r, c.g, c.b, fade);
        }
        private readonly Dictionary<int, float> _beltPressAt = new Dictionary<int, float>();

        /// <summary>Un toque aceptado en el motor (id del motor) o en una correa (el índice de su unión): se ve presionado.</summary>
        private void MarkPressed(int id)
        {
            if (id == EngranajesContract.MotorId) _motorPressAt = GameClock.Time;
            else _beltPressAt[id] = GameClock.Time;
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
            if (m.Stations.TryGetValue(Station.Antena, out int ia))
            {
                _dish.rectTransform.localEulerAngles = new Vector3(0f, 0f, -m.Gears[ia].A * Mathf.Rad2Deg);
                if (_dishCanto != null) _dishCanto.Refresh();
            }
            float flame = 0f;
            if (m.Stations.TryGetValue(Station.Turbina, out int it))
            {
                _fan.rectTransform.localEulerAngles = new Vector3(0f, 0f, -m.Gears[it].A * Mathf.Rad2Deg);
                if (_fanCanto != null) _fanCanto.Refresh();
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
            _rocketTitle.text = "Cohete número " + (_shownOrbit + 1);
            _orbitText.text = _shownOrbit > 0 ? $"{_shownOrbit} {(_shownOrbit == 1 ? "cohete" : "cohetes")} en órbita" : "";
        }

        /// <summary>Abajo: las llaves de la cuenta de cambios (se llenan de dorado al usarlas) y «Arrancar» (se hunde al tocarlo).</summary>
        private void AnimateBottom(float now)
        {
            if (_mach == null) return;
            bool can = _phase == Phase.Play;
            _counterGroup.alpha = can ? 1f : 0.5f;
            float pt = Mathf.Clamp01((now - _counterPulseAt) / 0.3f);
            float pulse = pt < 1f ? Mathf.Sin(pt * Mathf.PI) : 0f;                           // el contador de llaves late una vez al rechazar un toque (el color late siempre; el tamaño, solo con movimiento)
            _counterRoot.localScale = Vector3.one * (1f + (Motion.Decorative ? 0.12f * pulse : 0f));
            _counterText.color = _counterSlab != null ? Color.Lerp(NeuroStyle.InkMuted, new Color(0.72f, 0.2f, 0.1f), pulse) : Color.Lerp(Lavender, Coral, pulse);
            for (int k = 0; k < 2; k++)
            {
                bool shown = k < _mach.Stage.Keys;
                _wrenchBg[k].gameObject.SetActive(shown);
                _wrenches[k].gameObject.SetActive(shown);
                if (!shown) continue;
                bool used = k < _changes.Count;
                if (_keyArt)                                                                                          // cada llave en su moneda de plasticina: libre = lila; usada = dorada (late hacia coral al rechazar un toque)
                {
                    _wrenchBg[k].sprite = EngranajesSprites.KeyCoin(used);
                    _wrenchBg[k].color = used ? Color.Lerp(Color.white, new Color(1f, 0.72f, 0.66f), pulse) : Color.white;
                    continue;
                }
                _wrenchBg[k].color = used ? new Color(Gold.r, Gold.g, Gold.b, 0.18f) : new Color(1f, 1f, 1f, 0.06f);
                _wrenches[k].color = used ? Color.Lerp(Gold, Coral, pulse) : new Color(110f / 255f, 106f / 255f, 154f / 255f);
            }
            var b = _start;
            b.Group.alpha = can ? 1f : 0.45f;
            float pr = b.Slab != null || _startPressAt <= 0f ? 0f : Mathf.Max(0f, 1f - (now - _startPressAt) / 0.2f);          // con plasticina la losa presionada reemplaza el hundido de siempre
            b.Bg.color = Cyan;
            b.Rim.color = NeuroStyle.Ink;
            b.Label.color = NeuroStyle.Ink;
            b.Icon.color = _playArt ? Color.white : NeuroStyle.Ink;                                                // el triángulo de plasticina ya trae su tinta
            if (b.Slab != null)
            {
                PlasticSlab.Set(b.Slab, SlabSize.Button, SlabTone.Sky, b.Touch.Down);
                b.Root.localScale = Vector3.one * (1f + b.Touch.Bounce);                                           // el rebote al soltar, el mismo de las casillas de Rescate
            }
            var top = new Vector2(0f, -(b.Slab != null ? b.Touch.Drop : pr * 4f * _s));
            b.Bg.rectTransform.anchoredPosition = top;
            b.Rim.rectTransform.anchoredPosition = top;
            b.Icon.rectTransform.anchoredPosition = new Vector2((-b.Rect.width / 2f + 63f) * _s, 0f) + top;
            b.Label.rectTransform.anchoredPosition = new Vector2((-b.Rect.width / 2f + 88f + (b.Rect.width - 96f) / 2f) * _s, 0f) + top;
        }

        // ------------------------------------------------------------------ aviso, luz que vuela, chispas, tarjeta «NUEVO»

        /// <summary>
        /// Lo que dice el aviso (Tarea 179): la fila de consigna del marcador, en un solo sistema con todos los juegos (la franja de abajo se retiró). <paramref name="good"/> = lima con el visto; si no, coral con la equis. El fallo y su pista («Truco…») se dicen en dos tiempos y
        /// duran lo que duraba la franja (3,2 s): la pista ayuda a resolver.
        /// </summary>
        private void Tell(string title, string sub, bool good)
        {
            _sayTitleFull = title ?? "";
            _saySubFull = sub ?? "";
            _hud.Announce(_sayTitleFull, good ? AnnounceTone.Done : AnnounceTone.Error, string.IsNullOrEmpty(_saySubFull) ? 0f : StripSeconds, _saySubFull, subtitleTone: AnnounceTone.Info);          // la pista («Truco…») es ayuda: crema con el punto, no un segundo error
        }

        /// <summary>Cuánto se veía la franja de abajo con título y pista (segundos): lo que la fila no baja de decir cuando hay pista.</summary>
        public const float StripSeconds = 3.2f;

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
            var to = new Vector2(EngranajesLayout.LightsX0 + idx * EngranajesLayout.LightsStep, _lay.StripTop + 14f);
            float k = (now - _flyerAt) / (deco ? 0.7f : 0.01f);
            if (k >= 1f)
            {
                _flyerOn = false;
                _shownLights = Mathf.Min(EngranajesContract.RocketLights, _shownLights + 1);
                _lastLightAt = now;
                Emit(to, 14, 90f, 0.6f, Gold, 20f);
                WoodCore.Play(WoodSfx.Spark, Mathf.Clamp(idx + 1, 1, 8));     // la nota sube con cada luz (1 a 8)
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
                // el primer cuadro ya sale con su posición, tamaño y color: si se activara antes, nacería con los de fábrica (100 × 100, blanco, en el centro) hasta el siguiente AnimateSparks (Tarea 170)
                p.Img.rectTransform.anchoredPosition = P(p.Pos);
                p.Img.rectTransform.sizeDelta = Vector2.one * (p.Size * 2f * _s);
                p.Img.color = NeuroStyle.WithAlpha(p.Color, 0.9f);
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

        /// <summary>Para las pruebas (Tarea 114): en una máquina de 1 llave y en una de 2 usa todas las llaves y toca otra cosa; devuelve lo que NO pasó como debía: que no cambie nada, que salga la moneda con su imagen, que el aviso nombre lo cambiado y que tocar algo ya cambiado lo deshaga.</summary>
        public List<string> AuditBlockedTouch()
        {
            var problems = new List<string>();
            _s = 3f; _playW = 1080f; _playH = 1920f; _logicalH = 640f;
            _lay = EngranajesLayout.Compute(_logicalH);
            if (_rng == null) _rng = new System.Random(1);
            var bake = EngranajesSprites.Prewarm();
            while (bake.MoveNext()) { }
            AssignSprites();
            foreach (int level in new[] { 1, 10 })
            {
                var m = EngranajesContract.Generate(level, _rng);
                ShowMachine(m);
                int keys = m.Stage.Keys;
                string tag = "nivel " + level + " (" + keys + " llave" + (keys == 1 ? "" : "s") + "): ";
                var ids = new List<int>();
                foreach (var sw in m.Switches) ids.Add(sw.Id);
                for (int i = 0; i < keys; i++) ToggleSwitch(ids[i]);
                var before = new List<int>(_changes);
                if (before.Count != keys) problems.Add(tag + "no quedaron usadas todas las llaves (" + before.Count + ")");
                int other = ids.Find(i => !before.Contains(i));
                ToggleSwitch(other);
                if (_changes.Count != before.Count) problems.Add(tag + "el toque bloqueado cambió la cuenta de cambios");
                else for (int i = 0; i < before.Count; i++) if (_changes[i] != before[i]) problems.Add(tag + "el toque bloqueado cambió los cambios");
                AnimateBlocked(m, GameClock.Time);
                if (!_blockCoin.gameObject.activeSelf) problems.Add(tag + "no salió la moneda");
                if (_blockCoin.sprite == null) problems.Add(tag + "la moneda no tiene imagen");
                if (_sayTitleFull != EngranajesContract.BlockedTitle(keys)) problems.Add(tag + "el aviso dice «" + _sayTitleFull + "»");
                if (_saySubFull != EngranajesContract.BlockedHint(before, keys)) problems.Add(tag + "el aviso dice «" + _saySubFull + "» debajo");
                AnimateBlocked(m, GameClock.Time + BlockCoinSeconds + 0.1f);
                if (_blockCoin.gameObject.activeSelf) problems.Add(tag + "la moneda no se apagó al pasar su segundo");
                ToggleSwitch(before[0]);
                if (_changes.Count != keys - 1 || _changes.Contains(before[0])) problems.Add(tag + "tocar algo ya cambiado no lo deshizo");
                ClearMachine();
            }
            return problems;
        }

        /// <summary>Para las pruebas (Tarea 115, el volumen aprobado el 16-oct): arma máquinas de los niveles 1, 7 y 12 y devuelve lo que NO está como debe: las capas de la sombra blanda y del canto de los engranajes DEBAJO de todos ellos, el canto de cada engranaje (4,5 dp, 5 pasos) con su sombra de contacto, que el canto NO gire
        /// con el engranaje (aunque se gire 37°, su corrimiento en la pantalla sigue siendo hacia ABAJO), las placas de los carteles y del rótulo MOTOR, las luces con su aro y las bandas de las correas con canto. Sin plasticina no hay volumen: no devuelve nada.</summary>
        /// <summary>Los botones y los rótulos son los del kit, SIN nada de más encima (Tarea 116): «Arrancar», la cuenta de llaves, los carteles y el rótulo MOTOR no llevan canto propio, halo de texto ni teñido (lo que los separaba de VERDAD/DISPARATE y de la pausa).</summary>
        private void AuditKitButtons(List<string> problems, string tag)
        {
            void Bare(Graphic g, string what) { if (g != null && g.GetComponent<CantoEffect>() != null) problems.Add(tag + what + ": lleva canto o halo propio encima de la losa del kit"); }
            if (_start.Slab != null)
            {
                Bare(_start.Slab, "«Arrancar» (losa)"); Bare(_start.Label, "«Arrancar» (texto)"); Bare(_start.Icon, "«Arrancar» (ícono)");
                if (_start.Slab.color != Color.white) problems.Add(tag + "«Arrancar»: la losa del kit está teñida");
                if (_start.Touch == null) problems.Add(tag + "«Arrancar» sin estado presionado");
            }
            if (_counterSlab != null)
            {
                Bare(_counterSlab, "la cuenta de llaves (losa)"); Bare(_counterText, "la cuenta de llaves (texto)");
                if (_counterSlab.color != Color.white) problems.Add(tag + "la cuenta de llaves: la losa del kit está teñida");
            }
            for (int k = 0; k < 2; k++) { Bare(_wrenchBg[k], "la llave " + k + " (moneda)"); Bare(_wrenches[k], "la llave " + k); }
            foreach (var c in _cartels) { Bare(c.Name, "un cartel (nombre)"); Bare(c.Word, "un cartel (palabra)"); Bare(c.Icon, "un cartel (ícono)"); Bare(c.Check, "un cartel (✓)"); Bare(c.Cross, "un cartel (✗)"); }
            Bare(_motorPill.Label, "el rótulo MOTOR"); Bare(_blockCoin, "la moneda del toque bloqueado");
            if (_playArt && _start.Icon.sprite != EngranajesSprites.PlayIcon()) problems.Add(tag + "«Arrancar»: el triángulo no es el de plasticina");
        }

        public List<string> AuditVolume()
        {
            var problems = new List<string>();
            _s = 3f; _playW = 1080f; _playH = 1920f; _logicalH = 640f;
            _lay = EngranajesLayout.Compute(_logicalH);
            if (_rng == null) _rng = new System.Random(1);
            if (!_volume) return problems;
            var bake = EngranajesSprites.Prewarm();
            while (bake.MoveNext()) { }
            AssignSprites();
            if (_softLayer == null || _cantoLayer == null) { problems.Add("faltan las capas de la sombra blanda y del canto"); return problems; }
            int first = int.MaxValue;
            foreach (var g in _gears) first = Mathf.Min(first, g.Root.GetSiblingIndex());
            if (_softLayer.GetSiblingIndex() >= _cantoLayer.GetSiblingIndex()) problems.Add("la sombra blanda debe ir debajo del canto");
            if (_cantoLayer.GetSiblingIndex() >= first) problems.Add("el canto y la sombra de todos los engranajes deben ir DEBAJO de todos los engranajes (el canto de uno no tapa los dientes del vecino)");
            foreach (int level in new[] { 1, 7, 12 })
            {
                var m = EngranajesContract.Generate(level, _rng);
                ShowMachine(m);
                string tag = "nivel " + level + ": ";
                for (int i = 0; i < m.Gears.Count && i < GearPool; i++)
                {
                    var v = _gears[i];
                    if (v.Soft == null || v.Canto == null) { problems.Add(tag + "engranaje " + i + ": sin sombra blanda o sin canto"); continue; }
                    if (!v.Soft.gameObject.activeSelf || !v.Shadow.gameObject.activeSelf) problems.Add(tag + "engranaje " + i + ": su canto o su sombra están apagados");
                    if (v.Soft.transform.parent != _softLayer || v.Shadow.transform.parent != _cantoLayer) problems.Add(tag + "engranaje " + i + ": el canto y la sombra no están en sus capas");
                    if (Mathf.Abs(v.Canto.Distance - 4.5f * Su) > 0.01f || v.Canto.Steps != CantoEffect.DefaultSteps) problems.Add(tag + "engranaje " + i + ": el canto no baja 4,5 dp en " + CantoEffect.DefaultSteps + " pasos");
                    // el canto no gira: se gira el engranaje 37° y el corrimiento del canto, visto en la pantalla, sigue siendo hacia abajo
                    ApplyAngle(v, 37f * Mathf.Deg2Rad);
                    var vh = new UnityEngine.UI.VertexHelper();
                    vh.AddVert(new Vector3(-10f, -10f, 0f), Color.white, Vector2.zero); vh.AddVert(new Vector3(-10f, 10f, 0f), Color.white, Vector2.zero);
                    vh.AddVert(new Vector3(10f, 10f, 0f), Color.white, Vector2.zero); vh.AddVert(new Vector3(10f, -10f, 0f), Color.white, Vector2.zero);
                    vh.AddTriangle(0, 1, 2); vh.AddTriangle(2, 3, 0);
                    v.Canto.ModifyMesh(vh);
                    var stream = new List<UIVertex>();
                    vh.GetUIVertexStream(stream);
                    vh.Dispose();
                    if (stream.Count != 6 * (CantoEffect.DefaultSteps + 1)) { problems.Add(tag + "engranaje " + i + ": la malla del canto tiene " + stream.Count + " vértices"); continue; }
                    var local = stream[0].position - stream[stream.Count - 6].position;                      // la copia más honda menos la cara
                    var onScreen = Quaternion.Euler(0f, 0f, v.Shadow.rectTransform.localEulerAngles.z) * local;
                    if (Mathf.Abs(onScreen.x) > 0.05f || Mathf.Abs(onScreen.y + 4.5f * Su) > 0.05f) problems.Add(tag + "engranaje " + i + ": al girar, el canto se corrió a " + onScreen + " en la pantalla (debía ser hacia abajo)");
                    ApplyAngle(v, m.Gears[i].A);
                }
                for (int s2 = 0; s2 < 4; s2++)
                {
                    var c = _cartels[s2];
                    if (!c.Root.gameObject.activeSelf) continue;
                    if (EngranajesSprites.HasPlates && (c.Slab == null || c.Slab.sprite == null)) problems.Add(tag + "el cartel " + (Station)s2 + " no tiene su placa");
                }
                if (EngranajesSprites.HasPlates && (_motorPill.Slab == null || _motorPill.Slab.sprite == null)) problems.Add(tag + "el rótulo MOTOR no tiene su pastilla");
                for (int k = 0; k < _windows.Length; k++)
                    if (_windowRings[k] == null || _windowRings[k].sprite == null || _windows[k].sprite != EngranajesSprites.Dome()) problems.Add(tag + "la luz " + k + " no tiene su aro y su domo");
                for (int b = 0; b < _beltLinks.Count; b++)
                    if (_belts[b].Lines[2].Canto == null || _belts[b].Caps[2] == null || _belts[b].CapCanto[2] == null) problems.Add(tag + "la correa " + b + " no tiene el canto de su banda oscura");
                AuditKitButtons(problems, tag);
                ClearMachine();
            }
            return problems;
        }

        // ------------------------------------------------------------------ la mesa de taller y el aviso crema (Tarea 164)

        /// <summary>Para las pruebas: la mesa de taller (losa crema DEBAJO de todo, con la ventanilla noche del tamaño y en el lugar del panel de antes, un paso más clara que el cielo y hundida, y los cuatro tornillos en el marco, no sobre la ventanilla)
        /// y el aviso crema (losa del kit, letra en tinta, la moneda ✓ o ✗ y la losa que no baja del alto mínimo).</summary>
        public List<string> AuditTable()
        {
            var problems = new List<string>();
            _s = 3f; _playW = 1080f; _playH = 1920f; _logicalH = 640f;
            _lay = EngranajesLayout.Compute(_logicalH);
            if (_rng == null) _rng = new System.Random(1);
            var bake = EngranajesSprites.Prewarm();
            while (bake.MoveNext()) { }
            AssignSprites();
            if (!_tableOn) { problems.Add("no hay mesa de taller (¿el atlas «comun» trae la losa crema mediana?)"); return problems; }
            var table = _table.rectTransform;
            float tw = EngranajesLayout.TableRight - EngranajesLayout.TableLeft;
            if (Mathf.Abs(table.sizeDelta.x - tw * Su) > 0.01f || Mathf.Abs(table.sizeDelta.y - EngranajesLayout.SceneHeight * Su) > 0.01f) problems.Add("la mesa no mide " + tw + " × " + EngranajesLayout.SceneHeight + " dp del boceto: mide " + table.sizeDelta / Su);
            if (_tableSlab == null || _tableSlab.sprite == null) problems.Add("la mesa no tiene su losa crema");
            if (_table.color.a > 0.01f) problems.Add("el fondo de la mesa debe quedar invisible (la losa es la que se ve)");
            var win = _room.rectTransform;
            if (Mathf.Abs(win.sizeDelta.x - EngranajesLayout.WindowW * Su) > 0.01f || Mathf.Abs(win.sizeDelta.y - EngranajesLayout.WindowH * Su) > 0.01f) problems.Add("la ventanilla no mide lo mismo que el panel de antes (242 × 348 dp)");
            if ((win.anchoredPosition - B(EngranajesLayout.WindowCx, EngranajesLayout.WindowCy)).magnitude > 0.01f) problems.Add("la ventanilla no está donde estaba el panel: la máquina se movería");
            if (_room.sprite != EngranajesSprites.Window()) problems.Add("la sala no usa la ventanilla hundida");
            float wl = EngranajesLayout.WindowCx - EngranajesLayout.WindowW / 2f, wr = EngranajesLayout.WindowCx + EngranajesLayout.WindowW / 2f, wt = EngranajesLayout.WindowCy - EngranajesLayout.WindowH / 2f, wb = EngranajesLayout.WindowCy + EngranajesLayout.WindowH / 2f;
            if (wl - EngranajesLayout.TableLeft < 12f || EngranajesLayout.SceneTop + 12f > wt || EngranajesLayout.SceneBottom - wb < 12f) problems.Add("el marco crema de la mesa mide menos de 12 dp alrededor de la ventanilla");
            if (EngranajesLayout.TableRight < EngranajesLayout.CartelRight + 4f) problems.Add("los carteles (hasta " + EngranajesLayout.CartelRight + ") se salen de la mesa");
            for (int k = 0; k < _screws.Length; k++)
            {
                var p = _screws[k].rectTransform.anchoredPosition / Su;                                                    // en coordenadas del boceto relativas al centro de la escena
                float bx = p.x + 180f, by = 280f - p.y;
                if (bx > wl - 1f && bx < wr + 1f && by > wt - 1f && by < wb + 1f) problems.Add("el tornillo " + k + " cae sobre la ventanilla: va en el marco");
                if (bx < EngranajesLayout.TableLeft || bx > EngranajesLayout.TableRight || by < EngranajesLayout.SceneTop || by > EngranajesLayout.SceneBottom) problems.Add("el tornillo " + k + " se sale de la mesa");
            }
            // el tono de la ventanilla: un paso más claro que el cielo (#1C1B52) y más oscuro que la mesa; hundida: más oscura junto al borde de arriba y de la izquierda que junto al de abajo y de la derecha
            var tex = EngranajesSprites.Window().texture;
            Color At(int x, int y) => tex.GetPixel(x, y);
            var center = At(tex.width / 2, tex.height / 2);
            var sky = NeuroStyle.NightBottom;
            if (!(center.b > sky.b + 0.01f && center.r > sky.r + 0.01f && center.b < 0.5f)) problems.Add("la ventanilla debe ser un paso más clara que el cielo " + sky + " y seguir siendo noche: es " + center);
            var left = At(5, tex.height / 2); var right = At(tex.width - 6, tex.height / 2);
            if (!(left.r + left.g + left.b < right.r + right.g + right.b)) problems.Add("la ventanilla no se ve hundida: junto al borde izquierdo debía ser más oscura que junto al derecho (" + left + " contra " + right + ")");
            // el aviso va por la fila de consigna (Tarea 179): coral con la equis si falla, lima con el visto si acierta
            Tell("La turbina giró al revés de su cartel", "Truco: el de al lado gira al revés", false);
            if (!_hud.Announcing || _hud.AnnounceToneNow != AnnounceTone.Error) problems.Add("un error lo dice la fila de consigna en coral con la equis");
            Tell("¡Todo en orden!", null, true);
            if (!_hud.Announcing || _hud.AnnounceToneNow != AnnounceTone.Done) problems.Add("un acierto lo dice la fila de consigna en lima con el visto");
            _hud.CancelAnnounce();
            return problems;
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
            if (_tableOn)
            {
                Check("mesa (losa crema)", _tableSlab, true);
                for (int k = 0; k < _screws.Length; k++) Check("tornillo " + k, _screws[k], true);
                if (_table.transform.GetSiblingIndex() >= _room.transform.GetSiblingIndex()) problems.Add("la mesa va DEBAJO de la sala");
                if (_room.transform.GetSiblingIndex() >= _rocketRoot.GetSiblingIndex() || _rocketRoot.GetSiblingIndex() >= _machineLayer.GetSiblingIndex()) problems.Add("la sala va debajo del cohete y el cohete debajo de la máquina");
                foreach (var screw in _screws) if (screw.transform.GetSiblingIndex() >= _rocketRoot.GetSiblingIndex()) problems.Add("un tornillo va encima del cohete");
            }
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
                        if (_volume) { Check(tag + "engranaje " + i + ".sombra blanda", v.Soft, true); if (v.Canto == null) problems.Add(tag + "engranaje " + i + ": sin canto"); }
                        Check(tag + "engranaje " + i + ".cubo", v.Hub, true);
                        Check(tag + "engranaje " + i + ".soporte", v.Support, true);
                    }
                    Check(tag + "flecha del motor", _motorArrow, false);
                    Check(tag + "MOTOR", _motorPill.Bg, true);
                    if (_counterSlab != null) Check(tag + "cuenta de cambios (losa)", _counterSlab, true);                 // con plasticina el fondo es la losa del kit y el Image de siempre queda invisible a propósito
                    else Check(tag + "cuenta de cambios", _counterBg, true);
                    Check(tag + "cuenta de cambios.texto", _counterText, false);
                    Check(tag + "Arrancar.fondo", _start.Bg, true);
                    Check(tag + "Arrancar.borde", _start.Rim, true);
                    Check(tag + "Arrancar.ícono", _start.Icon, true);
                    if (PlasticSlab.Available(SlabSize.Button, SlabTone.Sky) && _start.Slab == null) problems.Add(tag + "Arrancar: falta la losa de plasticina");
                    if (_start.Slab != null && _start.Touch == null) problems.Add(tag + "Arrancar: sin estado presionado");
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
                        Check(tag + "cartel " + kv.Key + ".✓", cv.Check, true, false);
                        Check(tag + "cartel " + kv.Key + ".aspa", cv.Cross, true, false);
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
