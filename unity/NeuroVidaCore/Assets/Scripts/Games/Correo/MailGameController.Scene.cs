using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Correo
{
    public sealed partial class MailGameController
    {
        // lo que se ve de cada momento (tiempos del reloj de juego)
        private readonly Dictionary<MailLetter, LetterView> _lvOf = new Dictionary<MailLetter, LetterView>();
        private float _peekAt = -100f, _showAt = -100f, _radioAt = -100f, _rushAt = -100f, _safePressAt = -100f, _beaconPressAt = -100f, _safeOkAt = -100f, _safeWarnAt = -100f, _beaconWarnAt = -100f, _beaconOnAt = -100f, _comboFxAt = -100f;
        private int _comboTier;
        private bool _sackDone = true;
        private string _lastCards;
        private readonly List<MailTodo> _briefItems = new List<MailTodo>();
        private bool _briefRoutineCard;

        // ------------------------------------------------------------------ capas y reinicios

        /// <summary>Muestra una sola pantalla encima de la estación (hoja, resumen, «NUEVO» o final); null = solo la estación (o nada si no hay día).</summary>
        private void ShowOnly(RectTransform layer)
        {
            if (_briefLayer == null) return;
            _briefLayer.gameObject.SetActive(layer == _briefLayer);
            _recapLayer.gameObject.SetActive(layer == _recapLayer);
            _introLayer.gameObject.SetActive(layer == _introLayer);
            _endLayer.gameObject.SetActive(layer == _endLayer);
            bool stage = _day != null && _phase != Phase.Done;
            foreach (var l in new[] { _showBackLayer, _stageLayer, _flyLayer, _showFrontLayer, _bannerLayer, _peekLayer, _floatLayer, _sparkLayer }) l.gameObject.SetActive(stage);
            if (stage) LayoutBoxes();
        }

        private void ResetViews()
        {
            if (_lv[0] == null) return;
            ClearBelt();
            foreach (var f in _floats) { f.At = -100f; f.Root.gameObject.SetActive(false); }
            foreach (var sp in _sparks) { sp.Alive = false; sp.Img.gameObject.SetActive(false); }
            _peekAt = _showAt = _radioAt = _rushAt = _safePressAt = _beaconPressAt = _safeOkAt = _safeWarnAt = _beaconWarnAt = _beaconOnAt = _comboFxAt = -100f;
            _comboTier = 0;
            _sackDone = true;
            _lastCards = null;
            foreach (var g in new[] { _rushGroup, _radioGroup }) g.gameObject.SetActive(false);
            _peekGroup.gameObject.SetActive(false);
            foreach (var i in new[] { _wash, _flareA, _flareB, _ship, _sackGlow, _shipSack }) i.gameObject.SetActive(false);
            _halo.gameObject.SetActive(false);
            _beam.gameObject.SetActive(false);
        }

        private void BindDay(MailDay day)
        {
            ResetViews();
            _boxCount = day.Stage.Boxes;
            _lay = MailLayout.Compute(_logicalH - (_guided ? TutorialControlsDp : 0f));
            LayoutStage();
            ShowOnly(null);
        }

        private void ClearBelt()
        {
            foreach (var v in _lv)
            {
                if (v == null) continue;
                v.InUse = v.Flying = v.Falling = false;
                v.L = null;
                v.Root.gameObject.SetActive(false);
                if (v.Root.parent != _lettersLayer) v.Root.SetParent(_lettersLayer, false);
            }
            _lvOf.Clear();
        }

        // ------------------------------------------------------------------ las cartas

        private LetterView Acquire(MailLetter l, float x)
        {
            LetterView v = null;
            foreach (var c in _lv) if (!c.InUse) { v = c; break; }
            if (v == null) v = _lv[0];                                  // el pool alcanza para la cinta (7) y las que vuelan: no debería pasar
            v.InUse = true;
            v.Flying = v.Falling = false;
            v.L = l;
            v.X = x;
            v.Vx = 0f;
            v.Y = _lay.BeltY;
            v.Bounce = -100f;
            v.Scale = 1f;
            v.Root.SetParent(_lettersLayer, false);
            v.Img.sprite = MailSprites.Letter(l.Planet, l.Cue);
            v.Img.color = Color.white;
            v.Img.rectTransform.sizeDelta = new Vector2(MailSprites.LetterBoxW * _s, MailSprites.LetterBoxH * _s);
            v.Glow.rectTransform.sizeDelta = new Vector2(66f * _s, 66f * _s);
            v.Glow.rectTransform.anchoredPosition = new Vector2(MailSprites.SealCx(true) * _s, -MailSprites.SealCy(true) * _s);
            v.Glow.gameObject.SetActive(l.Cue == MailCue.Gold);
            v.Root.gameObject.SetActive(true);
            _lvOf[l] = v;
            return v;
        }

        private LetterView ViewFor(MailLetter l, float spawnX)
        {
            if (_lvOf.TryGetValue(l, out var v)) return v;
            return Acquire(l, spawnX);
        }

        private LetterView Detach(MailLetter l)
        {
            if (_lvOf.TryGetValue(l, out var v)) { _lvOf.Remove(l); return v; }
            v = Acquire(l, MailLayout.BeltXA);
            _lvOf.Remove(l);
            return v;
        }

        private bool FrontReady()
        {
            if (_day == null || _day.Belt.Count == 0) return false;
            return _lvOf.TryGetValue(_day.Belt[0], out var v) && Mathf.Abs(v.X - MailLayout.BeltXA) <= MailContract.ReadyDistance;
        }

        private void StartFlight(LetterView v, float toX, float toY, float seconds, int into, int box)
        {
            v.Flying = true;
            v.FromX = v.X;
            v.FromY = v.Y;
            v.ToX = toX;
            v.ToY = toY;
            v.At = Now;
            v.Dur = Motion.Decorative ? seconds : 0.001f;
            v.Into = into;
            v.Box = box;
            v.Root.SetParent(_flyLayer, false);
        }

        private void StartFall(LetterView v)
        {
            v.Falling = true;
            v.At = Now;
            v.FromX = v.X;
            v.Root.SetParent(_flyLayer, false);
        }

        // ------------------------------------------------------------------ las acciones del juego (las de los toques, las del tutorial y las del auto-juego del smoke)

        private void DoBox(int box)
        {
            if (_day == null || _day.Ended) return;
            var res = _day.TapBox(box, FrontReady());
            if (res.Kind == MailTap.Ignored) return;
            var v = Detach(res.Letter);
            var r = MailLayout.BoxRect(box, _day.Stage.Boxes, _lay.BoxY0);
            StartFlight(v, r.Cx, r.Y + 30f, MailMotion.FlyBoxSeconds, 1, box);
            var b = _boxes[box];
            if (res.Kind == MailTap.BoxRight)
            {
                b.PulseAt = Now;
                PlayClip(MailSounds.BoxNote(box), 0.5f);
                if (res.Combo >= 3) PlayClip(MailSounds.ComboBell(res.Combo), 0.35f);
                if (res.ComboTier > 0) ComboTierFx(res.ComboTier, res.Combo);
            }
            else
            {
                b.ShakeAt = Now;
                PlayClip(MailSounds.Thud(), 0.6f);
                AddFloat(r.Cx, r.Y - 10f, MailContract.FloatWrongBox, Salmon);
            }
            if (res.CueMissed) CueMissedFx(res.Letter);
        }

        private void CueMissedFx(MailLetter l)
        {
            _safeWarnAt = Now;
            AddFloatAbove(_lay.Safe, l.Cue == MailCue.Gold ? MailContract.FloatGoldMissed : MailContract.FloatLazoMissed, GoldSoft);
            PlayClip(MailSounds.Soft(), 0.5f);
        }

        private void ComboTierFx(int tier, int combo)
        {
            _comboFxAt = Now;
            _comboTier = tier;
            AddFloat(MailLayout.BeltXA, _lay.BeltY - 70f, MailContract.FloatCombo(combo), Gold, true);
            PlayClip(MailSounds.Tier(tier), 0.7f);
            if (tier >= 3)
            {
                var cols = new[] { Gold, Cyan, Violet, Lime };
                for (int k = 0; k < 4; k++) Emit(new Vector2(40f + (float)_rng.NextDouble() * 280f, 90f + (float)_rng.NextDouble() * 120f), 18, 170f, 0.9f, cols[k], 20f);
            }
        }

        private void DoSafe()
        {
            if (_day == null || _day.Ended) return;
            var res = _day.TapSafe(FrontReady());
            _safePressAt = Now;
            var s = _lay.Safe;
            switch (res.Kind)
            {
                case MailTap.SafeCue:
                    StartFlight(Detach(res.Letter), s.Cx, s.Y + 30f, MailMotion.FlySafeSeconds, 2, -1);
                    AddFloatAbove(s, MailContract.FloatCueDone, Gold, true);
                    PlayClip(MailSounds.Fanfare(), 0.8f);
                    Emit(new Vector2(s.Cx, s.Y + 20f), 24, 160f, 0.8f, Gold, 20f);
                    _safeOkAt = Now;
                    break;
                case MailTap.SafeWrongLetter:
                    if (_lvOf.TryGetValue(res.Letter, out var v)) v.Bounce = Now;
                    AddFloatAbove(s, MailContract.FloatNotSafe, Salmon);
                    PlayClip(MailSounds.Thud(), 0.6f);
                    break;
                default:
                    PlayClip(MailSounds.Soft(), 0.4f);
                    break;
            }
        }

        private void DoBeacon()
        {
            if (_day == null || _day.Ended) return;
            var res = _day.TapBeacon();
            _beaconPressAt = Now;
            var b = _lay.Beacon;
            switch (res.Kind)
            {
                case MailTap.BeaconHit:
                    _beaconOnAt = Now;
                    _showAt = Now;
                    _sackDone = false;
                    AddFloatAbove(b, MailContract.FloatBeaconOn, Gold, true);
                    PlayClip(MailSounds.Fanfare(), 0.8f);
                    PlayClip(MailSounds.Horn(), 0.6f);
                    Emit(new Vector2(b.Cx, b.Y + 20f), 24, 160f, 0.8f, new Color(1f, 214f / 255f, 120f / 255f), 20f);
                    // destellos que suben por el cielo (empiezan escalonados)
                    if (Motion.Decorative)
                        for (int k = 0; k < 26; k++)
                            EmitDelayed(new Vector2((float)_rng.NextDouble() * MailLayout.W, 60f + (float)_rng.NextDouble() * (_lay.BeltY - 130f)), new Color(1f, 240f / 255f, 190f / 255f), (float)_rng.NextDouble() * 0.9f, 0.9f + (float)_rng.NextDouble() * 0.7f);
                    break;
                case MailTap.BeaconCommission:
                    AddFloatAbove(b, MailContract.FloatCancelled, Salmon);
                    PlayClip(MailSounds.Thud(), 0.6f);
                    break;
                case MailTap.BeaconEarly:
                    AddFloatAbove(b, MailContract.FloatTooEarly, GoldSoft);
                    PlayClip(MailSounds.Soft(), 0.5f);
                    break;
                default:
                    AddFloatAbove(b, MailContract.FloatNotNow, GoldSoft);
                    PlayClip(MailSounds.Soft(), 0.5f);
                    break;
            }
        }

        private void DoClock()
        {
            if (_day == null || _day.Ended) return;
            var res = _day.TapClock();
            if (res.Kind != MailTap.Peek) return;
            _peekAt = Now;
            PlayClip(MailSounds.Tick(), 0.5f);
        }

        /// <summary>Lo que pasa solo durante el día (cartas que caen, el saco, la radio, la ventana de hora que se cierra).</summary>
        private void ProcessEvents()
        {
            if (_day.Events.Count == 0) return;
            foreach (var e in _day.Events.ToList())
            {
                switch (e.Kind)
                {
                    case MailEventKind.Fell:
                        StartFall(Detach(e.Letter));
                        PlayClip(MailSounds.Thud(), 0.5f);
                        if (e.WasCue) CueMissedFx(e.Letter);
                        break;
                    case MailEventKind.RushStarted:
                        _rushAt = Now;
                        PlayClip(MailSounds.Sack(), 0.7f);
                        break;
                    case MailEventKind.RadioCancelled:
                        _radioAt = Now;
                        _radioText.text = MailContract.RadioText(e.Todo.Moment);
                        PlayClip(MailSounds.Radio(), 0.7f);
                        break;
                    case MailEventKind.WindowClosed:
                        _beaconWarnAt = Now;
                        AddFloatAbove(_lay.Beacon, MailContract.FloatLateWindow(e.Todo.Moment), GoldSoft);
                        PlayClip(MailSounds.Soft(), 0.5f);
                        break;
                }
            }
            _day.Events.Clear();
        }

        // ------------------------------------------------------------------ cada cuadro

        private void AnimateAll(float now, float dt)
        {
            if (_lay.BeltY <= 0f) return;
            if (_day != null && _stageLayer.gameObject.activeSelf)
            {
                AnimateHeader();
                AnimateBelt(now, dt);
                AnimateFlyers(now);
                AnimateBoxes(now);
                AnimateButtons(now);
                AnimateClock(now);
                AnimateShow(now);
                AnimateBanners(now);
            }
            AnimateFloats(now);
            AnimateSparks(dt);
            AnimateOverlays(now);
        }

        private void AnimateHeader()
        {
            string cards = MailContract.CartasLine(_day.Right, _day.Late);
            if (cards != _lastCards) { _lastCards = cards; _hdrCards.text = cards; }
            _hint.gameObject.SetActive(_phase == Phase.Play);
        }

        private void AnimateBelt(float now, float dt)
        {
            bool deco = Motion.Decorative;
            var m = _lay;
            // los rodillos de la cinta giran
            float off = deco ? (now / 0.012f) % 28f : 0f;
            for (int i = 0; i < _rollers.Length; i++)
            {
                float x = -28f + off + i * 28f;
                SetRect(_rollers[i].rectTransform, x + 7f, m.BeltY + 44f, 14f, 4f);
            }
            // el marco de la carta que espera: se enciende con la racha de 10
            bool lit = _day.Combo >= MailContract.ComboStep && deco;
            _frame.color = lit ? Gold : new Color(Cyan.r, Cyan.g, Cyan.b, 0.5f);
            _frameGlow.color = new Color(Gold.r, Gold.g, Gold.b, lit ? 0.35f + 0.15f * Mathf.Sin(now / 0.12f) : 0f);
            // el destello que recorre la cinta (escalón de ×20 y ×30)
            if (_comboTier >= 2 && deco && now - _comboFxAt < 1f)
            {
                float k = (now - _comboFxAt) / 1f, x = -60f + (MailLayout.W + 120f) * k;
                SetRect(_shine.rectTransform, x, m.BeltY, 160f, 130f);
                _shine.color = new Color(1f, 236f / 255f, 170f / 255f, 0.45f);
            }
            else _shine.color = new Color(1f, 236f / 255f, 170f / 255f, 0f);
            // las cartas: cada una avanza con un resorte hacia su lugar en la fila
            var belt = _day.Belt;
            for (int i = 0; i < belt.Count; i++)
            {
                var l = belt[i];
                var v = ViewFor(l, MailLayout.W + 60f);
                float target = MailLayout.SlotX(i);
                if (!deco) { v.X = target; v.Vx = 0f; }
                else MailMotion.Spring(ref v.X, ref v.Vx, target, dt, MailMotion.BeltFreq, MailMotion.BeltZeta);
                v.Y = m.BeltY;
                float k = i == 0 ? 1f : 0.7f;
                float shake = 0f;
                if (v.Bounce > -50f && now - v.Bounce < 0.4f && deco) shake = Mathf.Sin((now - v.Bounce) / 0.03f) * 5f * (1f - (now - v.Bounce) / 0.4f);
                v.Root.anchoredPosition = P(v.X + shake, v.Y);
                v.Root.localScale = Vector3.one * k;
                v.Root.localRotation = Quaternion.identity;
                v.Root.SetSiblingIndex(belt.Count - 1 - i);            // la carta de adelante se dibuja encima de las de atrás
                bool gold = l.Cue == MailCue.Gold;
                // el resplandor del sello dorado late; con «quitar animaciones» queda fijo (la señal no depende de que algo se mueva)
                v.Glow.color = new Color(1f, 214f / 255f, 120f / 255f, gold ? (deco ? 0.6f + 0.4f * Mathf.Sin(now / 0.16f) : 0.7f) : 0f);
            }
            // si una carta ya no está en la cinta (la guardó un efecto) y no vuela ni cae, se suelta
            foreach (var v in _lv)
                if (v.InUse && !v.Flying && !v.Falling && (v.L == null || !belt.Contains(v.L))) { v.InUse = false; v.Root.gameObject.SetActive(false); }
            // la píldora de la racha y la de las que esperan fuera de la pantalla
            if (_day.Combo >= 3)
            {
                _comboText.text = "Racha ×" + _day.Combo;
                float w = _comboText.preferredWidth / _s + 22f;
                _comboPill.gameObject.SetActive(true);
                SetRect(_comboPill.rectTransform, MailLayout.BeltXA, m.BeltY - 84f + 13f, w, 26f);
                SetChild(_comboText.rectTransform, 0f, 0.5f, w + 20f, 26f);                 // hijo de la píldora: se posiciona por desplazamiento, no con SetRect
            }
            else _comboPill.gameObject.SetActive(false);
            int waiting = belt.Count - MailContract.BeltCapacity;
            if (waiting > 0)
            {
                _waitText.text = "+" + waiting;
                float w = _waitText.preferredWidth / _s + 18f;
                _waitPill.gameObject.SetActive(true);
                SetRect(_waitPill.rectTransform, MailLayout.W - w / 2f - 8f, m.BeltY - 62f + 12f, w, 24f);
                SetChild(_waitText.rectTransform, 0f, 0.5f, w + 20f, 24f);
            }
            else _waitPill.gameObject.SetActive(false);
        }

        private void AnimateFlyers(float now)
        {
            foreach (var v in _lv)
            {
                if (!v.InUse) continue;
                if (v.Flying)
                {
                    float k = Mathf.Clamp01((now - v.At) / v.Dur), e = MailMotion.EaseInOut(k);
                    float x = Mathf.Lerp(v.FromX, v.ToX, e), y = Mathf.Lerp(v.FromY, v.ToY, e) - Mathf.Sin(Mathf.PI * e) * 50f;
                    if (k >= 1f)
                    {
                        if (v.Into == 1 && Motion.Decorative) Emit(new Vector2(v.ToX, v.ToY), 8, 70f, 0.45f, PlanetGlow[Mathf.Clamp(v.Box, 0, 3)], 20f);
                        v.InUse = v.Flying = false;
                        v.Root.gameObject.SetActive(false);
                        continue;
                    }
                    v.Root.anchoredPosition = P(x, y);
                    v.Root.localScale = Vector3.one * (1f - 0.6f * e);
                    v.Root.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sin(Mathf.PI * e) * 0.4f * Mathf.Rad2Deg);
                    v.Glow.color = new Color(1f, 214f / 255f, 120f / 255f, 0f);
                }
                else if (v.Falling)
                {
                    float k = (now - v.At) / MailMotion.FallSeconds;
                    if (k >= 1f || !Motion.Decorative && k >= 0.25f) { v.InUse = v.Falling = false; v.Root.gameObject.SetActive(false); continue; }
                    float x = MailLayout.BeltXA - 40f - k * 30f, y = _lay.BeltY + k * k * 160f;
                    v.Root.anchoredPosition = P(Motion.Decorative ? x : v.FromX, Motion.Decorative ? y : _lay.BeltY);
                    v.Root.localScale = Vector3.one * (0.9f - 0.3f * k);
                    v.Root.localRotation = Quaternion.identity;
                    v.Img.color = new Color(1f, 1f, 1f, Motion.Decorative ? 1f - k : 0.6f);
                    v.Glow.color = new Color(1f, 214f / 255f, 120f / 255f, 0f);
                }
            }
        }

        private void AnimateBoxes(float now)
        {
            for (int i = 0; i < _boxes.Length; i++)
            {
                var b = _boxes[i];
                if (!b.Root.gameObject.activeSelf) continue;
                var r = MailLayout.BoxRect(i, _boxCount, _lay.BoxY0);
                float sx = 0f;
                bool deco = Motion.Decorative;
                if (deco && now - b.ShakeAt < 0.38f) sx = Mathf.Sin((now - b.ShakeAt) / 0.026f) * 4f * (1f - (now - b.ShakeAt) / 0.38f);
                float pk = now - b.PulseAt < 0.26f ? Mathf.Sin(Mathf.PI * (now - b.PulseAt) / 0.26f) : 0f;
                SetRect(b.Root, r.Cx + sx, r.Cy, r.W, r.H);
                b.Glow.color = new Color(PlanetGlow[i].r, PlanetGlow[i].g, PlanetGlow[i].b, deco ? pk * 0.8f : (pk > 0.2f ? 0.5f : 0f));
                float R = Mathf.Min(28f, r.W * 0.3f);
                SetChild(b.Planet.rectTransform, 0f, 60f - r.H / 2f - (deco ? pk * 3f : 0f), MailSprites.PlanetBox * R / MailSprites.PlanetR, MailSprites.PlanetBox * R / MailSprites.PlanetR);
            }
        }

        private void AnimateButtons(float now)
        {
            bool deco = Motion.Decorative;
            float ps = deco && now - _safePressAt < 0.14f ? 0.96f : 1f;
            _safeRoot.localScale = Vector3.one * ps;
            float pb = deco && now - _beaconPressAt < 0.14f ? 0.96f : 1f;
            _beaconRoot.localScale = Vector3.one * pb;
            // la caja fuerte: brilla al guardar un encargo (el dial gira) o con un encargo que se perdió
            bool ok = now - _safeOkAt < 0.9f, warn = now - _safeWarnAt < 1.5f;
            float a = ok ? (deco ? 0.9f : 0.6f) : warn ? (deco ? 0.6f + 0.4f * Mathf.Sin(now / 0.09f) : 0.6f) : 0f;
            _safeGlow.color = new Color(Gold.r, Gold.g, Gold.b, Mathf.Clamp01(a));
            _dial.rectTransform.localRotation = Quaternion.Euler(0f, 0f, ok && deco ? -(now - _safeOkAt) / 0.2f * Mathf.Rad2Deg : 0f);
            // el faro: la ventana que se pasó brilla; encendido a tiempo, la lámpara y dos haces
            bool bwarn = now - _beaconWarnAt < 1.5f;
            _beaconGlow.color = new Color(Gold.r, Gold.g, Gold.b, bwarn ? (deco ? 0.5f + 0.4f * Mathf.Sin(now / 0.09f) : 0.5f) : 0f);
            bool on = now - _beaconOnAt < MailMotion.ShowSeconds;
            if (_lamp.sprite != null) _lamp.sprite = MailSprites.Lamp(on);
            _lampGlow.color = new Color(1f, 226f / 255f, 122f / 255f, on && deco ? 1f : 0f);
            _lampBeamA.gameObject.SetActive(on);
            _lampBeamB.gameObject.SetActive(on);
            if (on)
            {
                float k = (now - _beaconOnAt) / MailMotion.ShowSeconds;
                float al = deco ? Mathf.Sin(Mathf.PI * Mathf.Min(1f, k * 1.2f)) * 0.8f : 0.7f;
                float sw = deco ? Mathf.Sin(now / 0.18f) * 0.5f : 0f;
                var c = new Color(1f, 231f / 255f, 168f / 255f, Mathf.Clamp01(al));
                _lampBeamA.Set(-sw, 0.197f, 130f * _s, c, 0.9f, 0.5f);
                _lampBeamB.Set(Mathf.PI - sw, 0.197f, 130f * _s, c, 0.9f, 0.5f);
            }
        }

        private void AnimateClock(float now)
        {
            bool open = now - _peekAt < MailContract.PeekSeconds;
            float k = open ? Mathf.Min(1f, Mathf.Min((now - _peekAt) / 0.16f, (MailContract.PeekSeconds - (now - _peekAt)) / 0.16f)) : 0f;
            _clockCover.color = new Color(Violet.r, Violet.g, Violet.b, 1f - k);
            _clockQ.color = new Color(InkEdge.r, InkEdge.g, InkEdge.b, 1f - k);
            _peekGroup.gameObject.SetActive(k > 0f);
            if (k <= 0f) return;
            _peekGroup.alpha = k;
            float f = _day.Fraction;
            _peekFill.fillAmount = Mathf.Max(0.01f, f);
            float sx = -136f + 272f * f;
            SetChild(_peekStar.rectTransform, sx, -1f, 16f, 16f);
            SetChild(_peekStarGlow.rectTransform, sx, -1f, 32f, 32f);
        }

        /// <summary>El momento del faro (docs §11): la luz va DETRÁS de la estación (un baño de luz tibia que nace del faro y un haz que barre el cielo: los buzones y botones siempre se leen); delante, el destello de la
        /// lámpara y la nave del correo, que entra siguiendo la luz, deja caer un saco dorado en la cinta y se va. Con «quitar animaciones»: la luz fija hacia arriba y la nave quieta sobre la cinta.</summary>
        private void AnimateShow(float now)
        {
            float t = now - _showAt;
            bool on = _showAt > -50f && t >= 0f && t < MailMotion.ShowSeconds;
            foreach (var i in new[] { _wash, _flareA, _flareB, _ship, _shipSack, _sackGlow }) i.gameObject.SetActive(on);
            _halo.gameObject.SetActive(on);
            _beam.gameObject.SetActive(on);
            if (!on) return;
            bool deco = Motion.Decorative;
            float env = MailMotion.ShowEnvelope(t);
            // detrás
            _wash.color = new Color(255f / 255f, 226f / 255f, 150f / 255f, 0.6f * env);
            float ang = -MailMotion.BeamAngle(t, !deco);                       // el boceto usa y hacia abajo; aquí hacia arriba
            _halo.Set(ang, 0.34f, 780f * _s, new Color(1f, 236f / 255f, 170f / 255f, 1f), 0.35f * env, 0.14f * env);
            _beam.Set(ang, 0.15f, 780f * _s, new Color(1f, 244f / 255f, 200f / 255f, 1f), 0.85f * env, 0.25f * env);
            // delante: el destello de la lámpara
            _flareA.color = new Color(1f, 236f / 255f, 170f / 255f, env * 0.9f);
            _flareB.color = new Color(1f, 1f, 235f / 255f, env * 0.9f);
            // la nave del correo
            float baseY = _lay.BeltY - 130f, sx, sy;
            MailMotion.ShipAt(t, !deco, baseY, out sx, out sy);
            SetRect(_ship.rectTransform, sx, sy, MailSprites.ShipBoxW, MailSprites.ShipBoxH);
            _ship.rectTransform.localRotation = Quaternion.Euler(0f, 0f, deco ? -Mathf.Sin(t * 3f) * 0.05f * Mathf.Rad2Deg : 0f);
            float fl = deco ? 0.8f + 0.3f * Mathf.Sin(t * 40f) : 1f;
            _flame.rectTransform.sizeDelta = new Vector2(MailSprites.FlameBoxW * _s * fl, MailSprites.FlameBoxH * _s);
            _flame.rectTransform.anchoredPosition = new Vector2(-36f * _s, 0f);
            _flameGlow.rectTransform.sizeDelta = new Vector2(32f * _s, 32f * _s);
            _flameGlow.rectTransform.anchoredPosition = new Vector2(-50f * _s, 0f);
            _flameGlow.gameObject.SetActive(deco);
            // el saco dorado que cae de la nave sobre la cinta
            float drop = MailMotion.SackDrop(t);
            bool sack = t > 1.8f && drop < 1f;
            _shipSack.gameObject.SetActive(sack);
            _sackGlow.gameObject.SetActive(sack && deco);
            if (sack)
            {
                float y0 = _lay.BeltY - 78f, y = y0 + (_lay.BeltY - 30f - y0) * drop * drop;
                SetRect(_shipSack.rectTransform, MailLayout.W / 2f, y, MailSprites.SackBox, MailSprites.SackBox);
                SetRect(_sackGlow.rectTransform, MailLayout.W / 2f, y, 60f, 60f);
            }
            else if (t > 1.8f && drop >= 1f && !_sackDone)
            {
                _sackDone = true;
                Emit(new Vector2(MailLayout.W / 2f, _lay.BeltY - 30f), 30, 190f, 0.9f, Gold, 20f);
                PlayClip(MailSounds.Tier(1), 0.5f);
                AddFloat(MailLayout.W / 2f, _lay.BeltY - 96f, MailContract.FloatShipArrived, Gold, true);
            }
        }

        private void AnimateBanners(float now)
        {
            float rt = now - _rushAt;
            bool rush = rt >= 0f && rt < 1.7f;
            _rushGroup.gameObject.SetActive(rush);
            if (rush) _rushGroup.alpha = Motion.Decorative ? Mathf.Clamp01(Mathf.Min(rt / 0.18f, (1.7f - rt) / 0.3f)) : 1f;
            float dt = now - _radioAt;
            bool radio = dt >= 0f && dt < MailContract.RadioSeconds;
            _radioGroup.gameObject.SetActive(radio);
            if (radio) _radioGroup.alpha = Motion.Decorative ? Mathf.Clamp01(Mathf.Min(dt / 0.2f, (MailContract.RadioSeconds - dt) / 0.3f)) : 1f;
        }

        // ------------------------------------------------------------------ etiquetas que flotan y partículas

        /// <summary>Un aviso que flota ENCIMA de un botón (caja fuerte o faro): su borde de abajo queda 2 dp sobre el botón y no sube, así cabe en el hueco de 30 dp entre los buzones y los botones y nunca tapa un botón (docs §12).</summary>
        private void AddFloatAbove(MailLayout.Rect2 button, string text, Color color, bool big = false) => AddFloat(button.Cx, button.Y - MailLayout.AboveGap, text, color, big, true);

        private void AddFloat(float x, float y, string text, Color color, bool big = false, bool above = false)
        {
            FloatView f = null;
            float oldest = float.MaxValue;
            foreach (var c in _floats) if (c.At < oldest) { oldest = c.At; f = c; }
            if (f == null) return;
            f.At = Now;
            f.Dur = big ? 1.4f : 1.1f;
            f.Color = color;
            // el texto más grande que quepa (de 17 o 15 a 14 dp); si aun así no cabe en una línea, se parte en dos
            float maxW = MailLayout.W - 62f;
            float dp = big ? 17f : 15f;
            f.Text.text = text;
            f.Text.horizontalOverflow = HorizontalWrapMode.Overflow;
            f.Text.fontSize = Mathf.RoundToInt(dp * _s);
            while (f.Text.preferredWidth / _s > maxW && dp > 14f) { dp -= 1f; f.Text.fontSize = Mathf.RoundToInt(dp * _s); }
            float w = f.Text.preferredWidth / _s, h = above ? MailLayout.AboveH : 30f;
            if (w > maxW)
            {
                f.Text.horizontalOverflow = HorizontalWrapMode.Wrap;
                w = maxW;
                h = 50f;
            }
            float bw = w + 22f;
            x = Mathf.Clamp(x, bw / 2f + 8f, MailLayout.W - bw / 2f - 8f);
            f.Pos = new Vector2(x, above ? y - h / 2f : y);          // «above»: y es el borde de abajo del aviso
            f.Rise = above ? 0f : 18f;
            f.Bg.rectTransform.sizeDelta = new Vector2(bw * _s, h * _s);
            f.Text.rectTransform.sizeDelta = new Vector2(bw * _s, h * _s);
            SetRadius(f.Bg, Mathf.Min(h, 30f) * 0.5f * _s);
            f.Root.gameObject.SetActive(true);
        }

        private void AnimateFloats(float now)
        {
            foreach (var f in _floats)
            {
                if (!f.Root.gameObject.activeSelf) continue;
                float k = (now - f.At) / f.Dur;
                if (k >= 1f || k < 0f) { f.Root.gameObject.SetActive(false); continue; }
                float a = Mathf.Min(1f, Mathf.Min(k * 6f, (1f - k) * 3f));
                float y = f.Pos.y - (Motion.Decorative ? f.Rise * MailMotion.EaseOut(k) : 0f);
                f.Root.anchoredPosition = P(f.Pos.x, y);
                var bg = f.Bg.color; bg.a = 0.92f * a; f.Bg.color = bg;
                f.Text.color = new Color(f.Color.r, f.Color.g, f.Color.b, a);
            }
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

        /// <summary>Un destello que sube despacio y empieza <paramref name="delay"/> s después.</summary>
        private void EmitDelayed(Vector2 logicalAt, Color color, float delay, float life)
        {
            var p = _sparks.Find(x => !x.Alive);
            if (p == null) return;
            p.Alive = true;
            p.Pos = logicalAt;
            p.Vel = new Vector2(0f, -6f);
            p.Age = -delay;
            p.Life = life;
            p.Size = 0.8f + (float)_rng.NextDouble() * 1.6f;
            p.Color = color;
            p.Img.gameObject.SetActive(false);
        }

        private void AnimateSparks(float dt)
        {
            foreach (var p in _sparks)
            {
                if (!p.Alive) continue;
                p.Age += dt;
                if (p.Age < 0f) continue;
                if (p.Age >= p.Life) { p.Alive = false; p.Img.gameObject.SetActive(false); continue; }
                p.Img.gameObject.SetActive(true);
                p.Vel.y += 60f * dt;
                p.Pos += p.Vel * dt;
                float k = p.Age / p.Life;
                p.Img.rectTransform.anchoredPosition = P(p.Pos);
                p.Img.rectTransform.sizeDelta = Vector2.one * (p.Size * 2f * (1f - k * 0.5f) * _s);
                p.Img.color = NeuroStyle.WithAlpha(p.Color, 0.9f * (1f - k));
            }
        }

        // ------------------------------------------------------------------ la hoja de la mañana

        private static void LineOf(MailTodo t, out string a, out string b, out bool beacon, out MailCue cue)
        {
            cue = t.Cue;
            beacon = t.IsTime;
            a = t.IsTime ? MailContract.TimeLineA(t.Moment) : MailContract.CueLineA(t.Cue);
            b = t.IsTime ? MailContract.TimeLineB : MailContract.CueLineB;
        }

        private void BindBrief()
        {
            _briefItems.Clear();
            foreach (var t in _day.Todos) if (!(t.Routine && _day.DayNo > 1)) _briefItems.Add(t);
            _briefRoutineCard = _day.DayNo > 1 && _day.Todos.Any(t => t.Routine);
            _briefTag.text = MailContract.DayTitle(_day.DayNo);
            FillBrief();
        }

        /// <summary>Llena las filas de la hoja con <see cref="_briefItems"/> (el tutorial muestra una hoja con un solo encargo).</summary>
        private void FillBrief()
        {
            for (int i = 0; i < _briefRows.Length; i++)
            {
                var r = _briefRows[i];
                bool on = i < _briefItems.Count;
                r.Root.gameObject.SetActive(on);
                if (!on) continue;
                var t = _briefItems[i];
                LineOf(t, out var a, out var b, out var beacon, out var cue);
                r.A.text = a;
                r.B.text = b;
                if (beacon) { r.Icon.sprite = MailSprites.Tower(); r.Check.sprite = MailSprites.Lamp(true); r.Check.gameObject.SetActive(true); }
                else { r.Icon.sprite = MailSprites.Letter(cue == MailCue.Gold ? 0 : 1, cue); r.Check.gameObject.SetActive(false); }
                r.Pill.gameObject.SetActive(t.Routine);
            }
            LayoutBrief();
        }

        private void LayoutBrief()
        {
            if (_briefItems.Count == 0 && !_briefRoutineCard) return;
            float h = _logicalH, cx = MailLayout.W / 2f;
            SetRect(_briefTag.rectTransform, cx, 60f, 240f, 22f);
            SetRect(_briefTitle.rectTransform, cx, 92f, 320f, 40f);
            int n = _briefItems.Count;
            float rowsTop = 124f, bottom = h - 170f;
            float items = n + (_briefRoutineCard ? 1f : 0f);
            float per = Mathf.Clamp((bottom - rowsTop) / Mathf.Max(1f, items), 56f, 88f);
            float bh = per - 12f;
            for (int i = 0; i < n; i++)
            {
                var r = _briefRows[i];
                float y = rowsTop + per * i + per / 2f;
                SetRect(r.Root, cx, y, MailLayout.W - 36f, bh);
                SetChild(r.CardRim.rectTransform, 0f, 0f, MailLayout.W - 33f, bh + 3f);
                SetChild(r.Card.rectTransform, 0f, 0f, MailLayout.W - 36f, bh);
                bool beacon = _briefItems[i].IsTime;
                if (beacon)
                {
                    SetChild(r.Icon.rectTransform, -124f, 4f, MailSprites.TowerBoxW * 0.6f, MailSprites.TowerBoxH * 0.6f);
                    SetChild(r.Check.rectTransform, -124f, 4f - 12f, MailSprites.LampBox * 0.6f, MailSprites.LampBox * 0.6f);
                }
                else SetChild(r.Icon.rectTransform, -124f, 4f, MailSprites.LetterBoxW * 0.42f, MailSprites.LetterBoxH * 0.42f);
                SetChild(r.A.rectTransform, -84f + 130f, -6f, 260f, 20f);
                SetChild(r.B.rectTransform, -84f + 130f, 14f, 260f, 22f);
                if (_briefItems[i].Routine)
                {
                    float tw = r.PillText.preferredWidth / _s + 16f;
                    SetChild(r.Pill.rectTransform, (MailLayout.W - 36f) / 2f - 12f - tw / 2f, -bh / 2f + 1f, tw, 20f);
                    SetChild(r.PillText.rectTransform, 0f, 0f, tw + 20f, 20f);
                }
            }
            float y2 = rowsTop + per * n;
            _routineCard.gameObject.SetActive(_briefRoutineCard);
            _routineCardRim.gameObject.SetActive(_briefRoutineCard);
            _routineT1.gameObject.SetActive(_briefRoutineCard);
            _routineT2.gameObject.SetActive(_briefRoutineCard);
            if (_briefRoutineCard)
            {
                float ch = Mathf.Min(62f, per - 8f);
                SetRect(_routineCardRim.rectTransform, cx, y2 + per / 2f - 2f, MailLayout.W - 33f, ch + 3f);
                SetRect(_routineCard.rectTransform, cx, y2 + per / 2f - 2f, MailLayout.W - 36f, ch);
                SetRect(_routineT1.rectTransform, cx, y2 + per / 2f - 2f - 11f, 300f, 22f);
                SetRect(_routineT2.rectTransform, cx, y2 + per / 2f - 2f + 12f, 300f, 22f);
                y2 += per;
            }
            SetRect(_briefNote1.rectTransform, cx, y2 + 14f, 320f, 22f);
            SetRect(_briefNote2.rectTransform, cx, y2 + 38f, 340f, 22f);
            SetRect(_briefBtn.rectTransform, cx, h - 72f, 180f, 58f);
            SetRect(_briefBtnRim.rectTransform, cx, h - 72f, 183f, 61f);
            SetChild(_briefBtnLabel.rectTransform, 0f, 0f, 180f, 40f);                 // hijo del botón
        }

        // ------------------------------------------------------------------ el resumen del día

        private void BindRecap()
        {
            var r = _recap;
            _recapTag.text = MailContract.RecapTag(_day.DayNo);
            _recapTitle.text = MailContract.RecapTitle(r.Stat.PmOk, r.Stat.PmAll);
            for (int i = 0; i < _recapRows.Length; i++)
            {
                var row = _recapRows[i];
                bool on = i < r.Items.Count;
                row.Root.gameObject.SetActive(on);
                if (!on) continue;
                var it = r.Items[i];
                row.A.text = it.Text;
                row.B.text = it.Detail;
                row.Check.sprite = it.Ok ? MailSprites.CheckOk() : MailSprites.CheckNo();
                row.Check.gameObject.SetActive(true);
                row.Icon.gameObject.SetActive(false);
                row.Pill.gameObject.SetActive(false);
            }
            _recapLine1.text = MailContract.RecapCards(r.Stat.Right, r.Stat.Sorted + r.Stat.Late, r.Stat.BestCombo);
            bool anyTime = _day.Todos.Any(t => t.IsTime);
            _recapLine2.gameObject.SetActive(anyTime);
            if (anyTime) _recapLine2.text = MailContract.RecapClock(r.Stat.Peeks, r.Stat.GoodPeeks);
            bool record = !_guided && _run.NewRecordToday;
            _recapRecord.gameObject.SetActive(record);
            _recapRecord.text = MailContract.RecapRecord;
            bool up = !_guided && _run.Up;
            _recapUp.text = up ? MailContract.RecapUp : MailContract.RecapSame;
            _recapUp.color = up ? Mint : Lavender;
            _recapBtnLabel.text = MailContract.RecapNext(_day.DayNo);
            LayoutRecap();
        }

        private void LayoutRecap()
        {
            if (_recap == null) return;
            float h = _logicalH, cx = MailLayout.W / 2f;
            SetRect(_recapTag.rectTransform, cx, 60f, 240f, 22f);
            SetRect(_recapTitle.rectTransform, cx, 92f, 330f, 36f);
            int n = _recap.Items.Count;
            float rowsTop = 122f, bottom = h - 200f;
            float per = Mathf.Clamp((bottom - rowsTop) / Mathf.Max(1, n), 50f, 66f);
            for (int i = 0; i < n; i++)
            {
                var row = _recapRows[i];
                float y = rowsTop + per * i + per / 2f, ch = per - 8f;
                SetRect(row.Root, cx, y, MailLayout.W - 36f, ch);
                SetChild(row.CardRim.rectTransform, 0f, 0f, MailLayout.W - 33f, ch + 3f);
                SetChild(row.Card.rectTransform, 0f, 0f, MailLayout.W - 36f, ch);
                SetChild(row.Check.rectTransform, -134f, 0f, MailSprites.CheckBox, MailSprites.CheckBox);
                SetChild(row.A.rectTransform, -110f + 150f, -ch * 0.16f, 290f, 20f);
                SetChild(row.B.rectTransform, -110f + 150f, ch * 0.22f, 290f, 20f);
            }
            float y2 = rowsTop + per * n + 14f;
            SetRect(_recapLine1.rectTransform, cx, y2, 340f, 22f);
            SetRect(_recapLine2.rectTransform, cx, y2 + 24f, 340f, 22f);
            SetRect(_recapRecord.rectTransform, cx, y2 + 50f, 340f, 22f);
            SetRect(_recapUp.rectTransform, cx, h - 132f, 320f, 22f);
            SetRect(_recapBtn.rectTransform, cx, h - 72f, 180f, 58f);
            SetRect(_recapBtnRim.rectTransform, cx, h - 72f, 183f, 61f);
            SetChild(_recapBtnLabel.rectTransform, 0f, 0f, 180f, 40f);
        }

        // ------------------------------------------------------------------ la pantalla final (docs §7)

        private void BindEnd()
        {
            var t = _run.Tally;
            int all = t.MeasureAll, ok = t.MeasureOk;
            _endPct.text = t.Percent.HasValue ? t.Percent.Value + " %" : "—";
            _endSub.text = ok + " de " + all + " encargos cumplidos";
            string[] labels = { "Por evento (cartas señal)", "Por hora (faro)", "Cancelados que no hiciste", "Miradas al reloj cerca de la hora", "Cartas bien puestas", "Etapa más alta" };
            string[] values =
            {
                t.Events > 0 ? t.EventsOk + " de " + t.Events : "—",
                t.Times > 0 ? t.TimesOk + " de " + t.Times : "—",
                t.Cancels > 0 ? (t.Cancels - t.Commissions) + " de " + t.Cancels : "—",
                t.Peeks > 0 ? t.GoodPeeks + " de " + t.Peeks : "—",
                t.Right.ToString(),
                MailContract.GroupOf(_run.MaxLevelReached) + " de " + MailContract.GroupCount,
            };
            for (int i = 0; i < EndRows; i++) { _endLabels[i].text = labels[i]; _endValues[i].text = values[i]; }
            _endTip.text = MailContract.Tip(_run.DayNo + t.Events);
            LayoutEnd();
        }

        private void LayoutEnd()
        {
            float cx = MailLayout.W / 2f;
            SetRect(_endTitle.rectTransform, cx, 70f, 330f, 40f);
            SetRect(_endCardRim.rectTransform, cx, 159f, MailLayout.W - 37f, 121f);
            SetRect(_endCard.rectTransform, cx, 159f, MailLayout.W - 40f, 118f);
            SetRect(_endHead.rectTransform, cx, 124f, 320f, 22f);
            SetRect(_endPct.rectTransform, cx, 164f, 320f, 56f);
            SetRect(_endSub.rectTransform, cx, 198f, 320f, 22f);
            float y = 250f;
            for (int i = 0; i < EndRows; i++)
            {
                SetRect(_endLabels[i].rectTransform, 26f + 150f, y, 300f, 24f);
                SetRect(_endValues[i].rectTransform, MailLayout.W - 26f - 60f, y, 120f, 26f);
                y += 36f;
            }
            SetRect(_endTip.rectTransform, cx, y + 4f, 340f, 22f);
            SetRect(_endNote.rectTransform, cx, y + 28f, 340f, 22f);
        }

        private void LayoutOverlays()
        {
            LayoutBrief();
            LayoutRecap();
            if (_run != null && _phase == Phase.Done) LayoutEnd();
        }

        // ------------------------------------------------------------------ «NUEVO»: la ilustración de cada regla

        private int _artCursor;

        private Image ArtImage(Sprite sprite, float cx, float cy, float w, float h, Color? tint = null)
        {
            var img = _introImgs[_artCursor++];
            img.sprite = sprite;
            img.color = tint ?? Color.white;
            SetRect(img.rectTransform, cx, cy, w, h);
            img.gameObject.SetActive(true);
            return img;
        }

        private void ArtLabel(int i, float cx, float cy, string text, float dp = 14f, Color? color = null, bool bold = false)
        {
            var t = _introLabels[i];
            t.text = text;
            t.color = color ?? Lavender;
            t.font = bold ? UiFonts.Bold : UiFonts.Regular;
            t.fontSize = Mathf.RoundToInt(dp * _s);
            SetRect(t.rectTransform, cx, cy, 200f, 26f);
            t.gameObject.SetActive(true);
        }

        private void ArtBeacon(float cx, float cy)
        {
            ArtImage(MailSprites.Tower(), cx, cy, MailSprites.TowerBoxW * 0.7f, MailSprites.TowerBoxH * 0.7f);
            ArtImage(MailSprites.Lamp(true), cx, cy - 14f, MailSprites.LampBox * 0.7f, MailSprites.LampBox * 0.7f);
        }

        private void LayoutIntro(string[] lines)
        {
            float cw = MailLayout.W - 36f, cx = MailLayout.W / 2f;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0) sb.Append(lines[i]);
                else sb.Append(i == 1 ? "\n<color=#D6D1F2>" : "\n").Append(lines[i]);
            }
            if (lines.Length > 1) sb.Append("</color>");
            _introBody.text = sb.ToString();
            _introBody.rectTransform.sizeDelta = new Vector2((cw - 28f) * _s, 400f * _s);
            float bodyH = _introBody.preferredHeight / _s;
            float ch = 150f + bodyH + 44f;
            float top = Mathf.Min(_logicalH * 0.5f - ch / 2f, _logicalH - ch - 12f);
            top = Mathf.Max(top, 12f);
            SetRect(_introRim.rectTransform, cx, top + ch / 2f, cw + 4f, ch + 4f);
            SetRect(_introCard.rectTransform, cx, top + ch / 2f, cw, ch);
            SetRect(_introTag.rectTransform, cx, top + 26f, 220f, 24f);
            SetRect(_introBody.rectTransform, cx, top + 150f + bodyH / 2f, cw - 28f, bodyH + 4f);
            SetRect(_introTap.rectTransform, cx, top + ch - 24f, 200f, 24f);
            LayoutIntroArt(cx, top + 88f);
        }

        private void LayoutIntroArt(float cx, float cy)
        {
            _artCursor = 0;
            foreach (var i in _introImgs) i.gameObject.SetActive(false);
            foreach (var t in _introLabels) t.gameObject.SetActive(false);
            _introBox.gameObject.SetActive(false);
            float lw = MailSprites.LetterBoxW, lh = MailSprites.LetterBoxH;
            switch (_introKind)
            {
                case MailIntro.Estacion:
                    ArtImage(MailSprites.Letter(0, MailCue.None), cx - 62f, cy, lw * 0.62f, lh * 0.62f);
                    ArtImage(MailSprites.Letter(1, MailCue.Gold), cx + 62f, cy, lw * 0.62f, lh * 0.62f);
                    ArtLabel(0, cx - 62f, cy + 40f, "a su buzón");
                    ArtLabel(1, cx + 62f, cy + 40f, "a la caja fuerte");
                    break;
                case MailIntro.Hora:
                    ArtImage(DiscGlyph(), cx - 50f, cy, 52f, 52f, Violet);
                    ArtLabel(0, cx - 50f, cy + 1f, "?", 24f, InkEdge, true);
                    ArtBeacon(cx + 50f, cy);
                    break;
                case MailIntro.Rutina:
                    ArtLabel(0, cx, cy - 18f, MailContract.RoutineTag, 15f, Violet, true);
                    ArtBeacon(cx, cy + 16f);
                    break;
                case MailIntro.Cancela:
                    _introBox.gameObject.SetActive(true);
                    SetRect(_introBox.rectTransform, cx, cy, MailLayout.W - 100f, 56f);
                    ArtLabel(0, cx, cy - 12f, MailContract.RadioTag, 14f, Violet, true);
                    ArtLabel(1, cx, cy + 10f, "hoy NO hace falta el faro", 14f, TextColor, true);
                    break;
                case MailIntro.Lazo:
                    ArtImage(MailSprites.Letter(2, MailCue.Lazo), cx, cy, lw * 0.7f, lh * 0.7f);
                    break;
            }
        }

        private static Sprite DiscGlyph() => DiscSprite.Get();

        // ------------------------------------------------------------------ verificación de que lo dibujado se ve

        /// <summary>Para las pruebas: arma un día de cada etapa y devuelve lo que NO se vería (pieza apagada, sin imagen o sin opacidad): la cinta, el marco, el reloj, la caja fuerte con su dial, el faro con su torre y su lámpara, la
        /// nave y el saco, los buzones de cada etapa con su planeta y las cartas de cada tipo; y las letras de menos de 14 dp. Una pieza horneada sin sprite se dibuja como un cuadrado blanco (lo que pasó con las fichas de Punta el 3-oct).</summary>
        public List<string> AuditVisibility()
        {
            var problems = new List<string>();
            _s = 3f; _playW = 1080f; _playH = 1920f; _logicalH = 640f;
            _lay = MailLayout.Compute(_logicalH);
            if (_rng == null) _rng = new System.Random(1);
            var bake = MailSprites.Prewarm();
            while (bake.MoveNext()) { }
            AssignSprites();
            void Check(string what, Graphic g, bool needsSprite, bool needsAlpha = true)
            {
                if (g == null) { problems.Add(what + ": no existe"); return; }
                if (!g.gameObject.activeInHierarchy) problems.Add(what + ": apagada");
                else if (needsAlpha && g.color.a <= 0.01f) problems.Add(what + ": transparente");
                if (needsSprite && g is Image im && im.sprite == null) problems.Add(what + ": sin imagen");
            }
            _stageLayer.gameObject.SetActive(true);
            foreach (var l in new[] { _showBackLayer, _flyLayer, _showFrontLayer, _bannerLayer, _peekLayer, _floatLayer, _sparkLayer }) l.gameObject.SetActive(true);
            foreach (var g in new GameObject[] { _ship.gameObject, _shipSack.gameObject, _rushGroup.gameObject, _peekGroup.gameObject, _radioGroup.gameObject }) g.SetActive(true);        // se encienden solo cuando toca; aquí se miran todas
            Check("cinta", _beltBand, false); Check("riel de arriba", _railTop, false); Check("riel de abajo", _railBottom, false);
            Check("marco punteado", _frame, true);
            Check("reloj.fondo", _clockFill, false); Check("reloj.borde", _clockRim, false); Check("reloj.tapa", _clockCover, false);
            Check("caja fuerte.cuerpo", _safeBody, false); Check("caja fuerte.puerta", _safeDoor, false); Check("caja fuerte.dial", _dial, true); Check("caja fuerte.manija", _handle, false);
            Check("faro.cuerpo", _bBody, false); Check("faro.torre", _tower, true); Check("faro.lámpara", _lamp, true);
            Check("nave", _ship, true, false); Check("llama", _flame, true, false); Check("saco de la nave", _shipSack, true, false); Check("saco del aviso", _rushSack, true);
            Check("barra del día", _peekFill, true);
            foreach (var t in new[] { _hdrCards, _hint, _clockLabel, _safeLabel, _beaconLabel }) if (t == null || !t.gameObject.activeInHierarchy) problems.Add("texto fijo apagado");
            if (_boxes.Length != 4) problems.Add("faltan buzones");
            MailMoment? routine = null;
            for (int level = 1; level <= MailContract.MaxLevel; level++)
            {
                var day = MailDay.Create(level, 1, ref routine, _rng);
                _day = day;
                _boxCount = day.Stage.Boxes;
                LayoutBoxes();
                for (int i = 0; i < day.Stage.Boxes; i++)
                {
                    Check("etapa " + level + " buzón " + i + ".cuerpo", _boxes[i].Body, false);
                    Check("etapa " + level + " buzón " + i + ".planeta", _boxes[i].Planet, true);
                    if (_boxes[i].Label.text.Length == 0) problems.Add("etapa " + level + " buzón " + i + ": sin nombre");
                }
                for (int i = day.Stage.Boxes; i < 4; i++) if (_boxes[i].Root.gameObject.activeSelf) problems.Add("etapa " + level + ": sobra el buzón " + i);
            }
            ClearBelt();
            foreach (MailCue cue in new[] { MailCue.None, MailCue.Gold, MailCue.Lazo })
                for (int pl = 0; pl < MailContract.PlanetCount; pl++)
                {
                    var v = Acquire(new MailLetter { Planet = pl, Cue = cue }, 150f);
                    Check("carta " + pl + "/" + cue, v.Img, true);
                }
            ClearBelt();
            foreach (var kv in _fonts) if (kv.Value < 14f) problems.Add("un texto de " + kv.Value + " dp (el mínimo es 14)");
            _day = null;
            return problems;
        }

        /// <summary>Para las pruebas: con cartas en la cinta, cada una se dibuja DESPUÉS (encima) de la banda de la cinta, los rieles, los rodillos y el marco, y la de adelante después de las de atrás. Devuelve lo que no cumple
        /// (el error del 8-oct: las cartas eran hijas de la capa de la estación y el orden de hermanos las mandaba debajo de la banda).</summary>
        public List<string> AuditLayering()
        {
            var problems = new List<string>();
            _s = 3f; _playW = 1080f; _playH = 1920f; _logicalH = 640f;
            _lay = MailLayout.Compute(_logicalH);
            if (_rng == null) _rng = new System.Random(1);
            var bake = MailSprites.Prewarm();
            while (bake.MoveNext()) { }
            AssignSprites();
            MailMoment? routine = null;
            _day = MailDay.Create(5, 1, ref routine, _rng);
            _day.Belt.Clear();
            for (int i = 0; i < 5; i++) _day.Belt.Add(new MailLetter { Planet = i % 4, Cue = i == 1 ? MailCue.Gold : MailCue.None });
            _stageLayer.gameObject.SetActive(true);
            ClearBelt();
            AnimateBelt(0f, 0.016f);
            var under = new List<Transform> { _beltBand.transform, _railTop.transform, _railBottom.transform, _frameGlow.transform, _frame.transform };
            foreach (var r in _rollers) if (r != null) under.Add(r.transform);
            LetterView front = null, behind = null;
            for (int i = 0; i < _day.Belt.Count; i++)
            {
                if (!_lvOf.TryGetValue(_day.Belt[i], out var v)) { problems.Add("la carta " + i + " de la cinta no tiene vista"); continue; }
                if (!v.Root.gameObject.activeInHierarchy) problems.Add("la carta " + i + " de la cinta está apagada");
                foreach (var u in under)
                    if (!MailHierarchy.DrawnAfter(v.Root, u)) problems.Add("la carta " + i + " se dibuja DEBAJO de «" + u.name + "»");
                if (i == 0) front = v;
                if (i == 1) behind = v;
            }
            if (front != null && behind != null && !MailHierarchy.DrawnAfter(front.Root, behind.Root)) problems.Add("la carta de adelante se dibuja detrás de la que le sigue");
            ClearBelt();
            _day = null;
            return problems;
        }

        /// <summary>Para las pruebas: con la estación, la hoja, el resumen y la pantalla final armados, todo texto que es HIJO de un botón o de una píldora (un <see cref="Image"/>) tiene su centro dentro del rect del padre
        /// (el error del 8-oct: se posicionaban con <c>SetRect</c>, que usa coordenadas de la capa, y quedaban fuera del botón). Devuelve lo que no cumple.</summary>
        public List<string> AuditChildPlacement()
        {
            var problems = new List<string>();
            _s = 3f; _playW = 1080f; _playH = 1920f; _logicalH = 640f;
            _lay = MailLayout.Compute(_logicalH);
            if (_rng == null) _rng = new System.Random(1);
            MailMoment? routine = null;
            _run = new MailRun(5, 0, _rng);
            _day = _run.NextDay();
            LayoutStage();
            BindBrief();
            _recap = _day.Summarize();
            BindRecap();
            _run.Complete(_day, _recap);
            BindEnd();
            // las píldoras de la racha y de las cartas que esperan se muestran con cartas y racha
            _day.Combo = 12;
            for (int i = 0; i < 6; i++) _day.Belt.Add(new MailLetter { Planet = i % 4 });
            AnimateBelt(0f, 0.016f);
            foreach (var t in _play.GetComponentsInChildren<Text>(true))
            {
                var parent = t.transform.parent as RectTransform;
                if (parent == null || parent.GetComponent<Image>() == null) continue;
                if (string.IsNullOrEmpty(t.text)) continue;
                var half = parent.sizeDelta / 2f;
                if (half.x <= 0f || half.y <= 0f) continue;                  // un padre que aún no tiene tamaño (se mide al mostrarse)
                var c = t.rectTransform.anchoredPosition;
                if (Mathf.Abs(c.x) > half.x + 1f || Mathf.Abs(c.y) > half.y + 1f)
                    problems.Add("«" + t.text + "» (" + t.name + ") queda fuera de «" + parent.name + "»: su centro está a (" + Mathf.RoundToInt(c.x) + ", " + Mathf.RoundToInt(c.y) + ") y el padre mide " + Mathf.RoundToInt(parent.sizeDelta.x) + "×" + Mathf.RoundToInt(parent.sizeDelta.y));
            }
            ClearBelt();
            _day = null; _run = null; _recap = null;
            return problems;
        }

        // ------------------------------------------------------------------ las pantallas encima de la estación: aparecen con un fundido

        private void AnimateOverlays(float now)
        {
            float k = Motion.Decorative ? MailMotion.EaseOut((now - _phaseAt) / 0.35f) : 1f;
            if (_phase == Phase.Brief) SetGroupAlpha(_briefLayer, k);
            else if (_phase == Phase.Recap) SetGroupAlpha(_recapLayer, Motion.Decorative ? MailMotion.EaseOut((now - _phaseAt) / 0.4f) : 1f);
            else if (_phase == Phase.Intro) SetGroupAlpha(_introLayer, k);
            else if (_phase == Phase.Done) SetGroupAlpha(_endLayer, Motion.Decorative ? MailMotion.EaseOut((now - _phaseAt) / 0.5f) : 1f);
        }

        private static void SetGroupAlpha(RectTransform layer, float a)
        {
            var g = layer.GetComponent<CanvasGroup>();
            if (g == null) g = layer.gameObject.AddComponent<CanvasGroup>();
            g.alpha = a;
            g.blocksRaycasts = false;
        }
    }
}
