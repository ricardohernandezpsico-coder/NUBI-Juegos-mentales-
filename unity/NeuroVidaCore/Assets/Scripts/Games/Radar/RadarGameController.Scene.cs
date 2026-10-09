using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Radar
{
    public sealed partial class RadarGameController
    {
        private enum BoardMode { Dim, Live, Reveal }

        /// <summary>Una cápsula acertada que sube a la nave por el rayo tractor (750 ms, escalonadas 160 ms).</summary>
        private sealed class Lift
        {
            public int Index, Slot;
            public CapsuleType Type;
            public float FromX, FromY, Tilt, At;
            public bool Landed;
        }

        private readonly List<CapsuleType> _seats = new List<CapsuleType>();
        private readonly List<Lift> _lifts = new List<Lift>();
        private readonly List<int> _drifting = new List<int>();
        private readonly HashSet<CapsuleType> _hitSet = new HashSet<CapsuleType>(), _wrongSet = new HashSet<CapsuleType>(), _missedSet = new HashSet<CapsuleType>();
        private readonly float[] _pressAt = { -10f, -10f, -10f, -10f, -10f, -10f };
        private BoardMode _boardMode = BoardMode.Dim;
        private float _flashAt = -10f, _flashMs = 100f, _shipPopAt = -10f, _revealAt = -10f, _endAt = -10f, _goPressAt = -10f, _beamAngle;
        private int _shownRescued, _lastTurn = -1, _maskCount;
        private bool _beamOn = true;
        private bool _confettiOn;
        private readonly Vector2[] _confettiPos = new Vector2[ConfettiPool], _confettiVel = new Vector2[ConfettiPool];
        private readonly float[] _confettiLife = new float[ConfettiPool];
        private float _confettiAt;

        // ------------------------------------------------------------------ reposo y ronda

        /// <summary>El estado de reposo: sin objetos, sin rayos, sin estática, sin pantalla final, tablero apagado y la nave vacía.</summary>
        private void ResetViews()
        {
            HideRoundObjects();
            _lifts.Clear();
            _drifting.Clear();
            foreach (var b in _beams) b.gameObject.SetActive(false);
            foreach (var f in _flying) f.gameObject.SetActive(false);
            foreach (var sp in _sparks) sp.gameObject.SetActive(false);
            foreach (var e in _echoes) e.gameObject.SetActive(false);
            _mask.gameObject.SetActive(false);
            _flashFill.color = new Color(_flashFill.color.r, _flashFill.color.g, _flashFill.color.b, 0f);
            _flashGlow.color = new Color(_flashGlow.color.r, _flashGlow.color.g, _flashGlow.color.b, 0f);
            _flashWave.color = new Color(_flashWave.color.r, _flashWave.color.g, _flashWave.color.b, 0f);
            _endLayer.gameObject.SetActive(false);
            foreach (var c in _confetti) c.gameObject.SetActive(false);
            _confettiOn = false;
            _hudLayer.gameObject.SetActive(true);
            _boardLayer.gameObject.SetActive(true);
            _shipLayer.gameObject.SetActive(true);
            _radarLayer.gameObject.SetActive(true);
            _seats.Clear();
            _shownRescued = 0;
            _beamOn = true;
            SetBoard(BoardMode.Dim);
            SetMessage("", "");
            RefreshSeats();
            UpdateHudExtras();
        }

        /// <summary>Arma los objetos de la ronda en sus lugares (todavía escondidos: se encienden en el destello).</summary>
        private void BeginRoundObjects(RadarRound round)
        {
            _round = round;
            HideRoundObjects();
            for (int i = 0; i < round.Capsules.Length; i++)
            {
                _capImgs[i].sprite = RadarSprites.CapsuleSprite(round.Capsules[i].Type);
                _capImgs[i].color = Color.white;
                _capImgs[i].rectTransform.localScale = Vector3.one;
            }
            for (int i = 0; i < round.Rocks.Length; i++)
            {
                _rockImgs[i].color = Color.white;
                _rockImgs[i].rectTransform.localScale = Vector3.one;
            }
            PlaceRoundObjects();
        }

        private void PlaceRoundObjects()
        {
            if (_round == null) return;
            float k = _plan.Scale, cx = _plan.RadarCx, cy = _plan.RadarCy;
            for (int i = 0; i < _round.Capsules.Length; i++)
            {
                var o = _round.Capsules[i];
                _capImgs[i].rectTransform.anchoredPosition = P(cx + o.X * k, cy + o.Y * k);
                _capImgs[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, -o.Tilt * Mathf.Rad2Deg);
                _capRings[i].rectTransform.anchoredPosition = _capImgs[i].rectTransform.anchoredPosition;
            }
            for (int i = 0; i < _round.Rocks.Length; i++)
            {
                var o = _round.Rocks[i];
                _rockImgs[i].rectTransform.anchoredPosition = P(cx + o.X * k, cy + o.Y * k);
                _rockImgs[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, -o.Tilt * Mathf.Rad2Deg);
            }
        }

        private void HideRoundObjects()
        {
            foreach (var c in _capImgs) c.gameObject.SetActive(false);
            foreach (var c in _capRings) c.gameObject.SetActive(false);
            foreach (var r in _rockImgs) r.gameObject.SetActive(false);
        }

        /// <summary>Enciende las cápsulas y las rocas de la ronda (en el destello, un 6 % más grandes).</summary>
        private void ShowObjects(float scale)
        {
            if (_round == null) return;
            for (int i = 0; i < _round.Capsules.Length; i++)
            {
                _capImgs[i].gameObject.SetActive(true);
                _capImgs[i].rectTransform.localScale = Vector3.one * scale;
                _capImgs[i].color = Color.white;
            }
            for (int i = 0; i < _round.Rocks.Length; i++)
            {
                _rockImgs[i].gameObject.SetActive(true);
                _rockImgs[i].rectTransform.localScale = Vector3.one * scale;
                _rockImgs[i].color = Color.white;
            }
        }

        // ------------------------------------------------------------------ mensajes (la franja de arriba: NUNCA sobre el radar ni el tablero)

        /// <summary>Un mensaje en la franja de mensajes. <paramref name="pill"/> = un aviso (con su píldora oscura); sin ella, el texto suelto de la espera y de la pregunta.</summary>
        private void SetMessage(string a, string b, Color? colorA = null, bool pill = false)
        {
            _msgA.text = a;
            _msgA.color = colorA ?? TextColor;
            _msgB.text = b ?? "";
            _msgPill.gameObject.SetActive(pill && !string.IsNullOrEmpty(a));
        }

        // ------------------------------------------------------------------ marcador

        private void UpdateHud()
        {
            if (_guided) return;
            _hud.SetLevel(_dda.PresentedLevel);
            int round = _run.RoundsPlayed + 1;
            _hud.SetInfo(RadarContract.RoundChip(round, Endless ? 0 : RoundsTotal));
            UpdateHudExtras();
        }

        private void UpdateHudExtras()
        {
            _hudCount.text = _shownRescued.ToString();
            int streak = _run != null ? _run.Streak : 0;
            bool hot = !_guided && streak >= 2;
            _streakBg.gameObject.SetActive(hot);
            _streakText.gameObject.SetActive(hot);
            if (hot) _streakText.text = RadarContract.StreakChip(streak);
        }

        // ------------------------------------------------------------------ el tablero

        private void SetBoard(BoardMode mode)
        {
            _boardMode = mode;
            _go.Root.gameObject.SetActive(mode == BoardMode.Live);
            RefreshBoard(Now);
        }

        /// <summary>Colores y marcas del tablero según la fase: apagado (45 %), vivo con lo elegido, o la revelación (acertada: verde y ✓; elegida que no estaba: vino y ✗; estaba y no la elegiste: aro coral punteado).</summary>
        private void RefreshBoard(float now)
        {
            bool live = _boardMode != BoardMode.Dim;
            for (int i = 0; i < _buttons.Length; i++)
            {
                var v = _buttons[i];
                var type = RadarContract.BoardOrder[i];
                Color typeColor = RadarSprites.Colors[(int)type];
                bool picked = _picks.Contains(type);
                Color fill = live ? ButtonLive : ButtonDim;
                Color? ring = null;
                bool dashed = false;
                bool badgeOk = false, badgeNo = false;
                if (_boardMode == BoardMode.Reveal)
                {
                    if (_hitSet.Contains(type)) { fill = ButtonHit; ring = Lime; badgeOk = true; }
                    else if (_wrongSet.Contains(type)) { fill = ButtonWrong; ring = Coral; badgeNo = true; }
                    else if (_missedSet.Contains(type)) { ring = Coral; dashed = true; }
                }
                else if (_boardMode == BoardMode.Live && picked)
                {
                    fill = Color.Lerp(ButtonLive, typeColor, 0.32f);
                    ring = typeColor;
                }
                v.Bg.color = fill;
                v.Group.alpha = live ? 1f : 0.45f;
                v.Ring.gameObject.SetActive(ring.HasValue);
                if (ring.HasValue)
                {
                    v.Ring.sprite = RadarSprites.ButtonRing(dashed);
                    v.Ring.color = ring.Value;
                }
                v.Badge.gameObject.SetActive(badgeOk || badgeNo);
                if (badgeOk) v.Badge.sprite = AnswerMarkSprite.Check();
                else if (badgeNo) v.Badge.sprite = AnswerMarkSprite.Cross();
                // se hunde 3 dp durante 140 ms al tocarlo
                float press = now - _pressAt[i] < 0.14f && now >= _pressAt[i] ? 3f : 0f;
                var (cx, cy) = _plan.CellCenter(i);
                v.Root.anchoredPosition = P(cx, cy + press);
            }
            bool on = _picks.Count > 0;
            _go.Bg.color = on ? Gold : new Color(74f / 255f, 70f / 255f, 114f / 255f);
            _go.Label.color = on ? Ink : new Color(156f / 255f, 151f / 255f, 196f / 255f);
            float gp = now - _goPressAt < 0.14f && now >= _goPressAt ? 3f : 0f;
            _go.Root.anchoredPosition = P(_plan.GoCx, _plan.GoCy + gp);
        }

        // ------------------------------------------------------------------ un cuadro de dibujo

        private void Animate(float now, float dt)
        {
            AnimateBack(now, dt);
            if (_phase == Phase.Done)
            {
                AnimateEnd(now, dt);
                return;
            }
            AnimateRadar(now, dt);
            AnimateShip(now);
            if (_phase == Phase.Reveal) AnimateReveal(now);
            RefreshBoard(now);
        }

        private void AnimateBack(float now, float dt)
        {
            for (int i = 0; i < DustPool; i++)
            {
                if (Motion.Decorative && dt > 0f)
                {
                    _dustPos[i].y += _dustSpeed[i] * dt;
                    _dustPos[i].x += _dustSpeed[i] * 0.3f * dt;
                    if (_dustPos[i].y > _logicalH) _dustPos[i].y -= _logicalH;
                    if (_dustPos[i].x > RadarPlan.Width) _dustPos[i].x -= RadarPlan.Width;
                }
                SetRect(_dust[i].rectTransform, _dustPos[i].x, Mathf.Min(_dustPos[i].y, _logicalH), _dustSize[i], _dustSize[i]);
            }
        }

        /// <summary>El haz que gira (con estela y ecos sueltos de adorno) mientras se espera; en el destello, el relámpago: el disco se enciende, un resplandor nace del centro y una onda sale hacia el borde.</summary>
        private void AnimateRadar(float now, float dt)
        {
            bool beam = _beamOn && (_phase == Phase.Watch || _phase == Phase.Idle);
            _beamLine.gameObject.SetActive(beam);
            _sweep.gameObject.SetActive(beam && Motion.Decorative);
            _sweepHead.gameObject.SetActive(beam && Motion.Decorative);
            if (beam)
            {
                if (dt > 0f) _beamAngle = now * 2.4f;
                float a = _beamAngle, k = _plan.Scale;
                _sweep.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -a * Mathf.Rad2Deg);
                _beamLine.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f - a * Mathf.Rad2Deg);
                float hr = _plan.GlassR - 10f * k;
                SetRect(_sweepHead.rectTransform, _plan.RadarCx + Mathf.Sin(a) * hr, _plan.RadarCy - Mathf.Cos(a) * hr, 20f * k, 20f * k);
                int turn = Mathf.FloorToInt(a / (Mathf.PI * 2f));
                if (turn != _lastTurn)
                {
                    _lastTurn = turn;
                    if (_phase == Phase.Watch) PlayClip(RadarSounds.Ping(), 0.45f);              // un «ping» por vuelta
                }
                // ecos sueltos que deja el haz: adorno, nunca están donde aparecerán las cápsulas
                if (_phase == Phase.Watch && Motion.Decorative && dt > 0f && Random.value < 0.06f * dt * 60f)
                    for (int i = 0; i < EchoPool; i++)
                        if (now - _echoAt[i] > 0.9f)
                        {
                            _echoAt[i] = now;
                            _echoAngle[i] = a;
                            _echoRadius[i] = (30f + Random.value * (RadarContract.RadarRadius - 40f)) * k;
                            break;
                        }
            }
            for (int i = 0; i < EchoPool; i++)
            {
                float age = now - _echoAt[i];
                bool on = beam && Motion.Decorative && age >= 0f && age < 0.9f;
                _echoes[i].gameObject.SetActive(on);
                if (!on) continue;
                SetRect(_echoes[i].rectTransform, _plan.RadarCx + Mathf.Sin(_echoAngle[i]) * _echoRadius[i], _plan.RadarCy - Mathf.Cos(_echoAngle[i]) * _echoRadius[i], 5f * _plan.Scale, 5f * _plan.Scale);
                _echoes[i].color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f * (1f - age / 0.9f));
            }

            // el relámpago
            if (_phase == Phase.Flash)
            {
                float t = now - _flashAt, k2 = _plan.Scale;
                _flashFill.color = new Color(205f / 255f, 232f / 255f, 1f, 0.2f);
                if (Motion.Decorative)
                {
                    _flashGlow.color = new Color(225f / 255f, 245f / 255f, 1f, Mathf.Max(0f, 0.55f - t / 0.4f));
                    float wk = Mathf.Min(1f, t / Mathf.Max(0.12f, _flashMs / 1000f));
                    float wd = 2f * (20f + wk * (RadarContract.RadarRadius - 20f)) * k2;
                    SetRect(_flashWave.rectTransform, _plan.RadarCx, _plan.RadarCy, wd, wd);
                    _flashWave.color = new Color(225f / 255f, 245f / 255f, 1f, 0.6f * (1f - wk));
                }
            }
            else
            {
                _flashFill.color = new Color(_flashFill.color.r, _flashFill.color.g, _flashFill.color.b, 0f);
                _flashGlow.color = new Color(_flashGlow.color.r, _flashGlow.color.g, _flashGlow.color.b, 0f);
                _flashWave.color = new Color(_flashWave.color.r, _flashWave.color.g, _flashWave.color.b, 0f);
            }
        }

        private void AnimateShip(float now)
        {
            float k = _plan.ShipScale;
            float pop = Motion.Decorative && now - _shipPopAt >= 0f && now - _shipPopAt < 0.26f ? Mathf.Sin((now - _shipPopAt) / 0.26f * Mathf.PI) : 0f;
            _shipHull.rectTransform.localScale = new Vector3(1f + 0.04f * pop, 1f - 0.08f * pop, 1f);
            for (int i = 0; i < WindowCount; i++) _windows[i].rectTransform.localScale = new Vector3(1f + 0.04f * pop, 1f - 0.08f * pop, 1f);
            for (int i = 0; i < _flames.Length; i++)
            {
                float fl = Motion.Decorative ? 22f + 4f * Mathf.Sin(now * 16.7f + i) : 22f;
                SetRect(_flames[i].rectTransform, _plan.RadarCx + (i == 0 ? -74f : 74f) * k, _plan.ShipY + 22f * k, 18f * k, fl * k);
            }
        }

        /// <summary>La revelación: las acertadas suben a la nave por un rayo tractor lima (750 ms cada una, escalonadas 160 ms); las que no elegiste derivan hacia afuera y se apagan con un aro coral punteado; las rocas quedan a media luz.</summary>
        private void AnimateReveal(float now)
        {
            float t = now - _revealAt;
            float k = _plan.Scale, cx = _plan.RadarCx, cy = _plan.RadarCy;
            foreach (var r in _rockImgs) r.color = new Color(1f, 1f, 1f, 0.5f);
            // las que no elegiste
            foreach (int i in _drifting)
            {
                var o = _round.Capsules[i];
                float d = Mathf.Min(1f, t / 2f);
                float x = o.X + o.X * 0.22f * d, y = o.Y + o.Y * 0.22f * d;
                var img = _capImgs[i];
                img.rectTransform.anchoredPosition = P(cx + x * k, cy + y * k);
                img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -(o.Tilt + (Motion.Decorative ? d * 0.6f : 0f)) * Mathf.Rad2Deg);
                img.color = new Color(1f, 1f, 1f, 1f - 0.45f * d);
                _capRings[i].rectTransform.anchoredPosition = img.rectTransform.anchoredPosition;
            }
            // las acertadas
            foreach (var l in _lifts)
            {
                float lk = Mathf.Clamp01((now - l.At) / 0.75f);
                float e = lk < 0.5f ? 2f * lk * lk : 1f - Mathf.Pow(-2f * lk + 2f, 2f) / 2f;
                var beam = _beams[l.Slot];
                var fly = _flying[l.Slot];
                if (lk >= 1f)
                {
                    beam.gameObject.SetActive(false);
                    fly.gameObject.SetActive(false);
                    foreach (var sp in SparksOf(l.Slot)) sp.gameObject.SetActive(false);
                    if (!l.Landed) Land(l, now);
                    continue;
                }
                float sx = cx, sy = _plan.ShipY - 18f * k;
                float fx = cx + l.FromX * k, fy = cy + l.FromY * k;
                beam.gameObject.SetActive(true);
                float fade = 1f - lk * 0.6f;
                beam.Set(P(fx - 30f * k, fy), P(fx + 30f * k, fy), P(sx + 10f * k, sy), P(sx - 10f * k, sy), new Color(Lime.r, Lime.g, Lime.b, 0.15f * 0.55f * fade), new Color(Lime.r, Lime.g, Lime.b, 0.75f * 0.55f * fade));
                _capImgs[l.Index].gameObject.SetActive(false);
                fly.gameObject.SetActive(true);
                if (Motion.Decorative)
                {
                    float px = Mathf.Lerp(fx, sx, e), py = Mathf.Lerp(fy, _plan.ShipY - 8f * k, e);
                    fly.rectTransform.anchoredPosition = P(px, py);
                    fly.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -l.Tilt * (1f - e) * Mathf.Rad2Deg);
                    float sc = (1f - 0.62f * e) * RadarContract.CapsuleSize * RadarSprites.CapsuleSideInSizes * k;
                    fly.rectTransform.sizeDelta = new Vector2(sc * _s, sc * _s);
                    fly.color = Color.white;
                    int n = 0;
                    foreach (var sp in SparksOf(l.Slot))
                    {
                        float sk = (now * 2f + n / (float)SparksPerBeam) % 1f;
                        sp.gameObject.SetActive(true);
                        sp.rectTransform.anchoredPosition = P(Mathf.Lerp(fx, sx, sk) + Mathf.Sin(n * 7f + now * 8.3f) * 6f, Mathf.Lerp(fy, sy, sk));
                        n++;
                    }
                }
                else
                {
                    // «quitar animaciones»: el cono queda fijo, la cápsula no vuela (se apaga al llegar su turno) y no hay chispas
                    fly.rectTransform.anchoredPosition = P(fx, fy);
                    fly.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -l.Tilt * Mathf.Rad2Deg);
                    float full = RadarContract.CapsuleSize * RadarSprites.CapsuleSideInSizes * k;
                    fly.rectTransform.sizeDelta = new Vector2(full * _s, full * _s);
                    fly.color = new Color(1f, 1f, 1f, 1f - lk);
                }
            }
        }

        private IEnumerable<Image> SparksOf(int slot)
        {
            for (int i = 0; i < SparksPerBeam; i++) yield return _sparks[slot * SparksPerBeam + i];
        }

        private void Land(Lift l, float now)
        {
            l.Landed = true;
            _seats.Add(l.Type);
            _shownRescued++;
            _shipPopAt = now;
            RefreshSeats();
            UpdateHudExtras();
            PlayClip(RadarSounds.Rescue(l.Slot), 0.8f);
            GameFeel.Haptic(GameFeel.HapticKind.Light);
        }

        /// <summary>Las diez ventanas de la nave: cada cápsula rescatada ocupa una con su color (se ven las últimas 10).</summary>
        private void RefreshSeats()
        {
            int from = Mathf.Max(0, _seats.Count - WindowCount);
            for (int i = 0; i < WindowCount; i++)
            {
                int s = from + i;
                _windows[i].color = s < _seats.Count ? RadarSprites.Colors[(int)_seats[s]] : WindowEmpty;
            }
        }

        // ------------------------------------------------------------------ la pantalla final

        private void PlaceEndCaps()
        {
            float y0 = RadarPlan.HudBottom + 84f, cs = 30f * RadarSprites.CapsuleSideInSizes;
            for (int i = 0; i < EndCapsPool; i++)
            {
                int col = i % 8, row = i / 8;
                SetRect(_endCaps[i].rectTransform, RadarPlan.Width * 0.5f - 3.5f * 34f + col * 34f, y0 + row * 34f, cs, cs);
            }
        }

        private void ShowEnd(int record, bool broke)
        {
            _beamOn = false;
            foreach (var b in _beams) b.gameObject.SetActive(false);
            foreach (var f in _flying) f.gameObject.SetActive(false);
            foreach (var sp in _sparks) sp.gameObject.SetActive(false);
            _radarLayer.gameObject.SetActive(false);
            _shipLayer.gameObject.SetActive(false);
            _boardLayer.gameObject.SetActive(false);
            _msgA.text = _msgB.text = "";
            _msgPill.gameObject.SetActive(false);
            _endTitle.text = RadarContract.EndTitle(_run.Rescued);
            _endValue[0].text = RadarContract.CaptureValue(_run.Capture);
            _endValue[1].text = RadarContract.ShortestValue(_run.ShortestPerfectMs);
            _endValue[2].text = RadarContract.PerfectValue(_run.Perfect, _run.RoundsPlayed);
            _endValue[3].text = RadarContract.StreakValue(_run.BestStreak);
            _endRecord.text = broke ? RadarContract.NewRecord : RadarContract.RecordLine(record);
            _endRecordBg.color = broke ? Gold : new Color(PanelFill.r, PanelFill.g, PanelFill.b, 0f);
            _endRecord.color = broke ? Ink : Lavender;
            for (int i = 0; i < EndCapsPool; i++)
            {
                bool on = i < _seats.Count;
                _endCaps[i].gameObject.SetActive(on);
                if (on) _endCaps[i].sprite = RadarSprites.CapsuleSprite(_seats[i]);
            }
            _endAt = Now;
            _endLayer.gameObject.SetActive(true);
            Layout();
            if (Motion.Decorative)
            {
                _confettiOn = true;
                _confettiAt = Now;
                var rng = new System.Random();
                for (int i = 0; i < ConfettiPool; i++)
                {
                    _confettiPos[i] = new Vector2(RadarPlan.Width * 0.5f + ((float)rng.NextDouble() - 0.5f) * 120f, 170f);
                    _confettiVel[i] = new Vector2(((float)rng.NextDouble() - 0.5f) * 260f, -120f - (float)rng.NextDouble() * 220f);
                    _confettiLife[i] = 1.6f + (float)rng.NextDouble() * 0.7f;
                    var c = RadarSprites.Colors[i % RadarContract.TypeCount];
                    _confetti[i].color = c;
                    float sz = 3f + (float)rng.NextDouble() * 3f;
                    _confetti[i].rectTransform.sizeDelta = new Vector2(sz * 2f * _s, sz * 2f * _s);
                    _confetti[i].gameObject.SetActive(true);
                }
            }
        }

        private void AnimateEnd(float now, float dt)
        {
            for (int i = 0; i < Mathf.Min(_seats.Count, EndCapsPool); i++)
            {
                float k = Motion.Decorative ? Mathf.Clamp01((now - _endAt - i * 0.045f) / 0.32f) : 1f;
                float e = 1f - Mathf.Pow(1f - k, 3f);
                _endCaps[i].rectTransform.localScale = Vector3.one * Mathf.Max(0.001f, e);          // las cápsulas rescatadas entran una tras otra
            }
            if (_confettiOn && dt > 0f)
            {
                float age = now - _confettiAt;
                for (int i = 0; i < ConfettiPool; i++)
                {
                    float life = _confettiLife[i];
                    bool on = age < life;
                    _confetti[i].gameObject.SetActive(on);
                    if (!on) continue;
                    _confettiVel[i].y += 300f * dt;
                    _confettiPos[i] += _confettiVel[i] * dt;
                    SetRect(_confetti[i].rectTransform, _confettiPos[i].x, _confettiPos[i].y, _confetti[i].rectTransform.sizeDelta.x / _s, _confetti[i].rectTransform.sizeDelta.y / _s);
                    var c = _confetti[i].color;
                    _confetti[i].color = new Color(c.r, c.g, c.b, 0.95f * (1f - age / life));
                }
            }
        }
    }
}
