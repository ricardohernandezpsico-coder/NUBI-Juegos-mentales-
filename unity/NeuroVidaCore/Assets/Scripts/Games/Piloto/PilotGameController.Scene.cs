using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Piloto
{
    public sealed partial class PilotGameController
    {
        // ------------------------------------------------------------------ el tinte de cada sector (boceto aprobado)

        private static readonly Color[] TintA =
        {
            new Color(60f / 255f, 90f / 255f, 220f / 255f), new Color(90f / 255f, 170f / 255f, 220f / 255f),
            new Color(220f / 255f, 110f / 255f, 90f / 255f), new Color(200f / 255f, 170f / 255f, 90f / 255f)
        };

        private static readonly Color[] TintB =
        {
            new Color(20f / 255f, 120f / 255f, 160f / 255f), new Color(140f / 255f, 120f / 255f, 230f / 255f),
            new Color(120f / 255f, 60f / 255f, 160f / 255f), new Color(60f / 255f, 90f / 255f, 200f / 255f)
        };

        private int _tintFrom, _tintTo;
        private float _tintMix = 1f;
        private const float TintAlpha = 0.5f, TintSeconds = 1.2f;

        // ------------------------------------------------------------------ reposo

        /// <summary>El estado de reposo: sin señales, sin avisos, sin pantalla final, sin arco ni rayo.</summary>
        private void ResetViews()
        {
            ClearSignals();
            ClearNotices();
            foreach (var f in _floatViews) { f.Active = false; f.Root.gameObject.SetActive(false); }
            for (int i = 0; i < _beaconDot.Length; i++) { _beaconDot[i].gameObject.SetActive(false); _beaconGlow[i].gameObject.SetActive(false); }
            _gateGraphic.gameObject.SetActive(false);
            _beam.gameObject.SetActive(false);
            _routeGraphic.Clear();
            _endLayer.gameObject.SetActive(false);
            ShowFlightLayers(true);
            _tintFrom = _tintTo = 0;
            _tintMix = 1f;
            StopEngine();
        }

        private void ShowFlightLayers(bool on)
        {
            _routeLayer.gameObject.SetActive(on);
            _sigLayer.gameObject.SetActive(on);
            _shipLayer.gameObject.SetActive(on);
            _stripLayer.gameObject.SetActive(on);
            _topLayer.gameObject.SetActive(on);
            _fxLayer.gameObject.SetActive(on);
            if (!on) _noticeLayer.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ la tarjeta de misión

        private bool _missionFresh;
        /// <summary>Cuándo cambió la misión (el latido de la tarjeta cuenta desde ahí; -10 = hace mucho).</summary>
        private float _missionPulseAt = -10f;

        /// <summary>La misión acaba de cambiar: la tarjeta se ilumina (brillo dorado 1,6 s), late dos veces (escala 1 → 1,12 → 1, ~0,8 s) y suenan dos notas que suben. «Quitar animaciones»: sin latido, queda el brillo fijo y el tono.</summary>
        private void MissionChanged(float now)
        {
            _missionFreshAt = now;
            _missionPulseAt = now;
            SetMissionCard();
            PlayClip(PilotSounds.MissionChange(), 0.85f);
        }

        private void SetMissionCard()
        {
            _missionIcon.sprite = PilotSignalSprites.Get(_mission.Shape, _mission.Detail);
            _missionIcon.enabled = true;
            _missionName.text = PilotContract.MissionName(_mission);
            _missionFresh = !(Now - _missionFreshAt < 1.6f);          // fuerza que se rearme
            AnimateMission(Now);
        }

        /// <summary>El rótulo («MISIÓN:» / «NUEVA MISIÓN:») y el nombre uno tras otro; el nombre ocupa lo que sobra de la tarjeta.</summary>
        private void PlaceMissionTexts()
        {
            float my = (_plan.MissionTop + _plan.MissionBottom) * 0.5f;
            float cx = PilotPlan.Width * 0.5f;                                           // la tarjeta es un objeto centrado en sí mismo: sus textos van por desplazamiento desde su centro
            float tagW = _missionTag.preferredWidth / _s + 4f;
            SetChild(_missionTag.rectTransform, 56f + tagW * 0.5f - cx, 0f, tagW, 22f);
            float left = 56f + tagW + 6f, w = 342f - left;
            SetChild(_missionName.rectTransform, left + w * 0.5f - cx, 0f, w, 22f);
        }

        private void AnimateMission(float now)
        {
            bool fresh = now - _missionFreshAt < 1.6f;
            if (fresh != _missionFresh)
            {
                _missionFresh = fresh;
                _missionTag.text = fresh ? PilotContract.NewMissionLabel : PilotContract.MissionLabel;
                _missionRim.color = fresh ? Gold : PanelEdge;
                PlaceMissionTexts();
            }
            float pulse = Motion.Decorative ? PilotContract.MissionPulseScale(now - _missionPulseAt) : 1f;
#if UNITY_EDITOR
            if (_editorNoticeHold && Motion.Decorative && now - _missionPulseAt >= 0f) pulse = PilotContract.PulsePeak;      // la toma del cambio de misión se saca con la tarjeta en lo más grande del latido (el peor caso para los bordes)
#endif
            _missionCard.localScale = Vector3.one * pulse;
            float a = fresh ? (Motion.Decorative ? 0.25f + 0.2f * Mathf.Sin(now * 11f) + 0.2f : 0.5f) : 0.95f;
            _missionFill.color = fresh ? new Color(Gold.r * 0.55f + PanelFill.r * 0.45f, Gold.g * 0.55f + PanelFill.g * 0.45f, Gold.b * 0.4f + PanelFill.b * 0.6f, Mathf.Clamp(a + 0.4f, 0.6f, 1f)) : new Color(PanelFill.r, PanelFill.g, PanelFill.b, 0.95f);
        }

        // ------------------------------------------------------------------ avisos (misión nueva por sector, hiperimpulso)

        private struct NoticeReq
        {
            public string Tag, Title;
            public float Seconds;
            public Color Tint;
            /// <summary>true = el aviso ALTO de misión nueva (título, forma dibujada con su nombre y el sector); false = el corto de dos renglones (hiperimpulso).</summary>
            public bool Tall;
            public int Sector;
            public PilotMission Mission;
        }

        private readonly Queue<NoticeReq> _noticeQueue = new Queue<NoticeReq>();
        private NoticeReq? _notice;
        private float _noticeAt;

        /// <summary>true mientras hay un aviso a la vista o esperando su turno: su rectángulo es zona prohibida para las señales nuevas (docs/diseno-piloto.md §7).</summary>
        private bool NoticeClaimsZone => _notice != null || _noticeQueue.Count > 0;

        private Box BoxOf(NoticeReq r) => r.Tall ? _plan.NoticeBox : _plan.BannerBox;

        /// <summary>El rectángulo que reclaman el aviso a la vista y los que esperan: el alto contiene al corto, así que si hay uno alto, ese.</summary>
        private Box ClaimedBox
        {
            get
            {
                bool tall = _notice.HasValue && _notice.Value.Tall;
                foreach (var q in _noticeQueue) tall |= q.Tall;
                return tall ? _plan.NoticeBox : _plan.BannerBox;
            }
        }

        private void RequestNotice(string tag, string title, float seconds, Color tint)
        {
            _noticeQueue.Enqueue(new NoticeReq { Tag = tag, Title = title, Seconds = seconds, Tint = tint });
        }

        /// <summary>El aviso de misión nueva (Tarea 63): «¡Nueva misión!», la forma con su detalle dibujada grande, su nombre y «Sector N · nombre». Va en el mismo lugar fijo de avisos, con la misma guardia: nunca sobre una señal.</summary>
        private void RequestMissionNotice(int sector, PilotMission mission, float seconds)
        {
            _noticeQueue.Enqueue(new NoticeReq { Seconds = seconds, Tint = Gold, Tall = true, Sector = sector, Mission = mission });
        }

        private void ClearNotices()
        {
            _noticeQueue.Clear();
            _notice = null;
            if (_noticeLayer != null) _noticeLayer.gameObject.SetActive(false);
        }

        /// <summary>El aviso espera a que NINGUNA señal (ni viva ni desvaneciéndose) quede bajo su rectángulo; mientras espera, las señales nuevas ya no nacen ahí.</summary>
        private void UpdateNotice(float now)
        {
            if (_notice == null && _noticeQueue.Count > 0 && !SignalUnder(BoxOf(_noticeQueue.Peek())))
            {
                var r = _noticeQueue.Dequeue();
                _notice = r;
                _noticeAt = now;
                ShowNoticeContent(r);
                _bannerGroup.alpha = 0f;
                _noticeLayer.gameObject.SetActive(true);
            }
            if (_notice == null) return;
            float t = now - _noticeAt, total = _notice.Value.Seconds;
#if UNITY_EDITOR
            if (_editorNoticeHold) total = Mathf.Max(total, t + 1f);      // las capturas sostienen el aviso a la vista hasta sacar la foto (con pocos cuadros por segundo un tiempo fijo no alcanza)
#endif
            _bannerGroup.alpha = Motion.Decorative ? Mathf.Clamp01(Mathf.Min(t / 0.2f, (total - t) / 0.3f)) : (t < total ? 1f : 0f);
            if (t >= total)
            {
                _notice = null;
                _noticeLayer.gameObject.SetActive(false);
            }
        }

        /// <summary>Enciende solo las piezas del aviso que toca (corto o alto) y les pone su texto y su forma.</summary>
        private void ShowNoticeContent(NoticeReq r)
        {
            _bannerRim.gameObject.SetActive(!r.Tall);
            _bannerFill.gameObject.SetActive(!r.Tall);
            _bannerTag.gameObject.SetActive(!r.Tall);
            _bannerTitle.gameObject.SetActive(!r.Tall);
            _tallRim.gameObject.SetActive(r.Tall);
            _tallFill.gameObject.SetActive(r.Tall);
            _tallTitle.gameObject.SetActive(r.Tall);
            _tallIcon.gameObject.SetActive(r.Tall);
            _tallName.gameObject.SetActive(r.Tall);
            _tallFoot.gameObject.SetActive(r.Tall);
            if (r.Tall)
            {
                _tallRim.color = r.Tint;
                _tallTitle.text = PilotContract.MissionNoticeTitle(r.Sector);
                _tallTitle.color = r.Tint;
                _tallIcon.sprite = PilotSignalSprites.Get(r.Mission.Shape, r.Mission.Detail);
                _tallName.text = PilotContract.MissionName(r.Mission);
                _tallFoot.text = PilotContract.MissionNoticeFoot(r.Sector);
                LayoutTallNotice();                                      // el grupo forma + nombre se centra según lo ancho del nombre
            }
            else
            {
                _bannerTag.text = r.Tag;
                _bannerTag.color = r.Tint;
                _bannerTitle.text = r.Title;
                _bannerRim.color = r.Tint;
            }
        }

        private bool SignalUnder(Box box)
        {
            foreach (var s in _signals)
                if (Box.Around(s.X, s.Y, PilotContract.SignalRingSize * 0.5f).Intersects(box, 0f)) return true;
            return false;
        }

        // ------------------------------------------------------------------ textos flotantes

        /// <summary>Un texto flotante junto a su señal (encima, debajo o al costado): SOLO en un lugar donde no tape otra señal, el aviso de sector, otro flotante, la tarjeta de misión ni la franja del dedo. Si no hay lugar, no sale.</summary>
        private void ShowFloat(string text, Color color, float x, float y, Signal owner)
        {
            FloatView f = null;
            foreach (var c in _floatViews) if (!c.Active) { f = c; break; }
            if (f == null) return;
            f.Label.text = text;
            f.Label.color = color;
            float w = Mathf.Max(64f, f.Label.preferredWidth / _s + 24f), h = 30f;
            float off = PilotContract.SignalRingSize * 0.5f + h * 0.5f + 3f, side = PilotContract.SignalRingSize * 0.5f + w * 0.5f + 6f;
            var candidates = new[] { new Vector2(x, y - off), new Vector2(x, y + off), new Vector2(x - side, y), new Vector2(x + side, y) };
            foreach (var c in candidates)
            {
                float cx = Mathf.Clamp(c.x, w * 0.5f + 6f, PilotPlan.Width - w * 0.5f - 6f), cy = c.y;
                var box = new Box(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);
                if (!FloatFree(box, owner, f)) continue;
                f.Area = box;
                f.Active = true;
                f.At = Now;
                f.Seconds = 1.0f;
                SetRect(f.Root, cx, cy, w, h);
                SetChild(f.Bg.rectTransform, 0f, 0f, w, h);
                SetChild(f.Label.rectTransform, 0f, 0f, w, h);
                f.Root.gameObject.SetActive(true);
                f.Root.localScale = Vector3.one;
                var g = f.Root.GetComponent<CanvasGroup>();
                if (g == null) g = f.Root.gameObject.AddComponent<CanvasGroup>();
                g.alpha = 0f;
                return;
            }
        }

        private bool FloatFree(Box b, Signal owner, FloatView self)
        {
            if (b.Y0 < _plan.MissionBottom + 8f || b.Y1 > _plan.StripTop - 4f) return false;
            foreach (var s in _signals)
                if (s != owner && Box.Around(s.X, s.Y, PilotContract.SignalRingSize * 0.5f).Intersects(b, 2f)) return false;
            if (NoticeClaimsZone && b.Intersects(ClaimedBox, 2f)) return false;
            foreach (var o in _floatViews)
                if (o != self && o.Active && b.Intersects(o.Area, 2f)) return false;
            return true;
        }

        private void AnimateFloats(float now)
        {
            foreach (var f in _floatViews)
            {
                if (!f.Active) continue;
                float k = (now - f.At) / f.Seconds;
                if (k >= 1f)
                {
                    f.Active = false;
                    f.Root.gameObject.SetActive(false);
                    continue;
                }
                f.Root.GetComponent<CanvasGroup>().alpha = Motion.Decorative ? Mathf.Clamp01(Mathf.Min(k * 6f, (1f - k) * 3f)) : (k < 0.85f ? 1f : 0f);
            }
        }

        // ------------------------------------------------------------------ un cuadro de dibujo

        private void Animate(float now, float dt)
        {
            AnimateTint(dt);
            if (_phase == Phase.Done || !_routeLayer.gameObject.activeInHierarchy) return;
            AnimateRoute(now);
            AnimateShip(now);
            AnimateSignals(now);
            AnimateStrip();
            AnimateMission(now);
            UpdateNotice(now);
            AnimateFloats(now);
            AnimateAudioAndStars(dt);
        }

        private void AnimateTint(float dt)
        {
            if (_tintMix < 1f) _tintMix = Mathf.Min(1f, _tintMix + dt / TintSeconds);
            var a = Color.Lerp(TintA[_tintFrom % TintA.Length], TintA[_tintTo % TintA.Length], _tintMix);
            var b = Color.Lerp(TintB[_tintFrom % TintB.Length], TintB[_tintTo % TintB.Length], _tintMix);
            _tintA.color = new Color(a.r, a.g, a.b, TintAlpha);
            _tintB.color = new Color(b.r, b.g, b.b, TintAlpha);
        }

        private float YOf(float p) => _plan.ShipY - (p - _dist);

        private void AnimateRoute(float now)
        {
            float pTop = _dist + (_plan.ShipY - _plan.RouteTop), pBottom = _dist - 70f;
            _routeGraphic.Clear();
            AddRoutePoint(pBottom);
            var beacons = _route.Beacons;
            int used = 0;
            for (int i = 0; i < beacons.Count; i++)
            {
                var b = beacons[i];
                if (b.P <= pBottom || b.P >= pTop) continue;
                AddRoutePoint(b.P);
                if (used < BeaconPool) { DrawBeacon(used, b, now); used++; }
            }
            AddRoutePoint(pTop);
            _routeGraphic.Apply(6f * _s, 1.5f * _s, Cyan, 0.07f, 0.16f, 0.28f);
            for (int i = used; i < BeaconPool; i++)
                for (int side = 0; side < 2; side++) { _beaconDot[i * 2 + side].gameObject.SetActive(false); _beaconGlow[i * 2 + side].gameObject.SetActive(false); }

            // el arco del sector: un portal dorado sobre la ruta que la nave cruza
            Gate gate = _gates.Count > 0 ? _gates[0] : null;
            if (gate != null && YOf(gate.P) > _plan.RouteTop - 24f && YOf(gate.P) < _plan.ShipY + 100f)
            {
                _route.At(gate.P, out float c, out float h);
                float y = YOf(gate.P);
                _gateGraphic.gameObject.SetActive(true);
                _gateGraphic.rectTransform.anchoredPosition = P(c, y);
                _gateGraphic.Set((h + 18f) * _s, 24f * _s, 5f * _s, 12f * _s, new Color(Gold.r, Gold.g, Gold.b, 0.9f));
            }
            else _gateGraphic.gameObject.SetActive(false);
        }

        private void AddRoutePoint(float p)
        {
            _route.At(p, out float c, out float h);
            float y = YOf(p);
            _routeGraphic.Add(P(c - h, y), P(c + h, y));
        }

        private void DrawBeacon(int slot, Beacon b, float now)
        {
            float y = YOf(b.P);
            float lit = b.LitAt > 0f && now - b.LitAt < 0.9f ? 1f - (now - b.LitAt) / 0.9f : 0f;
            bool bad = b.BadAt > 0f && now - b.BadAt < 0.7f;
            float blink = bad && Motion.Decorative ? 0.5f * Mathf.Abs(Mathf.Sin((now - b.BadAt) / 0.06f)) : (bad ? 0.5f : 0f);
            float alpha = 0.35f + 0.5f * lit + blink;
            Color glow = bad ? Bad : Cyan;
            Color dot = bad ? Bad : lit > 0f ? new Color(232f / 255f, 251f / 255f, 1f) : Cyan;
            float dotSize = bad ? 9.5f : 7.2f + 3.4f * lit;
            for (int side = 0; side < 2; side++)
            {
                int k = slot * 2 + side;
                float x = b.Cx + (side == 0 ? -b.Half : b.Half);
                var g = _beaconGlow[k];
                var d = _beaconDot[k];
                g.gameObject.SetActive(true);
                d.gameObject.SetActive(true);
                g.rectTransform.sizeDelta = new Vector2(28f * _s, 28f * _s);
                g.rectTransform.anchoredPosition = P(x, y);
                g.color = new Color(glow.r, glow.g, glow.b, Mathf.Clamp01(alpha));
                d.rectTransform.sizeDelta = new Vector2(dotSize * _s, dotSize * _s);
                d.rectTransform.anchoredPosition = P(x, y);
                d.color = dot;
            }
        }

        private void AnimateShip(float now)
        {
            float wob = 0f;
            float since = now - _wobbleAt;
            if (Motion.Decorative && since >= 0f && since < 0.4f) wob = Mathf.Sin(since * 33f) * 4f * (1f - since / 0.4f);
            _shipRoot.anchoredPosition = P(_shipX + wob, _plan.ShipY);
            _shipBody.color = _insideNow || _phase != Phase.Fly ? Color.white : new Color(1f, 0.78f, 0.74f, 1f);
            var gc = Hyper ? Cyan : new Color(1f, 170f / 255f, 90f / 255f);
            _shipGlow.color = new Color(gc.r, gc.g, gc.b, Motion.Decorative ? 0.3f + 0.08f * Mathf.Sin(now * 5f) : 0.3f);

            // el rayo al atrapar
            float bk = (now - _beamAt) / 0.26f;
            if (bk >= 0f && bk < 1f)
            {
                _beam.gameObject.SetActive(true);
                Vector2 a = new Vector2(_shipX, _plan.ShipY - 24f), b = new Vector2(_beamX, _beamY);
                Vector2 d = b - a;
                float len = d.magnitude;
                var r = _beam.rectTransform;
                r.anchoredPosition = P((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f);
                r.sizeDelta = new Vector2(len * _s, (4f * (1f - bk) + 1.5f) * _s);
                r.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                _beam.color = new Color(1f, 214f / 255f, 120f / 255f, Motion.Decorative ? 0.85f * (1f - bk) : 0.85f);
            }
            else _beam.gameObject.SetActive(false);
        }

        private void AnimateSignals(float now)
        {
            foreach (var s in _signals)
            {
                if (s.View < 0) continue;
                var v = _views[s.View];
                float age = now - s.At;
                bool done = !s.Alive;
                float k = done ? Mathf.Max(0f, 1f - (now - s.DoneAt) / SignalFadeSeconds) : Mathf.Min(1f, age / SignalEnterSeconds);
                if (!Motion.Decorative) k = done ? (now - s.DoneAt < SignalFadeSeconds * 0.8f ? 1f : 0f) : 1f;
                v.Ring.gameObject.SetActive(!done);
                if (!done) v.Ring.fillAmount = Mathf.Clamp01(1f - age / s.Expo);
                v.Ring.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.85f * k);
                v.Body.color = new Color(1f, 1f, 1f, k);
                float scale = s.Outcome == Outcome.Caught && Motion.Decorative ? 1f + 0.6f * (1f - k) : 1f;
                v.Body.rectTransform.localScale = Vector3.one * scale;
                bool wrong = s.Outcome == Outcome.Wrong;
                if (v.Mark.gameObject.activeSelf != wrong) v.Mark.gameObject.SetActive(wrong);
                if (wrong) v.Mark.color = new Color(Bad.r, Bad.g, Bad.b, k);
            }
        }

        private void AnimateStrip()
        {
            bool beginner = Gentle || _guided;
            _stripLabel.text = beginner ? PilotContract.StripLabelGentle : PilotContract.StripLabel;
            _stripHint.text = beginner ? PilotContract.StripHint : "";
            SetRect(_fingerRing.rectTransform, _steerX, FingerY, 40f, 40f);
            SetRect(_fingerDot.rectTransform, _steerX, FingerY, 30f, 30f);
            // la línea punteada del dedo a la nave
            Vector2 from = new Vector2(_steerX, FingerY - 22f), to = new Vector2(_shipX, _plan.ShipY + 32f);
            for (int i = 0; i < _dots.Length; i++)
            {
                float k = (i + 0.5f) / _dots.Length;
                var p = Vector2.Lerp(from, to, k);
                _dots[i].rectTransform.anchoredPosition = P(p.x, p.y);
            }
            _fingerDot.color = new Color(Cyan.r, Cyan.g, Cyan.b, (_steerFinger >= 0 || _mouseSteer) ? 0.75f : 0.5f);
        }

        private void AnimateAudioAndStars(float dt)
        {
            float speedK = SpeedK;
            if (_stars != null)
            {
                float warp = Motion.Decorative ? (Hyper ? 0.85f : 0.12f + 0.3f * speedK) : 0.08f;      // «quitar animaciones»: sin estrellas en línea
                _stars.Warp = Mathf.MoveTowards(_stars.Warp, warp, dt * 1.5f);
            }
            // el motor «Cohete»: la mezcla y el tono siguen la velocidad. Con Nubi congelando el juego (tutorial) no se corta: baja al 30 % y al soltar vuelve a su volumen (Tarea 64); con la pausa del menú el audio queda callado
            if (_engine != null) _engine.Tick(speedK, Hyper, GameClock.RealDeltaTime, GameClock.LoopVolume, GameClock.AudioSilenced);
        }

        // ------------------------------------------------------------------ la pantalla final

        private void ShowEnd(float? score)
        {
            _endTitle.text = PilotContract.EndTitle(_run.Points);
            for (int i = 0; i < PilotContract.Sectors; i++)
            {
                var c = TintA[i % TintA.Length];
                _endSector[i].color = new Color(c.r, c.g, c.b, 1f);
                string name = PilotContract.SectorName(i);
                int sp = name.IndexOf(' ');
                _endSectorName[i].text = sp > 0 ? name.Substring(0, sp) + "\n" + name.Substring(sp + 1) : name;
            }
            string[] labels =
            {
                PilotContract.EndLane, PilotContract.EndMission, PilotContract.EndWrong, PilotContract.EndLevel, PilotContract.EndStreak, PilotContract.EndMeasure
            };
            string[] values =
            {
                PilotContract.LaneValue(_run.InLaneFraction), PilotContract.MissionValue(_run.Hits, _run.Targets), _run.FalseAlarms.ToString(),
                PilotContract.LevelValue(_signalDda.Level), PilotContract.StreakValue(_run.BestStreak),
                score.HasValue ? PilotContract.MeasureValue(score) : PilotContract.MeasureTooFew
            };
            for (int i = 0; i < labels.Length; i++)
            {
                _endLabel[i].text = labels[i];
                _endValue[i].text = values[i];
                bool measure = i == labels.Length - 1;                                    // «Tus señales a los mandos» es la medida propia: va en dorado
                _endLabel[i].color = measure ? Gold : Soft;
                _endValue[i].color = measure ? Gold : Color.white;
            }
            _endLayer.gameObject.SetActive(true);
        }
    }
}
